using Cici.Models;
using System.Reflection;

namespace Cici.Runner
{
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

        public async Task<IEnumerable<TestInfo>> DiscoverTestsAsync(string assemblyPath)
        {
            return await DiscoverTestsAsync(assemblyPath, null);
        }

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

        private bool IsAsyncMethod(MethodInfo method)
        {
            return method.ReturnType == typeof(Task) ||
                   (method.ReturnType.IsGenericType &&
                    method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
        }
    }
}