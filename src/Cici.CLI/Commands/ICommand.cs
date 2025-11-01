namespace Cici.CLI.Commands
{
    /// <summary>
    /// Defines the contract for CLI commands.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of the command.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Executes the command with the specified arguments.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Exit code where 0 indicates success.</returns>
        Task<int> ExecuteAsync(string[] args);
    }
}