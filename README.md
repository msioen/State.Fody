## This is an add-in for [Fody](https://github.com/Fody/Fody/)

[Introduction to Fody](http://github.com/Fody/Fody/wiki/SampleUsage)

This add-in adds an attribute which will update a boolean state during method execution. It's set to true while the method executes and resets to false once the method returns.

Some background info and details can be foun on [my blog](https://michielsioen.be/2017-10-21-il-weaving/)

### Your code
```c#
[AddState("IsLoading")]
public void TestProperty()
{
    Console.WriteLine("TestProperty");
}

[AddState("IsTesting")]
public async Task<int> TestAsync3(int input)
{
    await Task.Delay(100);
    Console.WriteLine("TestAsync3");
    return input;
}
```

### What gets compiled
```c#
[AddState ("IsLoading")]
public void TestProperty ()
{
	this.IsLoading = true;
	try {
		Console.WriteLine ("TestProperty");
	} finally {
		this.IsLoading = false;
	}
}

// Note - for async methods the weaving happens in the created nested method
[AddState ("IsTesting"), AsyncStateMachine (typeof(Async.<TestAsync3>d__12))]
public Task<int> TestAsync3 (int input)
{
	Async.<TestAsync3>d__12 <TestAsync3>d__;
	<TestAsync3>d__.<>4__this = this;
	<TestAsync3>d__.input = input;
	<TestAsync3>d__.<>t__builder = AsyncTaskMethodBuilder<int>.Create ();
	<TestAsync3>d__.<>1__state = -1;
	AsyncTaskMethodBuilder<int> <>t__builder = <TestAsync3>d__.<>t__builder;
	<>t__builder.Start<Async.<TestAsync3>d__12> (ref <TestAsync3>d__);
	return <TestAsync3>d__.<>t__builder.Task;
}

void IAsyncStateMachine.MoveNext ()
	{
		this.<>4__this.IsTesting = true;
		try {
			int num = this.<>1__state;
			int result;
			try {
				TaskAwaiter awaiter;
				if (num != 0) {
					awaiter = Task.Delay (100).GetAwaiter ();
					if (!awaiter.IsCompleted) {
						this.<>1__state = 0;
						this.<>u__1 = awaiter;
						this.<>t__builder.AwaitUnsafeOnCompleted<TaskAwaiter, Async.<TestAsync3>d__12> (ref awaiter, ref this);
						return;
					}
				} else {
					awaiter = this.<>u__1;
					this.<>u__1 = default(TaskAwaiter);
					this.<>1__state = -1;
				}
				awaiter.GetResult ();
				Console.WriteLine ("TestAsync3");
				result = this.input;
			} catch (Exception exception) {
				this.<>1__state = -2;
				this.<>t__builder.SetException (exception);
				return;
			}
			this.<>1__state = -2;
			this.<>t__builder.SetResult (result);
		} finally {
			int num = this.<>1__state;
			if (num != 0) {
				this.<>4__this.IsTesting = false;
			}
		}
	}

```
