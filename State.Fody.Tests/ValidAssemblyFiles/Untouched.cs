using System;
using System.Threading.Tasks;

public class Untouched
{
    static bool _backingField;
    public static bool Property
    {
        get { return _backingField; }
        set { _backingField = value; }
    }

    public bool Prop { get; set; }
    public static bool SProp { get; set; }

    public void Test()
    {
        Console.WriteLine("Test");
    }

    public void TestParameter(int i)
    {
        Console.WriteLine("TestParameter " + i);
    }

    public int TestReturn()
    {
        Console.WriteLine("TestReturn");
        return 6;
    }

    public async Task AsyncTest()
    {
        await Task.Delay(100);
    }

    public async Task<int> AsyncTestReturn()
    {
        await Task.Delay(100);
        return 5;
    }

    public static void SPropT1()
    {
        SProp = false;
    }

    public static void SPropT2()
    {
        _backingField = true;
        try
        {
            Console.WriteLine("ddddd");
        }
        finally
        {
            _backingField = false;
        }
    }
}