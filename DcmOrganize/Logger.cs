using System;
using System.CommandLine;
using System.CommandLine.IO;

namespace DcmOrganize;

internal interface ILogger
{
    void WriteLine(string message);
}
    
internal class Logger : ILogger
{
    private readonly IConsole _console;

    public Logger(IConsole console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }
        
    public void WriteLine(string message)
    {
        _console.Out.WriteLine(message);
    }
}