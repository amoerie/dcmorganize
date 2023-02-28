using FellowOakDicom;
using FluentAssertions;
using Xunit;

namespace DcmOrganize.Tests;

public class TestsForDicomTagParser
{
    private readonly DicomTagParser _dicomTagParser;

    public TestsForDicomTagParser()
    {
        _dicomTagParser = new DicomTagParser();            
    }
        
    [Fact]
    public void ShouldParseTagByGroupAndElement()
    {
        var dicomTag = _dicomTagParser.Parse("(0008,0050)");
        dicomTag.Should().Be(DicomTag.AccessionNumber);
    }
        
    [Fact]
    public void ShouldParseTagByName()
    {
        var dicomTag = _dicomTagParser.Parse("AccessionNumber");
        dicomTag.Should().Be(DicomTag.AccessionNumber);
    }
        
    [Fact]
    public void ShouldNotParseUnknownTag()
    {
        _dicomTagParser.Invoking(p => p.Parse("Banana")).Should().Throw<DicomTagParserException>();
    }
}