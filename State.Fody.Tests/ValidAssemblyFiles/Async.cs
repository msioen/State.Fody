﻿using System;
using System.Threading.Tasks;
using State.Fody;

public class Async
{
    bool _isSyncing;

    public bool IsLoading { get; set; }

    public bool IsLoading2 { get; set; }

    [AddState("_isSyncing")]
    public Task TestField()
    {
        Console.WriteLine("TestField");

        return Task.FromResult(0);
    }

    [AddState("IsLoading")]
    public async Task TestAsync1()
    {
        await Task.Delay(100);
        Console.WriteLine("TestAsync1");
    }

    [AddState("IsTesting")]
    public async Task<int> TestAsync2()
    {
        await Task.Delay(100);
        Console.WriteLine("TestAsync2");
        return 5;
    }

    [AddState("IsTesting")]
    public async Task<int> TestAsync3(int input)
    {
        await Task.Delay(100);
        Console.WriteLine("TestAsync3");
        return input;
    }

    [AddState("IsLoading")]
    public async Task TestAsync4()
    {
        IsLoading2 = true;

        await Task.Delay(100);
        Console.WriteLine("TestAsync1");

        IsLoading2 = false;
    }

    [AddState("IsLoading")]
    public async Task Multiple()
    {
        await TestAsync1();
        var two = await TestAsync2();
        var three = await TestAsync3(two);
        await TestAsync4();
    }
}