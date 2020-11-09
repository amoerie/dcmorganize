using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using KeyedSemaphores;
using Polly;
using Polly.Retry;

namespace DcmOrganize
{
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

        public async Task OrganizeAsync(DicomOrganizerOptions dicomOrganizerOptions, CancellationToken cancellationToken)
        {
            var files = dicomOrganizerOptions.Files ?? _filesFromConsoleInputReader.Read(cancellationToken);
            var directory = dicomOrganizerOptions.Directory;
            var pattern = dicomOrganizerOptions.Pattern;
            var action = dicomOrganizerOptions.Action;
            var parallelism = dicomOrganizerOptions.Parallelism;
            
            if (!directory.Exists)
            {
                throw new DicomOrganizeException($"Target directory does not exist: {directory.FullName}");
            }

            await Task.WhenAll(
                Partitioner
                    .Create(files)
                    .GetPartitions(parallelism)
                    .AsParallel()
                    .Select(partition => OrganizeFilesAsync(partition, pattern, directory, action))
            ).ConfigureAwait(false);
        }
        
        private async Task OrganizeFilesAsync(IEnumerator<FileInfo> files, string pattern, DirectoryInfo directory, Action action)
        {
            using (files)
            {
                while (files.MoveNext())
                {
                    await OrganizeFileAsync(files.Current, pattern, directory, action).ConfigureAwait(false);
                }
            }
        }

        private async Task OrganizeFileAsync(FileInfo file, string pattern, DirectoryInfo directory, Action action)
        {
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
                using (await KeyedSemaphore.LockAsync(highestDirectoryName))
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
                _logger.WriteLine($"OK:    {file.FullName} === {targetFile.FullName}");
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
                        _logger.WriteLine($"OK:    {file.FullName} === {targetFile.FullName}");
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
                        break;
                    }
                    case Action.Copy:
                    {
                        _ioPolicy.Execute(() => File.Copy(file.FullName, targetFile.FullName));
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
}