using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;
using NUnit.Framework;
using State.Fody;

public static class TestHelper
{
    public static string WeaveAssembly(string inputPath, string suffix, string config)
    {
        string outputPath = inputPath.Replace(".dll", suffix + ".dll");
        File.Copy(inputPath, outputPath, true);

        using (var moduleDefinition = ModuleDefinition.ReadModule(inputPath))
        {
            var weavingTask = new ModuleWeaver
            {
                ModuleDefinition = moduleDefinition
            };

            if (!string.IsNullOrEmpty(config))
            {
                var xElement = XElement.Parse(config);
                weavingTask.Config = xElement;
            }

            weavingTask.LogDebug = Log;
            weavingTask.LogInfo = Log;
            weavingTask.LogError = Log;

            weavingTask.Execute();
            moduleDefinition.Write(outputPath);
        }

        return outputPath;
    }

    static void Log(string log)
    {
        Console.WriteLine(log);
    }

    public static string CreateAssemblyForFiles(string outputAssembly, string inputFolder, params string[] inputFilenames)
    {
        var outputPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, outputAssembly + ".dll"));

        var syntaxTrees = new List<SyntaxTree>();
        foreach (var filename in inputFilenames)
        {
            var inputPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, $@"../../{inputFolder}/{filename}"));
            var sourceCode = File.ReadAllText(inputPath);

            // parse code
            var parseOptions = new CSharpParseOptions();
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(sourceCode, parseOptions));
        }

        // compile code
        var references = new[] {
            MetadataReference.CreateFromFile(typeof(string).GetTypeInfo().Assembly.Location),
            MetadataReference.CreateFromFile(typeof(AddStateAttribute).GetTypeInfo().Assembly.Location)
        };

        var options = new CSharpCompilationOptions(
            outputKind: Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: Microsoft.CodeAnalysis.OptimizationLevel.Release);

        var compilation = CSharpCompilation.Create(
            assemblyName: outputAssembly,
            references: references,
            options: options).AddSyntaxTrees(syntaxTrees);

        var result = compilation.Emit(outputPath);
        Assert.IsTrue(result.Success, result.ToString());

        return outputPath;
    }

    public static dynamic GetInstance(Assembly assembly, string className, params object[] args)
    {
        var type = assembly.GetType(className, true);
        return Activator.CreateInstance(type, args);
    }
}