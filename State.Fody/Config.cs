using System;
using System.Linq;

public partial class ModuleWeaver
{
    public bool CountNestedStateChanges { get; private set; }

    public void ResolveConfig()
    {
        var value = Config?.Attributes("CountNestedStateChanges").FirstOrDefault();
        if (value != null)
        {
            CountNestedStateChanges = bool.Parse((string)value);
        }
    }
}