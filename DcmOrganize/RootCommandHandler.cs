using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace DcmOrganize;

internal interface IRootCommandHandler
{
    Task ExecuteAsync(IConsole console, DicomOrganizerOptions dicomOrganizerOptions, CancellationToken cancellationToken);
}
    
internal class RootCommandHandler : IRootCommandHandler
{
    private readonly IFilesFromConsoleInputReader _filesFromConsoleInputReader;

    public RootCommandHandler(IFilesFromConsoleInputReader filesFromConsoleInputReader)
    {
        _filesFromConsoleInputReader = filesFromConsoleInputReader ?? throw new ArgumentNullException(nameof(filesFromConsoleInputReader));
    }
        
    public Task ExecuteAsync(IConsole console, DicomOrganizerOptions dicomOrganizerOptions, CancellationToken cancellationToken)
    {
        var dicomTagParser = new DicomTagParser();
        var folderNameCleaner = new FolderNameCleaner();
        var patternApplier = new PatternApplier(dicomTagParser, folderNameCleaner);
        var errorHandler = dicomOrganizerOptions.ErrorMode == ErrorMode.Continue
            ? (IErrorHandler) new ContinueErrorHandler(console)
            : new StopErrorHandler(console); 
        var logger = new Logger(console);
        var dicomOrganizer = new DicomOrganizer(patternApplier, errorHandler, logger, _filesFromConsoleInputReader);
        return dicomOrganizer.OrganizeAsync(dicomOrganizerOptions, cancellationToken);
    }
}