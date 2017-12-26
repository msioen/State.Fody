using System;
using NUnit.Framework;

[TestFixture]
public class InvalidScenarioTests
{
    [Test]
    public void TestInvalidPropertySetter()
    {
        var inputAssemblyPath = TestHelper.CreateAssemblyForFiles("InvalidPropertySetter", "FailingAssemblyFiles", "InvalidPropertySetter.cs");
        var exception = Assert.Throws<WeavingException>(() => TestHelper.WeaveAssembly(inputAssemblyPath, "2", null));
        Assert.AreEqual(EWeavingError.InvalidPropertySetter, exception.Error);
    }

    [Test]
    public void TestInvalidPropertySetter2()
    {
        var inputAssemblyPath = TestHelper.CreateAssemblyForFiles("InvalidPropertySetter2", "FailingAssemblyFiles", "InvalidPropertySetter2.cs");
        var exception = Assert.Throws<WeavingException>(() => TestHelper.WeaveAssembly(inputAssemblyPath, "2", null));
        Assert.AreEqual(EWeavingError.InvalidPropertySetter, exception.Error);
    }

    [Test]
    public void TestInvalidPropertyType()
    {
        var inputAssemblyPath = TestHelper.CreateAssemblyForFiles("InvalidPropertyType", "FailingAssemblyFiles", "InvalidPropertyType.cs");
        var exception = Assert.Throws<WeavingException>(() => TestHelper.WeaveAssembly(inputAssemblyPath, "2", null));
        Assert.AreEqual(EWeavingError.InvalidPropertyType, exception.Error);
    }

    [Test]
    public void TestInvalidInstancePropertyInStaticMethod()
    {
        var inputAssemblyPath = TestHelper.CreateAssemblyForFiles("InvalidInstancePropertyInStaticMethod", "FailingAssemblyFiles", "InvalidInstancePropertyInStaticMethod.cs");
        var exception = Assert.Throws<WeavingException>(() => TestHelper.WeaveAssembly(inputAssemblyPath, "2", null));
        Assert.AreEqual(EWeavingError.InstancePropertyWithStaticMethod, exception.Error);
    }

    [Test]
    public void TestInvalidInstanceFieldInStaticMethod()
    {
        var inputAssemblyPath = TestHelper.CreateAssemblyForFiles("InvalidInstanceFieldInStaticMethod", "FailingAssemblyFiles", "InvalidInstanceFieldInStaticMethod.cs");
        var exception = Assert.Throws<WeavingException>(() => TestHelper.WeaveAssembly(inputAssemblyPath, "2", null));
        Assert.AreEqual(EWeavingError.InstanceFieldWithStaticMethod, exception.Error);
    }
}