using System;
using State.Fody;

public class InvalidInstanceFieldInStaticMethod
{
    bool _isBusy;

    [AddState("_isBusy")]
    public static void Test()
    {
        Console.WriteLine("IsBusy - ");
    }
}