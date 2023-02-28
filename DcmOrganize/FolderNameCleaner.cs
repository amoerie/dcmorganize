namespace DcmOrganize;

public interface IFolderNameCleaner
{
    string Clean(string folderName);
}

public class FolderNameCleaner : IFolderNameCleaner
{
    private static readonly char[] CharsToRemove = { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
    private static readonly char[] CharsToTrim = { '.', ' ' };

    public string Clean(string folderName)
    {
        return string.Join("", folderName.Split(CharsToRemove)).Trim(CharsToTrim);
    }
}