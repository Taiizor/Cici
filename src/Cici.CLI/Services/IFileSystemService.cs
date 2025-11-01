namespace Cici.CLI.Services;

public interface IFileSystemService
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    void EnsureDirectoryExists(string path);
    string GetFullPath(string path);
    string ReadAllText(string path);
    void WriteAllText(string path, string content);
    string[] GetFiles(string path, string searchPattern);
}
