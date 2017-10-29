using System;
using State.Fody;

public class InvalidInstancePropertyInStaticMethod
{
    public bool IsBusy { get; set; }

    [AddState("IsBusy")]
    public static void Test()
    {
        Console.WriteLine("IsBusy");
    }
}