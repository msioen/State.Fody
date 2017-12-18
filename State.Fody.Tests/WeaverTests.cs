using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;
using NUnit.Framework;
using State.Fody;

[TestFixture]
public class WeaverTests
{
    Assembly assembly;
    string newAssemblyPath;
    string assemblyPath;

    [OneTimeSetUp]
    public void Setup()
    {
        var validFileNames = Directory.GetFiles(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../ValidAssemblyFiles/"));
        var strippedFileNames = validFileNames.Select(x => Path.GetFileName(x)).ToArray();

        assemblyPath = CreateAssemblyForFiles(
            outputAssembly: "AssemblyToProcess",
            inputFolder: "ValidAssemblyFiles",
            inputFilenames: strippedFileNames);

        newAssemblyPath = WeaveAssembly(assemblyPath);
        assembly = Assembly.LoadFile(newAssemblyPath);
    }

    [Test]
    public void TestValidity()
    {
        Verifier.Verify(assemblyPath, newAssemblyPath);
    }

    [Test]
    public void TestPropertyCreation()
    {
        var instance = GetInstance(assembly, "Default");
        var type = (Type)instance.GetType();
        var properties = type.GetProperties();
        Assert.AreEqual(2, properties.Length);
        Assert.IsTrue(properties.FirstOrDefault(x => x.Name == "IsTesting") != null);
    }

    [Test]
    public void TestPropertyCreationInheritance()
    {
        var baseInstance = GetInstance(assembly, "BaseClass");
        var baseType = (Type)baseInstance.GetType();
        var baseProperties = baseType.GetProperties();
        Assert.AreEqual(2, baseProperties.Length);

        var subInstance = GetInstance(assembly, "SubClass");
        var subType = (Type)subInstance.GetType();
        var subProperties = subType.GetProperties();
        Assert.AreEqual(4, subProperties.Length);
    }

    [Test]
    public void TestStateChange()
    {
        var instance = GetInstance(assembly, "StateChange");
        Assert.AreEqual(0, instance.SetterCounter);
        instance.Test();
        Assert.AreEqual(2, instance.SetterCounter);
    }

    [Test]
    public void TestStateChangeSubcalls()
    {
        var instance = GetInstance(assembly, "StateChange");
        Assert.AreEqual(0, instance.SetterCounter);
        instance.TestSubcalls();
        Assert.AreEqual(2, instance.SetterCounter);
    }

    [Test]
    public async Task TestAsyncStateChange()
    {
        var instance = GetInstance(assembly, "StateChange");
        Assert.AreEqual(0, instance.SetterCounter);
        var task = (Task)instance.TestAsync();
        await task.ContinueWith(t =>
        {
            Assert.AreEqual(2, instance.SetterCounter);
        });
    }

    [Test]
    public async Task TestAsyncStateChangeSubcalls()
    {
        var instance = GetInstance(assembly, "StateChange");
        Assert.AreEqual(0, instance.SetterCounter);
        var task = (Task)instance.TestAsyncSubcalls();
        await task.ContinueWith(t =>
        {
            Assert.AreEqual(2, instance.SetterCounter);
        });
    }

    [Test]
    public void TestInvalidPropertySetter()
    {
        var inputAssemblyPath = CreateAssemblyForFiles("InvalidPropertySetter", "FailingAssemblyFiles", "InvalidPropertySetter.cs");
        var exception = Assert.Throws<WeavingException>(() => WeaveAssembly(inputAssemblyPath));
        Assert.AreEqual(EWeavingError.InvalidPropertySetter, exception.Error);
    }

    [Test]
    public void TestInvalidPropertySetter2()
    {
        var inputAssemblyPath = CreateAssemblyForFiles("InvalidPropertySetter2", "FailingAssemblyFiles", "InvalidPropertySetter2.cs");
        var exception = Assert.Throws<WeavingException>(() => WeaveAssembly(inputAssemblyPath));
        Assert.AreEqual(EWeavingError.InvalidPropertySetter, exception.Error);
    }

    [Test]
    public void TestInvalidPropertyType()
    {
        var inputAssemblyPath = CreateAssemblyForFiles("InvalidPropertyType", "FailingAssemblyFiles", "InvalidPropertyType.cs");
        var exception = Assert.Throws<WeavingException>(() => WeaveAssembly(inputAssemblyPath));
        Assert.AreEqual(EWeavingError.InvalidPropertyType, exception.Error);
    }

    [Test]
    public void TestInvalidInstancePropertyInStaticMethod()
    {
        var inputAssemblyPath = CreateAssemblyForFiles("InvalidInstancePropertyInStaticMethod", "FailingAssemblyFiles", "InvalidInstancePropertyInStaticMethod.cs");
        var exception = Assert.Throws<WeavingException>(() => WeaveAssembly(inputAssemblyPath));
        Assert.AreEqual(EWeavingError.InstancePropertyWithStaticMethod, exception.Error);
    }

    [Test]
    public void TestInvalidInstanceFieldInStaticMethod()
    {
        var inputAssemblyPath = CreateAssemblyForFiles("InvalidInstanceFieldInStaticMethod", "FailingAssemblyFiles", "InvalidInstanceFieldInStaticMethod.cs");
        var exception = Assert.Throws<WeavingException>(() => WeaveAssembly(inputAssemblyPath));
        Assert.AreEqual(EWeavingError.InstanceFieldWithStaticMethod, exception.Error);
    }

    string GetAssemblyPath(string assemblyName)
    {
        var projectPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, $@"../../../{assemblyName}/bin/Debug/"));
        var asPath = Path.Combine(Path.GetDirectoryName(projectPath), $@"{assemblyName}.dll");
#if (!DEBUG)
        asPath = asPath.Replace("Debug", "Release");
#endif
        return asPath;
    }

    string WeaveAssembly(string inputPath)
    {
        string outputPath = inputPath.Replace(".dll", "2.dll");
        File.Copy(inputPath, outputPath, true);

        using (var moduleDefinition = ModuleDefinition.ReadModule(inputPath))
        {
            var weavingTask = new ModuleWeaver
            {
                ModuleDefinition = moduleDefinition
            };

            weavingTask.LogDebug = Log;
            weavingTask.LogInfo = Log;
            weavingTask.LogError = Log;

            weavingTask.Execute();
            moduleDefinition.Write(outputPath);
        }

        return outputPath;
    }

    void Log(string log)
    {
        Console.WriteLine(log);
    }

    static dynamic GetInstance(Assembly assembly, string className, params object[] args)
    {
        var type = assembly.GetType(className, true);
        return Activator.CreateInstance(type, args);
    }

    static string CreateAssemblyForFiles(string outputAssembly, string inputFolder, params string[] inputFilenames)
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
}