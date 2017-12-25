using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class StateCountersTests
{
    Assembly assembly;
    string newAssemblyPath;
    string assemblyPath;

    [OneTimeSetUp]
    public void Setup()
    {
        var validFileNames = Directory.GetFiles(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../ValidAssemblyFiles/"));
        var strippedFileNames = validFileNames.Select(x => Path.GetFileName(x)).ToArray();

        assemblyPath = TestHelper.CreateAssemblyForFiles(
            outputAssembly: "AssemblyToProcessWithCounters",
            inputFolder: "ValidAssemblyFiles",
            inputFilenames: strippedFileNames);

        newAssemblyPath = TestHelper.WeaveAssembly(assemblyPath, "2", "<PropertyChanged CountNestedStateChanges='true'/>");
        assembly = Assembly.LoadFile(newAssemblyPath);
    }

    [Test]
    public void TestValidity()
    {
        Verifier.Verify(assemblyPath, newAssemblyPath);
    }

    [Test]
    public void TestStateChange()
    {
        var instance = TestHelper.GetInstance(assembly, "StateChange");
        Assert.AreEqual(0, instance.SetterCounter);
        instance.Test();
        Assert.AreEqual(2, instance.SetterCounter);
    }

    [Test]
    public async Task TestAsyncStateChange()
    {
        var instance = TestHelper.GetInstance(assembly, "StateChange");
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
        var instance = TestHelper.GetInstance(assembly, "StateChange");
        Assert.AreEqual(0, instance.SetterCounter);
        var task = (Task)instance.TestAsyncSubcalls();
        await task.ContinueWith(t =>
        {
            Assert.AreEqual(2, instance.SetterCounter);
        });
    }

    [Test]
    public void TestNested()
    {
        var instance = TestHelper.GetInstance(assembly, "Nested");
        Assert.AreEqual(0, instance.SetterCounter);
        instance.Test();
        Assert.AreEqual(2, instance.SetterCounter);
    }
}