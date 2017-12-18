using System;
using State.Fody;

public class PropertyWithoutSetter
{
    public bool IsTesting { get; }

    //[AddState("IsTesting")]
    //public void Test()
    //{
    //    Console.WriteLine("Test");
    //}
}

public class PropertyWithoutSetterSub : PropertyWithoutSetter
{
    [AddState("IsTesting")]
    public void TestSub()
    {
        Console.WriteLine("TestSub");
    }
}