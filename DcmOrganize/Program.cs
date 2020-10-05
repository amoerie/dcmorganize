using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Dicom;
using KeyedSemaphores;

namespace DcmOrganize
{
    public static class Program
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable ClassNeverInstantiated.Global
        public class Options
        {
            [Value(0, HelpText = "Organize these DICOM files. When missing, this option will be read from the piped input.", Required = false)]
            public IEnumerable<string>? Files { get; set; }

            [Option('t', "targetDirectory", Default = ".", HelpText = "Organize DICOM files in this directory")]
            public string? TargetDirectory { get; set; }

            [Option('f', "targetFilePattern", Default = "{PatientName}/{AccessionNumber}/{SeriesNumber}/{InstanceNumber ?? SOPInstanceUID} - {Guid}.dcm",
                HelpText =
                    "Write DICOM files using this pattern. DICOM tags are supported. Fallback for missing DICOM tags are supported. Nested directories will be created on demand.")]
            public string? TargetFilePattern { get; set; }
            
            [Option('p', "parallelism", Default = 8, HelpText = "Process this many files in parallel")]
            public int Parallelism { get; set; }
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Global
        // ReSharper restore MemberCanBePrivate.Global
        // ReSharper restore ClassNeverInstantiated.Global


        static async Task Main(string[] args)
        {
            var parserResult = Parser.Default.ParseArguments<Options>(args);

            if (parserResult is Parsed<Options> parsed)
            {
                await OrganizeAsync(parsed.Value).ConfigureAwait(false);
            }
            else if (parserResult is NotParsed<Options> notParsed)
            {
                Fail(notParsed.Errors);
            }
        }

        private static void Fail(IEnumerable<Error> errors)
        {
            Console.Error.WriteLine("Invalid arguments provided");
            foreach (var error in errors.Where(e => e.Tag != ErrorType.HelpRequestedError))
            {
                Console.Error.WriteLine(error.ToString());
            }
        }

        private static async Task OrganizeAsync(Options options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            IEnumerable<FileInfo> ReadFilesFromConsole()
            {
                string? file;
                while ((file = Console.ReadLine()) != null)
                {
                    if (file != null && File.Exists(file))
                        yield return new FileInfo(file);
                }
            }

            var files = options.Files != null && options.Files.Any() 
                ? options.Files.Select(f => new FileInfo(f)) 
                : ReadFilesFromConsole();
            var targetDirectory = new DirectoryInfo(options.TargetDirectory!);
            var targetFilePattern = options.TargetFilePattern!;
            var parallelism = options.Parallelism;

            if (!targetDirectory.Exists)
            {
                await Console.Error.WriteLineAsync($"Target directory does not exist: {targetDirectory.FullName}");
                return;
            }

            await Task.WhenAll(
                Partitioner
                    .Create(files)
                    .GetPartitions(parallelism)
                    .AsParallel()
                    .Select(partition => OrganizeFilesAsync(partition, targetFilePattern, targetDirectory))
            ).ConfigureAwait(false);
        }

        private static async Task OrganizeFilesAsync(IEnumerator<FileInfo> files, string targetFilePattern, DirectoryInfo targetDirectory)
        {
            using (files)
            {
                while (files.MoveNext())
                {
                    await OrganizeFileAsync(files.Current, targetFilePattern, targetDirectory).ConfigureAwait(false);
                }
            }
        }

        private static async Task OrganizeFileAsync(FileInfo file, string targetFilePattern, DirectoryInfo targetDirectory)
        {
            DicomFile dicomFile;
            try
            {
                await using var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096);

                dicomFile = await DicomFile.OpenAsync(fileStream, FileReadOption.SkipLargeTags);
            }
            catch
            {
                await Console.Error.WriteLineAsync("Not a DICOM file: " + file.FullName);
                return;
            }

            if (!DicomFilePatternApplier.TryApply(dicomFile.Dataset, targetFilePattern, out var fileName))
            {
                await Console.Error.WriteLineAsync("Failed to apply DICOM file pattern: " + file.FullName);
                return;
            }

            var targetFile = new FileInfo(Path.Join(targetDirectory.FullName, fileName));

            if (!targetFile.Directory!.Exists)
            {
                var highestDirectoryName = HighestDirectoryNameDeterminer.Determine(fileName!);
                using (await KeyedSemaphore.LockAsync(highestDirectoryName))
                {
                    try
                    {
                        if (!targetFile.Directory.Exists)
                        {
                            targetFile.Directory!.Create();
                        }
                    }
                    catch (IOException exception)
                    {
                        await Console.Error.WriteLineAsync($"Failed to create directory {targetFile.Directory.FullName}");
                        await Console.Error.WriteLineAsync(exception.ToString());
                        return;
                    }
                }
            }

            if (file.FullName == targetFile.FullName)
            {
                Console.WriteLine($"OK:    {file.FullName} === {targetFile.FullName}");
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
                        Console.WriteLine($"OK:    {file.FullName} === {targetFile.FullName}");
                        return;
                    }
                }
            }

            try
            {
                Console.WriteLine($"Moving {file.FullName} --> {targetFile.FullName}");
                File.Move(file.FullName, targetFile.FullName);
            }
            catch (IOException exception)
            {
                await Console.Error.WriteLineAsync("Failed to move file");
                await Console.Error.WriteLineAsync(exception.ToString());
            }
        }
    }
}