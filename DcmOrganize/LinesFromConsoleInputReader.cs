using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DcmOrganize;

internal interface ILinesFromConsoleInputReader
{
    IAsyncEnumerable<string> Read(CancellationToken cancellationToken);
}

public class LinesFromConsoleInputReader : ILinesFromConsoleInputReader
{
    private readonly TextReader _consoleInput;

    public LinesFromConsoleInputReader(TextReader consoleInput)
    {
        _consoleInput = consoleInput ?? throw new ArgumentNullException(nameof(consoleInput));
    }

    public async IAsyncEnumerable<string> Read([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var cancellationCts = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = cancellationToken.Register(() => cancellationCts.SetCanceled());
        Task<string?> cancellationTask = cancellationCts.Task;
            
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await await Task.WhenAny(
                Task.Run(() => _consoleInput.ReadLineAsync(), cancellationToken), 
                cancellationTask);
                
            if (line == null)
                yield break;

            yield return line;
        }
    }
}