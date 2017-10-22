using System;
using System.Threading.Tasks;

namespace AssemblyToProcess
{
    public class Untouched
    {
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
    }
}
