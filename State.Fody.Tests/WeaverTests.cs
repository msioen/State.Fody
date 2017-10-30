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
    string newAssemblyPath;
    string assemblyPath;

    [OneTimeSetUp]
    public void Setup()
    {
        assemblyPath = GetAssemblyPath("AssemblyToProcess");
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
    public void TestInvalidPropertySetter()
    {
        // due to C#6 usage not possible to use the default CodeDomProvider here
        var inputPath = GetAssemblyPath("InvalidPropertySetter");
        var exception = Assert.Throws<WeavingException>(() => WeaveAssembly(inputPath));
        Assert.AreEqual(EWeavingError.InvalidPropertySetter, exception.Error);
    }

    [Test]
    public void TestInvalidPropertyType()
    {
        var inputAssembly = CreateAssemblyForFile("FailingAssemblyFiles", "InvalidPropertyType");
        var exception = Assert.Throws<WeavingException>(() => WeaveAssembly(inputAssembly.Location));
        Assert.AreEqual(EWeavingError.InvalidPropertyType, exception.Error);
    }

    [Test]
    public void TestInvalidInstancePropertyInStaticMethod()
    {
        var inputAssembly = CreateAssemblyForFile("FailingAssemblyFiles", "InvalidInstancePropertyInStaticMethod");
        var exception = Assert.Throws<WeavingException>(() => WeaveAssembly(inputAssembly.Location));
        Assert.AreEqual(EWeavingError.InstancePropertyWithStaticMethod, exception.Error);
    }

    [Test]
    public void TestInvalidInstanceFieldInStaticMethod()
    {
        var inputAssembly = CreateAssemblyForFile("FailingAssemblyFiles", "InvalidInstanceFieldInStaticMethod");
        var exception = Assert.Throws<WeavingException>(() => WeaveAssembly(inputAssembly.Location));
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

    static Assembly CreateAssemblyForFile(string folder, string filename)
    {
        var inputPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, $@"../../{folder}/{filename}.cs"));
        var outputPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, filename + ".dll"));

        var compilerParams = new CompilerParameters();
        compilerParams.GenerateExecutable = false;
        compilerParams.GenerateInMemory = false;
        compilerParams.OutputAssembly = outputPath;
        compilerParams.TempFiles = new TempFileCollection(Path.GetTempPath(), false);
        compilerParams.ReferencedAssemblies.Add(typeof(AddStateAttribute).Assembly.Location);
        
        using (var provider = CodeDomProvider.CreateProvider(CodeDomProvider.GetLanguageFromExtension(".cs")))
        {
            var results = provider.CompileAssemblyFromFile(compilerParams, inputPath);
            return results.CompiledAssembly;
        }
    }
}