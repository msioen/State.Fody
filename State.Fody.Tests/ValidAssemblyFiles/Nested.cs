using System;
using State.Fody;

public class Nested
{
    public int SetterCounter { get; set; }

    bool _isBusy;
    public bool IsBusy
    {
        get { return _isBusy; }
        set
        {
            if (value != _isBusy)
            {
                _isBusy = value;
                SetterCounter++;
                Console.WriteLine("IsBusy " + _isBusy + " - " + SetterCounter);
            }
        }
    }

    [AddState("IsBusy")]
    public void Test()
    {
        Console.WriteLine("Test");
        TestSub1();
        TestSub2();
    }

    [AddState("IsBusy")]
    public void TestSub1()
    {
        Console.WriteLine("Test1");
    }

    [AddState("IsBusy")]
    public void TestSub2()
    {
        Console.WriteLine("Test2");
    }
}