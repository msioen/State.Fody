using System;
using State.Fody;

public class InvalidPropertySetter2
{
    public bool IsBusy => true;

    [AddState("IsBusy")]
    public void Test()
    {
        Console.WriteLine("Test");
    }
}