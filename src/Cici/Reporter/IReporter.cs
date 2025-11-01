using Cici.Analyzer;
using Cici.Models;

namespace Cici.Reporter
{
    /// <summary>
    /// Defines the contract for report generation from flaky test analysis results.
    /// </summary>
    public interface IReporter
    {
        /// <summary>
        /// Generates a report from the analyzed test results.
        /// </summary>
        /// <param name="results">Collection of analyzed test results.</param>
        /// <param name="summary">Summary statistics and insights from the analysis.</param>
        /// <returns>A task representing the asynchronous report generation operation.</returns>
        Task GenerateReportAsync(IEnumerable<FlakyTestResult> results, FlakyDetectionSummary summary);
    }

    /// <summary>
    /// Defines the contract for reporters that save output to files.
    /// </summary>
    public interface IFileReporter : IReporter
    {
        /// <summary>
        /// Gets or sets the output file path where the report will be saved.
        /// </summary>
        string OutputPath { get; set; }
    }
}