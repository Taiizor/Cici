namespace Cici.CLI.Services;

public class FileSystemService : IFileSystemService
{
    public bool FileExists(string path) => File.Exists(path);
    
    public bool DirectoryExists(string path) => Directory.Exists(path);
    
    public void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
    
    public string GetFullPath(string path) => Path.GetFullPath(path);
    
    public string ReadAllText(string path) => File.ReadAllText(path);
    
    public void WriteAllText(string path, string content) => File.WriteAllText(path, content);
    
    public string[] GetFiles(string path, string searchPattern) => Directory.GetFiles(path, searchPattern);
}
