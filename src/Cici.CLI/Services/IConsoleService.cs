namespace Cici.CLI.Services
{
    public interface IConsoleService
    {
        void WriteInfo(string message);
        void WriteSuccess(string message);
        void WriteWarning(string message);
        void WriteError(string message);
        void WriteHeader();
        T Prompt<T>(string message);
        bool Confirm(string message);
    }
}