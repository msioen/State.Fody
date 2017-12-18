using System;
using System.Threading.Tasks;
using State.Fody;

public class StateChange
{
    public int SetterCounter { get; set; }

    bool _isBusy;
    public bool IsBusy
    {
        get { return _isBusy; }
        set
        {
            _isBusy = value;
            SetterCounter++;
            Console.WriteLine("IsBusy " + _isBusy + " - " + SetterCounter);
        }
    }

    [AddState("IsBusy")]
    public void Test()
    {
        Console.WriteLine("Test");
    }

    [AddState("IsBusy")]
    public void TestSubcalls()
    {
        SubMethod1();
        SubMethod2();
        SubMethod3();
    }

    void SubMethod1()
    {
        Console.WriteLine("1");
    }

    void SubMethod2()
    {
        Console.WriteLine("2");
    }

    void SubMethod3()
    {
        Console.WriteLine("3");
    }

    [AddState("IsBusy")]
    public async Task TestAsync()
    {
        await Task.Delay(10);
    }

    [AddState("IsBusy")]
    public async Task TestAsyncSubcalls()
    {
        await AsyncSubMethod1();
        await AsyncSubMethod2();
        await AsyncSubMethod3();
    }

    async Task AsyncSubMethod1()
    {
        await Task.Delay(5);
        Console.WriteLine("1");
    }

    async Task AsyncSubMethod2()
    {
        await Task.Delay(5);
        Console.WriteLine("2");
    }

    async Task AsyncSubMethod3()
    {
        await Task.Delay(5);
        Console.WriteLine("3");
    }
}