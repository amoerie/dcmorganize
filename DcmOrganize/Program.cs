using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace DcmOrganize
{
    public static class Program
    {
        static Task<int> Main(string[] args)
        {
            var filesFromConsoleInputReader = new FilesFromConsoleInputReader(new LinesFromConsoleInputReader(Console.In));
            var rootCommandHandler = new RootCommandHandler(filesFromConsoleInputReader);
            var rootCommandFactory = new RootCommandFactory(rootCommandHandler);
            var rootCommand = rootCommandFactory.Create();
            return rootCommand.InvokeAsync(args);
        }
    }
}