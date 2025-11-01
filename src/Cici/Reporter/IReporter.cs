using Cici.Analyzer;
using Cici.Models;

namespace Cici.Reporter
{
    public interface IReporter
    {
        Task GenerateReportAsync(IEnumerable<FlakyTestResult> results, FlakyDetectionSummary summary);
    }

    public interface IFileReporter : IReporter
    {
        string OutputPath { get; set; }
    }
}
