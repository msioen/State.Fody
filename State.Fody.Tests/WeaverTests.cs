﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;

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

        assemblyPath = TestHelper.CreateAssemblyForFiles(
            outputAssembly: "AssemblyToProcess",
            inputFolder: "ValidAssemblyFiles",
            inputFilenames: strippedFileNames);

        newAssemblyPath = TestHelper.WeaveAssembly(assemblyPath, "2", null);
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
        var instance = TestHelper.GetInstance(assembly, "Default");
        var type = (Type)instance.GetType();
        var properties = type.GetProperties();
        Assert.AreEqual(2, properties.Length);
        Assert.IsTrue(properties.FirstOrDefault(x => x.Name == "IsTesting") != null);
    }

    [Test]
    public void TestPropertyCreationInheritance()
    {
        var baseInstance = TestHelper.GetInstance(assembly, "BaseClass");
        var baseType = (Type)baseInstance.GetType();
        var baseProperties = baseType.GetProperties();
        Assert.AreEqual(2, baseProperties.Length);

        var subInstance = TestHelper.GetInstance(assembly, "SubClass");
        var subType = (Type)subInstance.GetType();
        var subProperties = subType.GetProperties();
        Assert.AreEqual(4, subProperties.Length);
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
    public void TestStateChangeSubcalls()
    {
        var instance = TestHelper.GetInstance(assembly, "StateChange");
        Assert.AreEqual(0, instance.SetterCounter);
        instance.TestSubcalls();
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
        Assert.AreEqual(4, instance.SetterCounter);
    }
}