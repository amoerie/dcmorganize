using System;
using System.IO;
using FellowOakDicom;
using FluentAssertions;
using Xunit;

namespace DcmOrganize.Tests;

public class TestsForPatternApplier
{
    private readonly PatternApplier _patternApplier;

    public TestsForPatternApplier()
    {
        var dicomTagParser = new DicomTagParser();
        var folderNameCleaner = new FolderNameCleaner();
        _patternApplier = new PatternApplier(dicomTagParser, folderNameCleaner);
    }
        
    [Fact]
    public void ShouldApplySimplePattern()
    {
        // Arrange
        var dicomDataSet = new DicomDataset
        {
            { DicomTag.AccessionNumber, "ABC123" },
            { DicomTag.InstanceNumber, "7" },
        };
        var pattern = "{AccessionNumber}/{InstanceNumber}.dcm";

        // Act
        var file = _patternApplier.Apply(dicomDataSet, pattern);

        // Assert
        file.Should().Be(Path.Join("ABC123", "7.dcm"));
    }
        
    [Fact]
    public void ShouldApplyComplexPattern()
    {
        // Arrange
        var dicomDataSet = new DicomDataset
        {
            { DicomTag.PatientName, "Samson^Gert" },
            { DicomTag.AccessionNumber, "ABC123" },
            { DicomTag.SeriesNumber, "20" },
            { DicomTag.InstanceNumber, "7" },
        };
        var pattern = "Patient {PatientName}/Study {AccessionNumber}/Series {SeriesNumber}/Image {InstanceNumber}.dcm";

        // Act
        var file = _patternApplier.Apply(dicomDataSet, pattern);

        // Assert
        file.Should().Be(Path.Join("Patient Samson Gert", "Study ABC123", "Series 20", "Image 7.dcm"));
    }
        
    [Fact]
    public void ShouldUseValueWhenPatternContainsFallbackAndValueIsPresent()
    {
        // Arrange
        var dicomDataSet = new DicomDataset
        {
            { DicomTag.SOPInstanceUID, "1.2.3" },
            { DicomTag.InstanceNumber, "10" },
        };
        var pattern = "{InstanceNumber ?? SOPInstanceUID}.dcm";

        // Act
        var file = _patternApplier.Apply(dicomDataSet, pattern);

        // Assert
        file.Should().Be("10.dcm");
    }
        
    [Fact]
    public void ShouldUseFallbackWhenPatternContainsFallbackAndValueIsNotPresent()
    {
        // Arrange
        var dicomDataSet = new DicomDataset
        {
            { DicomTag.SOPInstanceUID, "1.2.3" },
        };
        var pattern = "{InstanceNumber ?? SOPInstanceUID}.dcm";

        // Act
        var file = _patternApplier.Apply(dicomDataSet, pattern);

        // Assert
        file.Should().Be("1.2.3.dcm");
    }
        
    [Fact]
    public void ShouldSupportGuidsInFilePattern()
    {
        // Arrange
        var dicomDataSet = new DicomDataset
        {
            { DicomTag.SOPInstanceUID, "1.2.3" },
        };
        var pattern = "{Guid}.dcm";

        // Act
        var file = _patternApplier.Apply(dicomDataSet, pattern);

        // Assert
        var guidAsString = file!.Substring(0, file.Length - ".dcm".Length);

        Guid.TryParse(guidAsString, out var _).Should().BeTrue();
    }
        
    [Fact]
    public void ShouldThrowExceptionWhenAnErrorOccurs()
    {
        // Arrange
        var dicomDataSet = new DicomDataset
        {
            { DicomTag.SOPInstanceUID, "1.2.3" },
        };
        var pattern = "{Banana}.dcm";

        // Act
        _patternApplier.Invoking(p => p.Apply(dicomDataSet, pattern)).Should().Throw<PatternException>();
    }
        
    [Fact]
    public void ShouldSupportConstantsAsFallback()
    {
        // Arrange
        var dicomDataSet = new DicomDataset
        {
            { DicomTag.SOPInstanceUID, "1.2.3" },
        };
        var pattern = "{InstanceNumber ?? 'Constant'}.dcm";

        // Act
        _patternApplier.Apply(dicomDataSet, pattern).Should().Be("Constant.dcm");
    }
}