using System;
using Mono.Cecil;

namespace State.Fody
{
    public class ModuleWeaver
    {
        public Action<string> LogWarning { get; set; }
        public Action<string> LogInfo { get; set; }
        public Action<string> LogDebug { get; set; }

        public ModuleDefinition ModuleDefinition { get; set; }

        public ModuleWeaver()
        {
            LogWarning = s => { };
            LogInfo = s => { };
            LogDebug = s => { };
        }

        public void Execute()
        {
            // weaving code
        }
    }
}
