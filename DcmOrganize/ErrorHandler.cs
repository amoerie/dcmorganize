using System;
using System.CommandLine;
using System.CommandLine.IO;

namespace DcmOrganize;

internal interface IErrorHandler
{
    void Handle(DicomOrganizeException error);
}

internal class StopErrorHandler : IErrorHandler
{
    private readonly IConsole _console;

    public StopErrorHandler(IConsole console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }
        
    public void Handle(DicomOrganizeException error)
    {
        _console.Error.WriteLine(error.ToString());
        throw error;
    }
}

internal class ContinueErrorHandler : IErrorHandler
{
    private readonly IConsole _console;

    public ContinueErrorHandler(IConsole console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }
        
    public void Handle(DicomOrganizeException error)
    {
        _console.Error.WriteLine(error.ToString());
    }
}