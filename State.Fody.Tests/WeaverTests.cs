using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using NUnit.Framework;

namespace State.Fody.Tests
{
    [TestFixture]
    public class WeaverTests
    {
        Assembly assembly;
        string newAssemblyPath;
        string assemblyPath;

        [OneTimeSetUp]
        public void Setup()
        {
            var projectPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, @"../../../AssemblyToProcess/bin/Debug/"));
            assemblyPath = Path.Combine(Path.GetDirectoryName(projectPath), @"AssemblyToProcess.dll");
#if (!DEBUG)
        assemblyPath = assemblyPath.Replace("Debug", "Release");
#endif

            newAssemblyPath = assemblyPath.Replace(".dll", "2.dll");
            File.Copy(assemblyPath, newAssemblyPath, true);

            using (var moduleDefinition = ModuleDefinition.ReadModule(assemblyPath))
            {
                var weavingTask = new ModuleWeaver
                {
                    ModuleDefinition = moduleDefinition
                };

                weavingTask.LogDebug = Log;
                weavingTask.LogInfo = Log;
                weavingTask.LogError = Log;

                weavingTask.Execute();
                moduleDefinition.Write(newAssemblyPath);
            }

            assembly = Assembly.LoadFile(newAssemblyPath);


        }

        void Log(string log)
        {
            Console.WriteLine(log);
        }

        [Test]
        public void TestMethod()
        {

        }
    }
}
