using System;
using State.Fody;

namespace AssemblyToProcess
{
    public class BaseClass
    {
        bool _isSyncing;

        public bool IsLoading { get; set; }

        [AddState("_isSyncing")]
        public void TestBaseClassField()
        {
            Console.WriteLine("TestBaseClassField");
        }

        [AddState("IsLoading")]
        public void TestBaseClassProperty()
        {
            Console.WriteLine("TestBaseClassProperty");
        }

        [AddState("IsTesting")]
        public void TestBaseClassNewProperty()
        {
            Console.WriteLine("TestBaseClassNewProperty");
        }
    }

    public class SubClass : BaseClass
    {
        [AddState("_isSyncing")]
        public void TestSubClassField()
        {
            Console.WriteLine("TestSubClassField");
        }

        [AddState("IsLoading")]
        public void TestSubClassProperty()
        {
            Console.WriteLine("TestSubClassProperty");
        }

        [AddState("IsTesting")]
        public void TestSubClassNewPropertyBase()
        {
            Console.WriteLine("TestSubClassNewPropertyBase");
        }

        [AddState("IsTesting2")]
        public void TestSubClassNewProperty()
        {
            Console.WriteLine("TestSubClassNewProperty");
        }
    }
}
