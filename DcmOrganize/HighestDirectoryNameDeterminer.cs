using System;
using System.IO;
using System.Linq;

namespace DcmOrganize;

public static  class HighestDirectoryNameDeterminer
{
    public static string Determine(string fileName)
    {
        return fileName.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                   .Split(new [] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
                   .SkipLast(1)
                   .DefaultIfEmpty()
                   .FirstOrDefault()
               ?? string.Empty;
    } 
}