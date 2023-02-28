using System.Collections.Generic;
using System.IO;

namespace DcmOrganize;

public class DicomOrganizerOptions
{
    public IEnumerable<FileInfo>? Files { get; set; } = default!;

    public DirectoryInfo Directory { get; set; } = default!;

    public string Pattern { get; set; } = default!;

    public Action Action { get; set; } = default!;

    public int Parallelism { get; set; } = default!;

    public ErrorMode ErrorMode { get; set; } = default!;
}