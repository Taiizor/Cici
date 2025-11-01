namespace Cici.CLI.Services
{
    /// <summary>
    /// Provides file system operations for the CLI application.
    /// </summary>
    public class FileSystemService : IFileSystemService
    {
        /// <summary>
        /// Checks whether a file exists at the specified path.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns>True if the file exists; otherwise, false.</returns>
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        /// <summary>
        /// Checks whether a directory exists at the specified path.
        /// </summary>
        /// <param name="path">The directory path to check.</param>
        /// <returns>True if the directory exists; otherwise, false.</returns>
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        /// Creates a directory if it doesn't already exist.
        /// </summary>
        /// <param name="path">The directory path to ensure exists.</param>
        public void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Returns the absolute path for the specified path string.
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <returns>The absolute path.</returns>
        public string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Reads all text from the specified file.
        /// </summary>
        /// <param name="path">The file path to read from.</param>
        /// <returns>The contents of the file.</returns>
        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        /// <summary>
        /// Writes text to the specified file, creating it if necessary.
        /// </summary>
        /// <param name="path">The file path to write to.</param>
        /// <param name="content">The content to write to the file.</param>
        public void WriteAllText(string path, string content)
        {
            File.WriteAllText(path, content);
        }

        /// <summary>
        /// Returns the file paths that match the specified search pattern in the directory.
        /// </summary>
        /// <param name="path">The directory path to search in.</param>
        /// <param name="searchPattern">The search string to match against file names.</param>
        /// <returns>An array of file paths that match the search pattern.</returns>
        public string[] GetFiles(string path, string searchPattern)
        {
            return Directory.GetFiles(path, searchPattern);
        }
    }
}