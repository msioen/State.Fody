using System;
using State.Fody;

namespace AssemblyToProcess
{
    public class Generic<T>
    {
        [AddState("IsLoading")]
        public void Test<T>()
        {
            Console.WriteLine("Test<T>");
        }
    }
}
