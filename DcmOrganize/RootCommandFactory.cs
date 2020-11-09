using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;

namespace DcmOrganize
{
    internal class RootCommandFactory
    {
        private readonly IRootCommandHandler _rootCommandHandler;

        public RootCommandFactory(IRootCommandHandler rootCommandHandler)
        {
            _rootCommandHandler = rootCommandHandler ?? throw new ArgumentNullException(nameof(rootCommandHandler));
        }

        public RootCommand Create()
        {
            var filesArgument = new Argument("files")
            {
                Arity = ArgumentArity.ZeroOrMore,
                ArgumentType = typeof(IEnumerable<FileInfo>)
            };
            var filesOption = new Option(new[] {"-f", "--files"}, "Organize these DICOM files. When missing, this option will be read from the piped input.")
            {
                Argument = filesArgument
            };

            var directoryOption = new Option(new[] {"-d", "--directory"}, "Organize DICOM files in this directory")
            {
                Argument = new Argument("targetDirectory")
                {
                    Arity = ArgumentArity.ZeroOrOne,
                    ArgumentType = typeof(DirectoryInfo)
                }
            };
            directoryOption.Argument.SetDefaultValue(new DirectoryInfo(Environment.CurrentDirectory));

            var patternOption = new Option(new[] {"-p", "--pattern"},
                "Write DICOM files using this pattern. DICOM tags are supported. Fallback for missing DICOM tags are supported. Nested directories will be created on demand.")
            {
                Argument = new Argument("pattern")
                {
                    Arity = ArgumentArity.ZeroOrOne,
                    ArgumentType = typeof(string)
                }
            };
            patternOption.Argument.SetDefaultValue("{PatientName}/{AccessionNumber}/{SeriesNumber}/{InstanceNumber ?? SOPInstanceUID} - {Guid}.dcm");

            var actionOption = new Option(new[] {"-a", "--action"}, "Action to execute for each file")
            {
                Argument = new Argument("action")
                {
                    Arity = ArgumentArity.ZeroOrOne,
                    ArgumentType = typeof(Action)
                }
            };
            actionOption.Argument.SetDefaultValue(Action.Move);

            var parallelismOption = new Option(new[] {"--parallelism"}, "Process this many files in parallel")
            {
                Argument = new Argument("parallelism")
                {
                    Arity = ArgumentArity.ZeroOrOne,
                    ArgumentType = typeof(int?)
                }
            };
            parallelismOption.Argument.SetDefaultValue(8);

            var errorModeOption = new Option(new[] {"--errorMode"}, "Specifies what to do when an error occurs")
            {
                Argument = new Argument("errorMode")
                {
                    Arity = ArgumentArity.ZeroOrOne,
                    ArgumentType = typeof(ErrorMode)
                }
            };
            errorModeOption.Argument.SetDefaultValue(ErrorMode.Stop);

            var organizeCommand = new RootCommand("Organize DCM files")
            {
                filesOption,
                directoryOption,
                patternOption,
                actionOption,
                parallelismOption
            };

            organizeCommand.Handler =
                CommandHandler.Create<IConsole, DicomOrganizerOptions, CancellationToken>((console, options, cancellationToken) =>
                    _rootCommandHandler.ExecuteAsync(console, options, cancellationToken));

            return organizeCommand;
        }
    }
}