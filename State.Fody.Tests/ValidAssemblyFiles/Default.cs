using System;
using State.Fody;

public class Default
{
    bool _isSyncing;

    public bool IsLoading { get; set; }

    [AddState("_isSyncing")]
    public void TestField()
    {
        Console.WriteLine("TestField");
    }

    [AddState("IsLoading")]
    public void TestProperty()
    {
        Console.WriteLine("TestProperty");
    }

    [AddState("IsTesting")]
    public void TestNewProperty()
    {
        Console.WriteLine("TestNewProperty");
    }

    [AddState("IsTesting")]
    public void TestMultiple()
    {
        TestField();
        TestProperty();
        TestNewProperty();
    }
}