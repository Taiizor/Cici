namespace Cici.CLI.Services
{
    /// <summary>
    /// Defines the contract for file system operations.
    /// </summary>
    public interface IFileSystemService
    {
        /// <summary>
        /// Checks whether a file exists at the specified path.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns>True if the file exists; otherwise, false.</returns>
        bool FileExists(string path);

        /// <summary>
        /// Checks whether a directory exists at the specified path.
        /// </summary>
        /// <param name="path">The directory path to check.</param>
        /// <returns>True if the directory exists; otherwise, false.</returns>
        bool DirectoryExists(string path);

        /// <summary>
        /// Creates a directory if it doesn't already exist.
        /// </summary>
        /// <param name="path">The directory path to ensure exists.</param>
        void EnsureDirectoryExists(string path);

        /// <summary>
        /// Returns the absolute path for the specified path string.
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <returns>The absolute path.</returns>
        string GetFullPath(string path);

        /// <summary>
        /// Reads all text from the specified file.
        /// </summary>
        /// <param name="path">The file path to read from.</param>
        /// <returns>The contents of the file.</returns>
        string ReadAllText(string path);

        /// <summary>
        /// Writes text to the specified file, creating it if necessary.
        /// </summary>
        /// <param name="path">The file path to write to.</param>
        /// <param name="content">The content to write to the file.</param>
        void WriteAllText(string path, string content);

        /// <summary>
        /// Returns the file paths that match the specified search pattern in the directory.
        /// </summary>
        /// <param name="path">The directory path to search in.</param>
        /// <param name="searchPattern">The search string to match against file names.</param>
        /// <returns>An array of file paths that match the search pattern.</returns>
        string[] GetFiles(string path, string searchPattern);
    }
}