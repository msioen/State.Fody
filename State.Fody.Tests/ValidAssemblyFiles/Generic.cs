using System;
using State.Fody;

public class Generic<T>
{
    [AddState("IsLoading")]
    public void Test<T>()
    {
        Console.WriteLine("Test<T>");
    }
}