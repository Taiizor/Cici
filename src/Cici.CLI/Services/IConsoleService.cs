namespace Cici.CLI.Services
{
    /// <summary>
    /// Defines the contract for console interaction services.
    /// </summary>
    public interface IConsoleService
    {
        /// <summary>
        /// Writes an informational message to the console.
        /// </summary>
        /// <param name="message">The message to display.</param>
        void WriteInfo(string message);

        /// <summary>
        /// Writes a success message to the console with appropriate formatting.
        /// </summary>
        /// <param name="message">The success message to display.</param>
        void WriteSuccess(string message);

        /// <summary>
        /// Writes a warning message to the console with appropriate formatting.
        /// </summary>
        /// <param name="message">The warning message to display.</param>
        void WriteWarning(string message);

        /// <summary>
        /// Writes an error message to the console with appropriate formatting.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        void WriteError(string message);

        /// <summary>
        /// Writes the application header/banner to the console.
        /// </summary>
        void WriteHeader();

        /// <summary>
        /// Prompts the user for input and returns the typed value.
        /// </summary>
        /// <typeparam name="T">The type of value to prompt for.</typeparam>
        /// <param name="message">The prompt message to display.</param>
        /// <returns>The user's input converted to the specified type.</returns>
        T Prompt<T>(string message);

        /// <summary>
        /// Prompts the user for a yes/no confirmation.
        /// </summary>
        /// <param name="message">The confirmation message to display.</param>
        /// <returns>True if the user confirms; otherwise, false.</returns>
        bool Confirm(string message);
    }
}