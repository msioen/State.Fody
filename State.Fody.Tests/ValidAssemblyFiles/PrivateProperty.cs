using System;
using State.Fody;

public class PrivatePropertyBaseClass
{
    bool IsTesting { get; set; }

    [AddState("IsTesting")]
    public void TestBase()
    {
        Console.WriteLine("TestBase");
    }
}

public class PrivatePropertySubClass : PrivatePropertyBaseClass
{
    [AddState("IsTesting")]
    public void TestSub()
    {
        Console.WriteLine("TestSub");
    }
}