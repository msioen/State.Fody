using System;
using State.Fody;

public class InvalidPropertyType
{
    public string IsBusy { get; set; }

    [AddState("IsBusy")]
    public void Test()
    {
        Console.WriteLine("IsBusy");
    }
}