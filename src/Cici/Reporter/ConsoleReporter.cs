using Cici.Analyzer;
using Cici.Models;

namespace Cici.Reporter;

public class ConsoleReporter : IReporter
{
    public async Task GenerateReportAsync(IEnumerable<FlakyTestResult> results, FlakyDetectionSummary summary)
    {
        await Task.Run(() =>
        {
            Console.WriteLine();
            PrintHeader();
            PrintSummary(summary);
            PrintFlakyTests(results.Where(r => r.IsFlaky));
            PrintStableTests(results.Where(r => !r.IsFlaky && r.PassRate == 1.0));
            PrintAlwaysFailingTests(results.Where(r => r.PassRate == 0));
            
            if (summary.ErrorPatterns.Any())
            {
                PrintErrorPatterns(summary.ErrorPatterns);
            }
            
            PrintFooter(summary);
        });
    }

    private void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    CICI - Flaky Test Report                    ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    private void PrintSummary(FlakyDetectionSummary summary)
    {
        Console.WriteLine("📊 Test Analysis Summary:");
        Console.WriteLine("────────────────────────");
        
        Console.Write("  Total Tests Analyzed: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(summary.TotalTests);
        Console.ResetColor();
        
        Console.Write("  ✅ Stable Tests: ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{summary.StableTests} ({GetPercentage(summary.StableTests, summary.TotalTests):F1}%)");
        Console.ResetColor();
        
        Console.Write("  ⚠️  Flaky Tests: ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{summary.FlakyTests} ({GetPercentage(summary.FlakyTests, summary.TotalTests):F1}%)");
        Console.ResetColor();
        
        Console.Write("  ❌ Always Failing: ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{summary.AlwaysFailingTests} ({GetPercentage(summary.AlwaysFailingTests, summary.TotalTests):F1}%)");
        Console.ResetColor();
        
        Console.WriteLine($"  ⏱️  Total Execution Time: {FormatDuration(summary.TotalExecutionTime)}");
        Console.WriteLine();
    }

    private void PrintFlakyTests(IEnumerable<FlakyTestResult> flakyTests)
    {
        var flakyList = flakyTests.ToList();
        if (!flakyList.Any()) return;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠️  Flaky Tests Detected ({flakyList.Count}):");
        Console.ResetColor();
        Console.WriteLine("─────────────────────────────");
        
        int index = 1;
        foreach (var test in flakyList.OrderBy(t => t.PassRate))
        {
            Console.Write($"  {index}. ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(TruncateTestName(test.Test.FullName, 50));
            Console.ResetColor();
            
            Console.Write(" ");
            PrintPassFailBar(test.PassedCount, test.FailedCount);
            
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($" ({test.PassRate * 100:F0}% pass rate)");
            Console.ResetColor();
            
            if (test.MaxDuration - test.MinDuration > TimeSpan.FromSeconds(1))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"     Duration variance: {test.MinDuration.TotalMilliseconds:F0}ms - {test.MaxDuration.TotalMilliseconds:F0}ms");
                Console.ResetColor();
            }
            
            index++;
        }
        Console.WriteLine();
    }

    private void PrintStableTests(IEnumerable<FlakyTestResult> stableTests)
    {
        var stableList = stableTests.Take(5).ToList();
        if (!stableList.Any()) return;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ Stable Tests (showing {stableList.Count} of {stableTests.Count()}):");
        Console.ResetColor();
        Console.WriteLine("──────────────────────────");
        
        foreach (var test in stableList)
        {
            Console.Write("  • ");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(TruncateTestName(test.Test.FullName, 60));
            Console.ResetColor();
        }
        Console.WriteLine();
    }

    private void PrintAlwaysFailingTests(IEnumerable<FlakyTestResult> failingTests)
    {
        var failingList = failingTests.ToList();
        if (!failingList.Any()) return;

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Always Failing Tests ({failingList.Count}):");
        Console.ResetColor();
        Console.WriteLine("────────────────────────────");
        
        foreach (var test in failingList.Take(5))
        {
            Console.Write("  • ");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write(TruncateTestName(test.Test.FullName, 50));
            Console.ResetColor();
            
            // Show common error if available
            var commonError = test.ExecutionResults
                .Where(r => !string.IsNullOrEmpty(r.ErrorMessage))
                .GroupBy(r => r.ErrorMessage)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;
            
            if (!string.IsNullOrEmpty(commonError))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"\n     Error: {TruncateTestName(commonError!, 60)}");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine();
            }
        }
        Console.WriteLine();
    }

    private void PrintErrorPatterns(Dictionary<string, int> errorPatterns)
    {
        Console.WriteLine("🔍 Common Error Patterns:");
        Console.WriteLine("──────────────────────");
        
        foreach (var pattern in errorPatterns.Take(5))
        {
            Console.Write("  • ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"{pattern.Key}: ");
            Console.ResetColor();
            Console.WriteLine($"{pattern.Value} occurrences");
        }
        Console.WriteLine();
    }

    private void PrintFooter(FlakyDetectionSummary summary)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("──────────────────────────────────────────────────────────────");
        Console.ResetColor();
        
        if (summary.FlakyTests > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"💡 Recommendation: Review and fix the {summary.FlakyTests} flaky test(s) to improve CI reliability.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✨ Great! No flaky tests detected. Your test suite is stable!");
            Console.ResetColor();
        }
    }

    private void PrintPassFailBar(int passed, int failed)
    {
        const int barLength = 10;
        int passedBars = (int)Math.Round((double)passed / (passed + failed) * barLength);
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("✅" + new string('█', passedBars));
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(new string('█', barLength - passedBars) + "❌");
        Console.ResetColor();
    }

    private string TruncateTestName(string name, int maxLength)
    {
        if (name.Length <= maxLength)
            return name;
        
        // Try to truncate at a meaningful boundary
        var parts = name.Split('.');
        if (parts.Length > 2)
        {
            var shortened = $"...{parts[^2]}.{parts[^1]}";
            if (shortened.Length <= maxLength)
                return shortened;
        }
        
        return "..." + name.Substring(name.Length - maxLength + 3);
    }

    private double GetPercentage(int part, int total)
    {
        return total > 0 ? (double)part / total * 100 : 0;
    }

    private string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{duration.TotalHours:F1}h";
        if (duration.TotalMinutes >= 1)
            return $"{duration.TotalMinutes:F1}m";
        return $"{duration.TotalSeconds:F1}s";
    }
}
