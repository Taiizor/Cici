# 🎯 Cici - Flaky Test Detector for .NET

<div align="center">

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-v1.0.0-blue)](https://www.nuget.org)

**Cici** is a powerful flaky test detection tool for .NET projects that helps you identify unreliable tests in your test suite.

</div>

## ✨ Features

- 🔍 **Automatic Test Discovery** - Supports xUnit, NUnit, and MSTest frameworks
- 🔄 **Multiple Test Executions** - Run each test multiple times to detect flakiness
- 📊 **Detailed Analytics** - Get comprehensive reports on test stability
- 🎨 **Beautiful Console Output** - Color-coded results with progress indicators
- 📄 **Multiple Report Formats** - Console, JSON, and HTML reports
- ⚡ **Parallel Execution** - Speed up analysis with parallel test runs
- 🎯 **Smart Error Categorization** - Automatically categorizes common error patterns

## 🚀 Quick Start

### Installation

#### As a .NET Global Tool

```bash
dotnet tool install --global Cici.CLI
```

#### As a NuGet Package

```bash
dotnet add package Cici
```

### Basic Usage

```bash
# Analyze all tests in an assembly
cici run --assembly MyTests.dll

# Run each test 20 times
cici run --assembly MyTests.dll --repeat 20

# Filter specific tests
cici run --assembly MyTests.dll --filter "UserTests"

# Generate JSON report
cici run --assembly MyTests.dll --report json --output ./reports
```

## 📋 Example Output

```
╔══════════════════════════════════════════════════════════════╗
║                    CICI - Flaky Test Report                    ║
╚══════════════════════════════════════════════════════════════╝

📊 Test Analysis Summary:
────────────────────────
  Total Tests Analyzed: 25
  ✅ Stable Tests: 18 (72.0%)
  ⚠️  Flaky Tests: 4 (16.0%)
  ❌ Always Failing: 3 (12.0%)
  ⏱️  Total Execution Time: 2.3m

⚠️  Flaky Tests Detected (4):
─────────────────────────────
  1. MyApp.Tests.Api.UserTests.Login ✅███████▓▓▓❌ (70% pass rate)
     Duration variance: 234ms - 1823ms
  2. MyApp.Tests.Async.QueueTests.Enqueue ✅████████▓▓❌ (80% pass rate)
  3. MyApp.Tests.Data.CacheTests.GetItem ✅██████▓▓▓▓❌ (60% pass rate)
  4. MyApp.Tests.Network.ApiTests.GetData ✅█████████▓❌ (90% pass rate)

🔍 Common Error Patterns:
──────────────────────
  • Timeout: 8 occurrences
  • Network/Connection Issue: 3 occurrences
  • Concurrency Issue: 2 occurrences
```

## 🏗️ Architecture

```
Cici/
├── src/
│   ├── Cici/                    # Core library
│   │   ├── Runner/              # Test discovery & execution
│   │   ├── Analyzer/            # Flaky test analysis
│   │   ├── Reporter/            # Report generation
│   │   └── Models/              # Data models
│   └── Cici.CLI/                # Command-line interface
├── tests/
│   └── Cici.Tests/              # Unit tests
└── samples/
    └── SampleTests/             # Example test project
```

## 🔧 Configuration

### Programmatic Usage

```csharp
using Cici;
using Cici.Runner;
using Cici.Analyzer;
using Cici.Reporter;

var runner = new CiciRunner();
var options = new CiciRunOptions
{
    AssemblyPath = "MyTests.dll",
    RepeatCount = 10,
    ParallelExecution = true,
    TestFilter = "Integration"
};

var result = await runner.RunAsync(options);

if (result.FlakyTests > 0)
{
    Console.WriteLine($"Found {result.FlakyTests} flaky tests!");
}
```

## 🤔 Understanding Flaky Tests

A test is considered **flaky** when it:
- Sometimes passes and sometimes fails without code changes
- Has inconsistent execution times
- Depends on external factors (network, filesystem, timing)
- Has race conditions or concurrency issues

### Common Causes of Flaky Tests

| Cause | Description | Example |
|-------|-------------|---------|
| **Timing Issues** | Tests that depend on specific timing | `Thread.Sleep()`, timeouts |
| **Shared State** | Tests that don't properly isolate state | Static variables, singletons |
| **External Dependencies** | Tests that rely on external services | API calls, databases |
| **Concurrency** | Race conditions in multi-threaded code | Parallel operations |
| **Resource Contention** | Competition for system resources | File locks, ports |

## 🛠️ Development

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code

### Building from Source

```bash
# Clone the repository
git clone https://github.com/Taiizor/Cici.git
cd cici

# Build the solution
dotnet build

# Run tests
dotnet test

# Pack NuGet packages
dotnet pack
```

### Running Sample Tests

```bash
# Build sample tests
dotnet build samples/SampleTests

# Run Cici on sample tests
dotnet run --project src/Cici.CLI -- run --assembly samples/SampleTests/bin/Debug/net8.0/SampleTests.dll
```

## 📊 CI/CD Integration

### GitHub Actions

```yaml
- name: Run Flaky Test Detection
  run: |
    dotnet tool install --global Cici.CLI
    cici run --assembly MyTests.dll --report json --output ./test-results
    
- name: Upload Test Results
  uses: actions/upload-artifact@v4
  with:
    name: flaky-test-report
    path: ./test-results/flaky-report.json
```

### Azure DevOps

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Install Cici'
  inputs:
    command: custom
    custom: tool
    arguments: 'install --global Cici.CLI'

- script: cici run --assembly $(Build.SourcesDirectory)/MyTests.dll --report json
  displayName: 'Detect Flaky Tests'
```

## 🤝 Contributing

Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md) for details.

### Areas for Contribution

- [ ] HTML report generator with charts
- [ ] Integration with test frameworks
- [ ] Machine learning for flaky test prediction
- [ ] Historical trend analysis
- [ ] VS Code extension
- [ ] Visual Studio extension

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by Google's flaky test detection practices
- Built with love for the .NET community
- Special thanks to all contributors

## 📬 Contact & Support

- **Issues**: [GitHub Issues](https://github.com/Taiizor/Cici/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Cici/discussions)
- **Twitter**: [@cici_flaky](https://twitter.com/cici_flaky)

---

<div align="center">
Made with ❤️ for reliable testing
</div>
