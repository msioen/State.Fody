using System;
using State.Fody;

public class ExistingClauses
{
    bool _isSyncing;

    public bool IsLoading { get; set; }

    [AddState("_isSyncing")]
    public void TestField()
    {
        try
        {
            Console.WriteLine("TestField");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    [AddState("IsLoading")]
    public void TestProperty()
    {
        try
        {
            Console.WriteLine("TestProperty");
        }
        finally
        {
            Console.WriteLine("Finally");
        }
    }

    [AddState("IsTesting")]
    public void TestNewProperty()
    {
        try
        {
            Console.WriteLine("TestNewProperty");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            Console.WriteLine("Finally");
        }
    }
}