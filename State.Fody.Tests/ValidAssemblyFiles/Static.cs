using System;
using State.Fody;

public class Static
{
    static bool _isSyncing;

    public static bool IsLoading { get; set; }

    [AddState("_isSyncing")]
    public static void TestField()
    {
        Console.WriteLine("TestField");
    }

    [AddState("IsLoading")]
    public static void TestProperty()
    {
        Console.WriteLine("TestProperty");
    }

    [AddState("IsTesting")]
    public void TestNewPropertyInstance()
    {
        Console.WriteLine("TestNewProperty");
    }

    [AddState("IsTesting")]
    public static void TestNewProperty()
    {
        Console.WriteLine("TestNewProperty");
    }

    [AddState("IsTesting")]
    public static void TestMultiple()
    {
        TestField();
        TestProperty();
        TestNewProperty();
    }
}