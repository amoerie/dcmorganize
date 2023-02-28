using FluentAssertions;
using Xunit;

namespace DcmOrganize.Tests;

public class TestsForHighestDirectoryNameDeterminer
{
    [Theory]
    [InlineData(@"sub1/sub2/file.dcm", "sub1")]
    [InlineData(@"/sub1/sub2/file.dcm", "sub1")]
    [InlineData(@"sub1/file.dcm", "sub1")]
    [InlineData(@"/sub1/file.dcm", "sub1")]
    [InlineData(@"file.dcm", "")]
    public void ShouldDetermineCorrectHighestDirectory(string filePath, string expectedHighestDirectoryName)
    {
        // Act
        var highestDirectoryName = HighestDirectoryNameDeterminer.Determine(filePath);
            
        // Assert
        highestDirectoryName.Should().Be(expectedHighestDirectoryName);
    }
}