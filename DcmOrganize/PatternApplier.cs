using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FellowOakDicom;

namespace DcmOrganize;

internal interface IPatternApplier
{
    string Apply(DicomDataset dicomDataset, string filePattern);
}

public class PatternApplier : IPatternApplier
{
    private readonly IDicomTagParser _dicomTagParser;
    private readonly IFolderNameCleaner _folderNameCleaner;

    public PatternApplier(IDicomTagParser dicomTagParser, IFolderNameCleaner folderNameCleaner)
    {
        _dicomTagParser = dicomTagParser ?? throw new ArgumentNullException(nameof(dicomTagParser));
        _folderNameCleaner = folderNameCleaner ?? throw new ArgumentNullException(nameof(folderNameCleaner));
    }
        
    public string Apply(DicomDataset dicomDataset, string filePattern)
    {
        var file = filePattern.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var openCurlyBraceIndex = file.IndexOf('{');
        var closingCurlyBraceIndex = file.IndexOf('}');
        var directorySeparatorIndex = file.LastIndexOf(Path.DirectorySeparatorChar);

        while (openCurlyBraceIndex != -1 && closingCurlyBraceIndex != -1)
        {
            var expression = file.Substring(openCurlyBraceIndex, closingCurlyBraceIndex - openCurlyBraceIndex).Trim('{', '}');
            var expressionTokens = new Stack<string>(
                expression
                    .Split("??", StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim())
                    .Reverse()
            );

            string? expressionValue = null;

            while (expressionTokens.TryPop(out var nextToken) && expressionValue == null)
            {
                if (string.Equals(nextToken, "Guid"))
                {
                    expressionValue = Guid.NewGuid().ToString();
                }
                else if (nextToken.StartsWith("'") && nextToken.EndsWith("'"))
                {
                    // Constant
                    expressionValue = nextToken.Trim('\''); 
                }
                else {
                    DicomTag dicomTag;
                    try
                    {
                        dicomTag = _dicomTagParser.Parse(nextToken);
                    }
                    catch (DicomTagParserException e)
                    {
                        throw new PatternException("Failed to parse DICOM tag while applying pattern", e);
                    }

                    expressionValue = dicomDataset.GetValueOrDefault(dicomTag, 0, (string?) null)?.Replace('^', ' ');
                }
            }

            if (expressionValue == null)
            {
                throw new PatternException($"DICOM tag expression '{expression}' is not present in DICOM dataset");
            }

            if (directorySeparatorIndex >= closingCurlyBraceIndex)
                expressionValue = _folderNameCleaner.Clean(expressionValue);

            file = file.Substring(0, openCurlyBraceIndex)
                   + expressionValue
                   + file.Substring(Math.Min(file.Length - 1, closingCurlyBraceIndex + 1));

            openCurlyBraceIndex = file.IndexOf('{');
            closingCurlyBraceIndex = file.IndexOf('}');
            directorySeparatorIndex = file.LastIndexOf(Path.DirectorySeparatorChar);
        }

        return file.Trim(Path.DirectorySeparatorChar);
    }
}