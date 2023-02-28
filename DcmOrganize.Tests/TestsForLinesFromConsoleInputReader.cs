using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DcmOrganize.Tests;

public class TestsForLinesFromConsoleInputReader : IDisposable
{
    private readonly LinesFromConsoleInputReader _linesFromConsoleInputReader;
    private readonly TextReader _consoleInputReader;
    private readonly Stream _consoleInputStream;
    private readonly StreamWriter _consoleInputWriter;

    public TestsForLinesFromConsoleInputReader()
    {
        _consoleInputStream = new MemoryStream();
        _consoleInputReader = new StreamReader(_consoleInputStream);
        _consoleInputWriter = new StreamWriter(_consoleInputStream) { AutoFlush = true };
        _linesFromConsoleInputReader = new LinesFromConsoleInputReader(_consoleInputReader);
    }

    [Fact]
    public async Task ShouldReadEmptyString()
    {
        var lines = await _linesFromConsoleInputReader.Read(CancellationToken.None).ToListAsync();

        lines.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldReadSingleLine()
    {
        _consoleInputWriter.Write("Hello world");
        _consoleInputStream.Seek(0, SeekOrigin.Begin);

        var lines = await _linesFromConsoleInputReader.Read(CancellationToken.None).ToListAsync();

        lines.Should().BeEquivalentTo(new []
        {
            "Hello world"
        });
    }

    [Theory]
    [InlineData("Hello\rWorld")]
    [InlineData("Hello\nWorld")]
    [InlineData("Hello\r\nWorld")]
    public async Task ShouldReadMultipleLines(string input)
    {
        _consoleInputWriter.Write(input);
        _consoleInputStream.Seek(0, SeekOrigin.Begin);

        var lines = await _linesFromConsoleInputReader.Read(CancellationToken.None).ToListAsync();

        lines.Should().BeEquivalentTo(new []
        {
            "Hello",
            "World"
        });
    }

    public void Dispose()
    {
        _consoleInputStream?.Dispose();
        _consoleInputReader?.Dispose();
    }
}