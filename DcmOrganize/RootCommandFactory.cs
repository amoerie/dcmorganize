using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;

namespace DcmOrganize;

internal class RootCommandFactory
{
    private readonly IRootCommandHandler _rootCommandHandler;

    public RootCommandFactory(IRootCommandHandler rootCommandHandler)
    {
        _rootCommandHandler = rootCommandHandler ?? throw new ArgumentNullException(nameof(rootCommandHandler));
    }

    public RootCommand Create()
    {
        var filesOption = new Option<IEnumerable<FileInfo>>(
            aliases: new[] {"-f", "--files"},
            description: "Organize these DICOM files. When missing, this option will be read from the piped input.")
        {
            Arity = ArgumentArity.ZeroOrMore,
        };

        var directoryOption = new Option<DirectoryInfo>(
            aliases: new[] {"-d", "--directory"},
            description: "Organize DICOM files in this directory",
            getDefaultValue: () => new DirectoryInfo(Environment.CurrentDirectory))
        {
            Arity = ArgumentArity.ZeroOrOne,
        };
            
        var patternOption = new Option<string>(
            aliases: new[] {"-p", "--pattern"},
            description: "Write DICOM files using this pattern. DICOM tags are supported. Fallback for missing DICOM tags are supported. Nested directories will be created on demand.",
            getDefaultValue: () => "{PatientName}/{AccessionNumber}/{SeriesNumber}/{InstanceNumber ?? SOPInstanceUID} - {Guid}.dcm")
        {
            Arity = ArgumentArity.ZeroOrOne,
        };
            
        var actionOption = new Option<Action>(
            aliases: new[] {"-a", "--action"},
            description: "Action to execute for each file",
            getDefaultValue: () => Action.Move)
        {
            Arity = ArgumentArity.ZeroOrOne
        };
            
        var parallelismOption = new Option<int>(
            aliases: new[] {"--parallelism"},
            description: "Process this many files in parallel",
            getDefaultValue: () => 8)
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        var errorModeOption = new Option<ErrorMode>(
            aliases: new[] {"--errorMode"},
            description: "Specifies what to do when an error occurs",
            getDefaultValue: () => ErrorMode.Stop)
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        var organizeCommand = new RootCommand("Organize DCM files")
        {
            filesOption,
            directoryOption,
            patternOption,
            actionOption,
            parallelismOption,
            errorModeOption
        };
            
        organizeCommand.SetHandler(context =>
        {
            var bindingContext = context.BindingContext;
            var console = context.Console;
            var cancellationToken = context.GetCancellationToken();
            var parseResult = bindingContext.ParseResult;
            var files = parseResult.GetValueForOption(filesOption)!;
            var directory = parseResult.GetValueForOption(directoryOption)!;
            var pattern = parseResult.GetValueForOption(patternOption)!;
            var action = parseResult.GetValueForOption(actionOption)!;
            var parallelism = parseResult.GetValueForOption(parallelismOption)!;
            var errorMode = parseResult.GetValueForOption(errorModeOption)!;
            var options = new DicomOrganizerOptions
            {
                Files = files,
                Directory = directory,
                Pattern = pattern,
                Action = action,
                Parallelism = parallelism,
                ErrorMode = errorMode
            };
            _rootCommandHandler.ExecuteAsync(console, options, cancellationToken);
        });

        return organizeCommand;
    }
}
