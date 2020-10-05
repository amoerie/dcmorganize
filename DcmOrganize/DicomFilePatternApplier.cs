using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dicom;

namespace DcmOrganize
{
    public static class DicomFilePatternApplier
    {
        public static bool TryApply(DicomDataset dicomDataset, string filePattern, out string? file)
        {
            file = filePattern.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
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
                    else
                    {
                        if (!DicomTagParser.TryParse(nextToken, out var dicomTag))
                        {
                            Console.Error.WriteLine($"ERROR: DICOM tag '{nextToken}' could not be parsed");
                            file = null;
                            return false;
                        }

                        expressionValue = dicomDataset.GetValueOrDefault(dicomTag, 0, (string?) null)?.Replace('^', ' ');
                    }
                }

                if (expressionValue == null)
                {
                    Console.Error.WriteLine($"ERROR: DICOM tag expression '{expression}' is not present in DICOM dataset");
                    file = null;
                    return false;
                }

                if (directorySeparatorIndex >= closingCurlyBraceIndex)
                    expressionValue = FolderNameCleaner.Clean(expressionValue);

                file = file.Substring(0, openCurlyBraceIndex)
                       + expressionValue
                       + file.Substring(Math.Min(file.Length - 1, closingCurlyBraceIndex + 1));

                openCurlyBraceIndex = file.IndexOf('{');
                closingCurlyBraceIndex = file.IndexOf('}');
                directorySeparatorIndex = file.LastIndexOf(Path.DirectorySeparatorChar);
            }

            file = file.Trim(Path.DirectorySeparatorChar);

            return true;
        }
    }
}