using System;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace DcmOrganize;

public static class Program
{
    static Task<int> Main(string[] args)
    {
        var filesFromConsoleInputReader = new FilesFromConsoleInputReader(new LinesFromConsoleInputReader(Console.In));
        var rootCommandHandler = new RootCommandHandler(filesFromConsoleInputReader);
        var rootCommandFactory = new RootCommandFactory(rootCommandHandler);
        var rootCommand = rootCommandFactory.Create();
        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseExceptionHandler(ExceptionHandler)
            .Build();
        return parser.InvokeAsync(args);
    }

    static void ExceptionHandler(Exception e, InvocationContext context)
    {
        if (e is OperationCanceledException)
            return;

        ExceptionDispatchInfo.Capture(e).Throw();
    }
}