using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FellowOakDicom;

namespace DcmOrganize;

public interface IDicomTagParser
{
    DicomTag Parse(string dicomTagAsString);
}

public class DicomTagParser : IDicomTagParser
{
    private static readonly Lazy<IEnumerable<FieldInfo>> DicomTagFields = new Lazy<IEnumerable<FieldInfo>>(
        () => typeof(DicomTag)
            .GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.FieldType == typeof(DicomTag))
            .ToList()
    );

    public DicomTag Parse(string dicomTagAsString)
    {
        try
        {
            // hex syntax 
            if (dicomTagAsString[0] == '(' || char.IsDigit(dicomTagAsString[0]))
            {
                return DicomTag.Parse(dicomTagAsString);
            }

            var field = DicomTagFields.Value
                .FirstOrDefault(f => string.Equals(f.Name, dicomTagAsString));

            if (field != null)
            {
                return (DicomTag?) field.GetValue(null)! ?? throw new DicomTagParserException($"Invalid DICOM tag '{dicomTagAsString}'");
            }

            return DicomTag.Parse(dicomTagAsString);
        }
        catch (DicomDataException e)
        {
            throw new DicomTagParserException($"Invalid DICOM tag '{dicomTagAsString}': " + e.Message, e);
        }
    }
}