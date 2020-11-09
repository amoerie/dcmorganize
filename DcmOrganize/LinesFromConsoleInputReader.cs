using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace DcmOrganize
{
    internal interface ILinesFromConsoleInputReader
    {
        IEnumerable<string> Read(CancellationToken cancellationToken);
    }

    public class LinesFromConsoleInputReader : ILinesFromConsoleInputReader
    {
        private readonly TextReader _consoleInput;

        public LinesFromConsoleInputReader(TextReader consoleInput)
        {
            _consoleInput = consoleInput ?? throw new ArgumentNullException(nameof(consoleInput));
        }
        
        public IEnumerable<string> Read(CancellationToken cancellationToken)
        {
            StringBuilder builder = new StringBuilder();
            
            bool carriageReturn = false;
            while (!cancellationToken.IsCancellationRequested)
            {
                int current = _consoleInput.Read();

                // End of input
                if (current == -1)
                {
                    var line = builder.ToString();
                    if (line.Length == 0)
                        yield break;

                    builder.Clear();
                    
                    yield return line;
                    yield break;
                }

                // A line is defined as a sequence of characters followed by
                // a carriage return ('\r') or
                // a line feed ('\n') or
                // a carriage return immediately followed by a line feed ('\r\n')
                if (current == '\r')
                {
                    // We must inspect the next character to know what to do
                    // - if followed by '\n', this is a carriage return + line feed
                    // - if followed by any other text, this is a simple carriage return
                    carriageReturn = true;
                    continue;
                }                
                
                if (current == '\n')
                {
                    var line = builder.ToString();

                    if (line.Length > 0)
                    {
                        yield return line;
                        builder.Clear();
                    }

                    continue;
                }
                
                if (carriageReturn)
                {
                    var line = builder.ToString();

                    if (line.Length > 0)
                    {
                        yield return line;
                        builder.Clear();
                    }

                    carriageReturn = false;
                }
                
                builder.Append((char)current);
            }
        }
    }
}