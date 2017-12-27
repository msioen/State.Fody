using System;
using System.Collections.Generic;

namespace State.Fody
{
    public static class StateCounters
    {
        static Dictionary<string, int> _counters = new Dictionary<string, int>();

        public static bool AddLoading(string name)
        {
            lock (_counters)
            {
                _counters.TryGetValue(name, out var currLoadingCount);
                _counters[name] = currLoadingCount + 1;
                return _counters[name] > 0;
            }
        }

        public static bool RemoveLoading(string name)
        {
            lock (_counters)
            {
                _counters[name]--;
                return _counters[name] > 0;
            }
        }
    }
}