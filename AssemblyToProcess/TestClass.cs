using System;
using System.Threading.Tasks;
using State.Fody;

namespace AssemblyToProcess
{
    public class Test
    {
        public bool IsState { get; set; }

        public Task SomethingAsync()
        {
            return Task.FromResult(0);
        }

        public void TestContinue()
        {
            IsState = true;
            var task = SomethingAsync();
            task.ContinueWith(x => IsState = false);
        }
    }

    public class TestClass : BaseClass
    {
        bool IsTesting3;

        public bool IsTesting2 { get; set; }

        //public new bool IsTesting { get; set; }

        [AddState("IsTesting")]
        public void TestMethod()
        {
            Console.WriteLine("Doing something");
        }

        [AddState("IsTesting2")]
        public void TestMethod2()
        {
            Console.WriteLine("Doing something");
        }

        [AddState("IsTesting3")]
        public void TestMethod3()
        {
            Console.WriteLine("Doing something");
        }

        [AddState("IsTesting3")]
        public async Task TestMethod4()
        {
            await Task.Delay(5);
            Console.WriteLine("Doing something");
        }

        [AddState("IsTesting3")]
        public async Task<int> TestMethod5()
        {
            await Task.Delay(5);
            Console.WriteLine("Doing something");
            return 15;
        }
    }

    public class GenericClass<T>
    {
        [AddState("IsTesting")]
        public void TestMethod()
        {
            Console.WriteLine("Doing something");
        }

        [AddState("IsTesting3")]
        public async Task TestMethod4()
        {
            await Task.Delay(5);
            Console.WriteLine("Doing something");
        }

        [AddState("IsTesting3")]
        public async Task<int> TestMethod5()
        {
            await Task.Delay(5);
            Console.WriteLine("Doing something");
            return 15;
        }
    }

    public class BaseClass
    {
        public bool IsTesting { get; set; }
    }

    public class Example
    {
        bool _isTesting;

        public bool IsTesting { get; set; }

        public void DoSomething()
        {
            try
            {
                _isTesting = true;

                Console.WriteLine("something");
            }
            finally
            {
                _isTesting = false;
            }
        }

        public void DoSomething2()
        {
            try
            {
                IsTesting = true;

                Console.WriteLine("something");
            }
            finally
            {
                IsTesting = false;
            }
        }
    }

    public class TryFinally
    {
        public bool IsTesting { get; set; }

        public void DoSomething()
        {
            try
            {
                IsTesting = true;

                Console.WriteLine("something");
            }
            finally
            {
                IsTesting = false;
            }
        }

        public int DoSomething1()
        {
            try
            {
                IsTesting = true;

                return 1;
            }
            finally
            {
                IsTesting = false;
            }
        }

        public int DoSomething2(bool test)
        {
            if (test)
                return 1;
            else
                return 2;
        }

        public int DoSomething3(bool test)
        {
            try
            {
                IsTesting = true;

                if (test)
                    return 1;
                else
                    return 2;
            }
            finally
            {
                IsTesting = false;
            }
        }

        public async Task<int> DoSomething4(bool test)
        {
            try
            {
                IsTesting = true;

                await Task.Delay(1);

                if (test)
                    return 1;
                else
                    return 2;
            }
            finally
            {
                IsTesting = false;
            }
        }
    }
}