using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mono.Cecil;
using NUnit.Framework;
using State.Fody;

[TestFixture]
public class WeaverTests
{
    Assembly assembly;

    [OneTimeSetUp]
    public void Setup()
    {
        var projectPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, @"../../../AssemblyToProcess/bin/Debug/"));
        var assemblyPath = Path.Combine(Path.GetDirectoryName(projectPath), @"AssemblyToProcess.dll");
#if (!DEBUG)
            assemblyPath = assemblyPath.Replace("Debug", "Release");
#endif

        var newAssemblyPath = WeaveAssembly(assemblyPath, false);
        assembly = Assembly.LoadFile(newAssemblyPath);
    }

    [Test]
    public void TestValidity()
    {
        //Verifier.Verify(assemblyPath, newAssemblyPath);
    }

    [Test]
    public void TestPropertyCreation()
    {
        var instance = GetInstance(assembly, "AssemblyToProcess.Default");
        var type = (Type)instance.GetType();
        var properties = type.GetProperties();
        Assert.AreEqual(2, properties.Length);
        Assert.IsTrue(properties.FirstOrDefault(x => x.Name == "IsTesting") != null);
    }

    [Test]
    public void TestPropertyCreationInheritance()
    {
        var baseInstance = GetInstance(assembly, "AssemblyToProcess.BaseClass");
        var baseType = (Type)baseInstance.GetType();
        var baseProperties = baseType.GetProperties();
        Assert.AreEqual(2, baseProperties.Length);

        var subInstance = GetInstance(assembly, "AssemblyToProcess.SubClass");
        var subType = (Type)subInstance.GetType();
        var subProperties = subType.GetProperties();
        Assert.AreEqual(4, subProperties.Length);
    }

    [Test]
    public void TestStateChange()
    {
        var instance = GetInstance(assembly, "AssemblyToProcess.StateChange");
        Assert.AreEqual(0, instance.SetterCounter);
        instance.Test();
        Assert.AreEqual(2, instance.SetterCounter);
    }

    [Test]
    public void TestStateChangeSubcalls()
    {
        var instance = GetInstance(assembly, "AssemblyToProcess.StateChange");
        Assert.AreEqual(0, instance.SetterCounter);
        instance.TestSubcalls();
        Assert.AreEqual(2, instance.SetterCounter);
    }

    [Test]
    public async Task TestAsyncStateChange()
    {
        var instance = GetInstance(assembly, "AssemblyToProcess.StateChange");
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
        var instance = GetInstance(assembly, "AssemblyToProcess.StateChange");
        Assert.AreEqual(0, instance.SetterCounter);
        var task = (Task)instance.TestAsyncSubcalls();
        await task.ContinueWith(t =>
        {
            Assert.AreEqual(2, instance.SetterCounter);
        });
    }

    [Test]
    public void TestInvalidPropertyType()
    {
        var inputAssembly = CreateAssemblyForFile("FailingAssemblyFiles", "InvalidPropertyType");
        var exception = Assert.Throws<WeavingException>(() => WeaveAssembly(inputAssembly.Location, true));
        Assert.AreEqual(EWeavingError.InvalidPropertyType, exception.Error);
    }

    [Test]
    public void TestInvalidPropertySetter()
    {
        var inputAssembly = CreateAssemblyForFile("FailingAssemblyFiles", "InvalidPropertySetter");
        var exception = Assert.Throws<WeavingException>(() => WeaveAssembly(inputAssembly.Location, true));
        Assert.AreEqual(EWeavingError.InvalidPropertySetter, exception.Error);
    }

    [Test]
    public void TestInvalidInstancePropertyInStaticMethod()
    {
        var inputAssembly = CreateAssemblyForFile("FailingAssemblyFiles", "InvalidInstancePropertyInStaticMethod");
        var exception = Assert.Throws<WeavingException>(() => WeaveAssembly(inputAssembly.Location, true));
        Assert.AreEqual(EWeavingError.InstancePropertyWithStaticMethod, exception.Error);
    }

    [Test]
    public void TestInvalidInstanceFieldInStaticMethod()
    {
        var inputAssembly = CreateAssemblyForFile("FailingAssemblyFiles", "InvalidInstanceFieldInStaticMethod");
        var exception = Assert.Throws<WeavingException>(() => WeaveAssembly(inputAssembly.Location, true));
        Assert.AreEqual(EWeavingError.InstanceFieldWithStaticMethod, exception.Error);
    }

    string WeaveAssembly(string inputPath, bool readWrite)
    {
        string outputPath = inputPath;
        if (!readWrite)
        {
            outputPath = inputPath.Replace(".dll", "2.dll");
            File.Copy(inputPath, outputPath, true);
        }

        using (var moduleDefinition = ModuleDefinition.ReadModule(inputPath, new ReaderParameters() { ReadWrite = readWrite }))
        {
            var weavingTask = new ModuleWeaver
            {
                ModuleDefinition = moduleDefinition
            };

            weavingTask.LogDebug = Log;
            weavingTask.LogInfo = Log;
            weavingTask.LogError = Log;

            weavingTask.Execute();
            if (readWrite)
                moduleDefinition.Write();
            else
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

    static Assembly CreateAssemblyForFile(string folder, string filename)
    {
        var inputPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, $@"../../{folder}/{filename}.cs"));
        var outputPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, filename + ".dll"));

        var compilerParams = new CompilerParameters();
        compilerParams.GenerateExecutable = false;
        compilerParams.GenerateInMemory = false;
        compilerParams.OutputAssembly = outputPath;
        compilerParams.TempFiles = new TempFileCollection(".", false);
        compilerParams.ReferencedAssemblies.Add(typeof(AddStateAttribute).Assembly.Location);

        using (var provider = CodeDomProvider.CreateProvider(CodeDomProvider.GetLanguageFromExtension(".cs")))
        {
            var results = provider.CompileAssemblyFromFile(compilerParams, inputPath);
            return results.CompiledAssembly;
        }
    }
}