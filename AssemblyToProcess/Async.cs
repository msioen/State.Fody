using System;
using System.Threading.Tasks;
using State.Fody;

namespace AssemblyToProcess
{
    public class Async
    {
        bool _isSyncing;

        public bool IsLoading { get; set; }

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
    }
}
