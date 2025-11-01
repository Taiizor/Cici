using Cici.Models;
using System.Reflection;

namespace Cici.Runner
{
    /// <summary>
    /// Service for discovering tests in .NET assemblies across multiple test frameworks.
    /// </summary>
    public class TestDiscoveryService : ITestDiscoveryService
    {
        private readonly Dictionary<string, TestFramework> _frameworkAttributes = new()
        {
            { "Xunit.FactAttribute", TestFramework.XUnit },
            { "Xunit.TheoryAttribute", TestFramework.XUnit },
            { "NUnit.Framework.TestAttribute", TestFramework.NUnit },
            { "NUnit.Framework.TestCaseAttribute", TestFramework.NUnit },
            { "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute", TestFramework.MSTest }
        };

        /// <summary>
        /// Discovers all tests in the specified assembly.
        /// </summary>
        /// <param name="assemblyPath">The path to the test assembly.</param>
        /// <returns>A task containing the collection of discovered tests.</returns>
        public async Task<IEnumerable<TestInfo>> DiscoverTestsAsync(string assemblyPath)
        {
            return await DiscoverTestsAsync(assemblyPath, null);
        }

        /// <summary>
        /// Discovers tests in the specified assembly with optional filtering.
        /// </summary>
        /// <param name="assemblyPath">The path to the test assembly.</param>
        /// <param name="filter">Optional filter to apply to test discovery.</param>
        /// <returns>A task containing the collection of discovered tests matching the filter.</returns>
        public async Task<IEnumerable<TestInfo>> DiscoverTestsAsync(string assemblyPath, string? filter)
        {
            return await Task.Run(() =>
            {
                List<TestInfo> tests = [];

                if (!File.Exists(assemblyPath))
                {
                    throw new FileNotFoundException($"Assembly not found: {assemblyPath}");
                }

                try
                {
                    Assembly assembly = Assembly.LoadFrom(assemblyPath);
                    Type[] types = assembly.GetTypes();

                    foreach (Type type in types)
                    {
                        if (type.IsAbstract || !type.IsClass)
                        {
                            continue;
                        }

                        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                        foreach (MethodInfo method in methods)
                        {
                            TestFramework testFramework = GetTestFramework(method);
                            if (testFramework == TestFramework.Unknown)
                            {
                                continue;
                            }

                            TestInfo testInfo = new()
                            {
                                FullName = $"{type.FullName}.{method.Name}",
                                ClassName = type.FullName ?? type.Name,
                                MethodName = method.Name,
                                AssemblyPath = assemblyPath,
                                Framework = testFramework,
                                Metadata = new Dictionary<string, object>
                                {
                                    ["DeclaringType"] = type.Name,
                                    ["Namespace"] = type.Namespace ?? string.Empty,
                                    ["IsAsync"] = IsAsyncMethod(method)
                                }
                            };

                            // Apply filter if provided
                            if (string.IsNullOrEmpty(filter) ||
                                testInfo.FullName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                            {
                                tests.Add(testInfo);
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Handle types that couldn't be loaded
                    IEnumerable<Type> loadedTypes = ex.Types.Where(t => t != null).Cast<Type>();
                    foreach (Type type in loadedTypes)
                    {
                        // Process loaded types if needed
                    }

                    if (tests.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"Could not load any tests from assembly: {assemblyPath}", ex);
                    }
                }

                return tests;
            });
        }

        /// <summary>
        /// Detects the test framework used by examining method attributes.
        /// </summary>
        /// <param name="method">The test method to examine.</param>
        /// <returns>The detected test framework.</returns>
        private TestFramework GetTestFramework(MethodInfo method)
        {
            object[] attributes = method.GetCustomAttributes(false);

            foreach (object attribute in attributes)
            {
                string? attributeTypeName = attribute.GetType().FullName;
                if (attributeTypeName == null)
                {
                    continue;
                }

                foreach (KeyValuePair<string, TestFramework> kvp in _frameworkAttributes)
                {
                    if (attributeTypeName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        return kvp.Value;
                    }
                }
            }

            return TestFramework.Unknown;
        }

        /// <summary>
        /// Determines whether a method is asynchronous.
        /// </summary>
        /// <param name="method">The method to check.</param>
        /// <returns>True if the method is asynchronous; otherwise, false.</returns>
        private static bool IsAsyncMethod(MethodInfo method)
        {
            return method.ReturnType == typeof(Task) ||
                   (method.ReturnType.IsGenericType &&
                    method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
        }
    }
}