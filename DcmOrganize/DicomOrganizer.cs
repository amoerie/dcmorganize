using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FellowOakDicom;
using KeyedSemaphores;
using Polly;
using Polly.Retry;

namespace DcmOrganize;

internal interface IDicomOrganizer
{
    Task OrganizeAsync(DicomOrganizerOptions dicomOrganizerOptions, CancellationToken cancellationToken);
}
    
internal class DicomOrganizer : IDicomOrganizer
{
    private readonly IPatternApplier _patternApplier;
    private readonly IErrorHandler _errorHandler;
    private readonly ILogger _logger;
    private readonly IFilesFromConsoleInputReader _filesFromConsoleInputReader;
    private readonly RetryPolicy _ioPolicy;

    public DicomOrganizer(IPatternApplier patternApplier, IErrorHandler errorHandler, ILogger logger,
        IFilesFromConsoleInputReader filesFromConsoleInputReader)
    {
        _patternApplier = patternApplier ?? throw new ArgumentNullException(nameof(patternApplier));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _filesFromConsoleInputReader = filesFromConsoleInputReader ?? throw new ArgumentNullException(nameof(filesFromConsoleInputReader));
        _ioPolicy = Policy.Handle<IOException>().Retry(3);
    }

    public async Task OrganizeAsync(DicomOrganizerOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var parallelism = options.Parallelism;

            if (!options.Directory.Exists)
            {
                throw new DicomOrganizeException($"Target directory does not exist: {options.Directory.FullName}");
            }

            var filesChannel = Channel.CreateUnbounded<FileInfo>(new UnboundedChannelOptions
            {
                SingleWriter = true,
                SingleReader = false
            });

            var tasks = new List<Task>
            {
                Task.Run(() => ProduceAsync(filesChannel.Writer, options, cancellationToken), cancellationToken)
            };
            for (var i = 0; i < parallelism; i++)
            {
                tasks.Add(
                    Task.Run(() => ConsumeAsync(filesChannel.Reader, options, cancellationToken), cancellationToken)
                );
            }

            await Task.WhenAll(tasks).WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.WriteLine("Cancelling...");
        }
    }

    private async Task ProduceAsync(ChannelWriter<FileInfo> filesChannelWriter, DicomOrganizerOptions dicomOrganizerOptions, CancellationToken cancellationToken)
    {
        if (dicomOrganizerOptions.Files != null)
        {
            foreach (var file in dicomOrganizerOptions.Files)
            {
                await filesChannelWriter.WriteAsync(file, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            await foreach (var file in _filesFromConsoleInputReader.Read(cancellationToken))
            {
                await filesChannelWriter.WriteAsync(file, cancellationToken).ConfigureAwait(false);
            }
        }
        filesChannelWriter.Complete();
    }
        
    private async Task ConsumeAsync(ChannelReader<FileInfo> filesChannelReader, DicomOrganizerOptions options, CancellationToken cancellationToken)
    {
        if (filesChannelReader == null) throw new ArgumentNullException(nameof(filesChannelReader));
        if (options == null) throw new ArgumentNullException(nameof(options));
            
        while (await filesChannelReader.WaitToReadAsync(cancellationToken))
        {
            while (filesChannelReader.TryRead(out var file))
            {
                cancellationToken.ThrowIfCancellationRequested();
                    
                await OrganizeFileAsync(file, options, cancellationToken);
            }
        }
    }

    private async Task OrganizeFileAsync(FileInfo file, DicomOrganizerOptions options, CancellationToken cancellationToken)
    {
        var directory = options.Directory;
        var pattern = options.Pattern;
        var action = options.Action;
            
        DicomFile dicomFile;
        try
        {
            await using var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096);

            dicomFile = await DicomFile.OpenAsync(fileStream, FileReadOption.SkipLargeTags);
        }
        catch(DicomFileException e)
        {
            throw new DicomOrganizeException("Not a DICOM file: " + file.FullName, e);
        }

        string fileName;
        try
        {
            fileName = _patternApplier.Apply(dicomFile.Dataset, pattern);
        }
        catch (PatternException e)
        {
            throw new DicomOrganizeException($"Failed to apply pattern to file {file}", e);
        }

        var targetFile = new FileInfo(Path.Join(directory.FullName, fileName));

        if (!targetFile.Directory!.Exists)
        {
            var highestDirectoryName = HighestDirectoryNameDeterminer.Determine(fileName!);
            using (await KeyedSemaphore.LockAsync(highestDirectoryName, cancellationToken))
            {
                try
                {
                    if (!targetFile.Directory.Exists)
                    {
                        var directoryToCreate = targetFile.Directory!;
                            
                        _ioPolicy.Execute(() => directoryToCreate.Create());
                    }
                }
                catch (IOException exception)
                {
                    throw new DicomOrganizeException($"Failed to create directory {targetFile.Directory.FullName}", exception);
                }
            }
        }

        if (file.FullName == targetFile.FullName)
        {
            _logger.WriteLine(targetFile.FullName);
            return;
        }

        if (targetFile.Exists)
        {
            var counter = 1;
            var targetFileName = targetFile.Name;
            var targetFileDirectoryName = targetFile.Directory.FullName;
            var targetFileExtension = targetFile.Extension;
            var targetFileNameWithoutExtension = targetFileName.Substring(0, targetFileName.Length - targetFileExtension.Length);

            while (targetFile.Exists)
            {
                targetFileName = $"{targetFileNameWithoutExtension} ({counter++}){targetFile.Extension}";

                targetFile = new FileInfo(Path.Join(targetFileDirectoryName, targetFileName));

                if (file.FullName == targetFile.FullName)
                {
                    _logger.WriteLine(targetFile.FullName);
                    return;
                }
            }
        }

        try
        {
            switch (action)
            {
                case Action.Move:
                {
                    _ioPolicy.Execute(() => File.Move(file.FullName, targetFile.FullName));
                    _logger.WriteLine(targetFile.FullName);
                    break;
                }
                case Action.Copy:
                {
                    _ioPolicy.Execute(() => File.Copy(file.FullName, targetFile.FullName));
                    _logger.WriteLine(targetFile.FullName);
                    break;
                }
            }
        }
        catch (IOException e)
        {
            _errorHandler.Handle(new DicomOrganizeException($"Failed to {action.ToString().ToLowerInvariant()} {file}", e));
        }
    }
}
