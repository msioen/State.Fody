using State.Fody;
using System;

namespace InvalidPropertySetter
{
    public class Class1
    {
        public bool IsBusy { get; }

        [AddState("IsBusy")]
        public void Test()
        {
            Console.WriteLine("Test");
        }
    }
}
