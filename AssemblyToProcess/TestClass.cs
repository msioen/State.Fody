using System;
using State.Fody;

namespace AssemblyToProcess
{
    public class TestClass : BaseClass
    {
        public bool IsTesting2 { get; set; }

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
    }

    public class BaseClass
    {
        public bool IsTesting { get; set; }
    }
}
