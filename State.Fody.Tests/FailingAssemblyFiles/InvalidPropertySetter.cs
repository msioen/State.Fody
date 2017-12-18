using System;
using State.Fody;

public class InvalidPropertySetter
{
    public bool IsBusy { get; }

    [AddState("IsBusy")]
    public void Test()
    {
        Console.WriteLine("Test");
    }
}