using Cici.CLI.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cici.CLI.Services
{
    public class CommandExecutor(IServiceProvider serviceProvider, ILogger<CommandExecutor> logger, IConsoleService console)
    {
        public async Task<int> ExecuteAsync(string[] args)
        {
            try
            {
                // Parse command name
                string commandName = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

                // Handle help flags
                if (commandName is "--help" or "-h" or "-?")
                {
                    commandName = "help";
                }

                // Handle version flags
                if (commandName is "--version" or "-v")
                {
                    commandName = "version";
                }

                // Get all available commands
                IEnumerable<ICommand> commands = serviceProvider.GetServices<ICommand>();
                ICommand? command = commands.FirstOrDefault(c => c.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));

                if (command == null)
                {
                    console.WriteError($"Unknown command: '{commandName}'");
                    console.WriteInfo("Use 'cici help' to see available commands.");
                    return 1;
                }

                // Remove command name from args for command execution
                string[] commandArgs = args.Length > 1 ? args.Skip(1).ToArray() : Array.Empty<string>();

                logger.LogDebug("Executing command: {CommandName} with {ArgCount} arguments", commandName, commandArgs.Length);

                return await command.ExecuteAsync(commandArgs);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception during command execution");
                console.WriteError($"An unexpected error occurred: {ex.Message}");

                if (ex.InnerException != null)
                {
                    console.WriteError($"Inner exception: {ex.InnerException.Message}");
                }

                console.WriteInfo("For more details, run with --verbose flag.");
                return 1;
            }
        }
    }
}