namespace Cici.CLI.Options
{
    /// <summary>
    /// Configuration options for the run command.
    /// </summary>
    public class RunOptions
    {
        /// <summary>
        /// Gets or sets the path to the test assembly to analyze.
        /// </summary>
        public string AssemblyPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the test filter to apply when discovering tests.
        /// </summary>
        public string? Filter { get; set; }

        /// <summary>
        /// Gets or sets the number of times to run each test. Default is 10.
        /// </summary>
        public int RepeatCount { get; set; } = 10;

        /// <summary>
        /// Gets or sets the report formats to generate. Default is console only.
        /// </summary>
        public List<string> ReportFormats { get; set; } = ["console"];

        /// <summary>
        /// Gets or sets the output directory for report files.
        /// </summary>
        public string? OutputDirectory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to run test iterations in parallel.
        /// </summary>
        public bool Parallel { get; set; }

        /// <summary>
        /// Gets or sets the timeout in seconds for each test execution. Default is 30.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets a value indicating whether to enable verbose logging.
        /// </summary>
        public bool Verbose { get; set; }
    }
}