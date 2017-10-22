using System;
using System.Linq;
using Mono.Cecil;

public class ReferenceFinder
{
    readonly ModuleDefinition moduleDefinition;
    readonly ModuleDefinition mscorlib;

    public ReferenceFinder(ModuleDefinition moduleDefinition)
    {
        this.moduleDefinition = moduleDefinition;
        var mscorlibAssemblyReference = moduleDefinition.AssemblyReferences.First(a => a.Name == "mscorlib");
        this.mscorlib = moduleDefinition.AssemblyResolver.Resolve(mscorlibAssemblyReference).MainModule;
    }

    public TypeReference GetTypeReference(Type type)
    {

        if (type.Assembly.GetName().Name == "mscorlib")
        {
            var typeReference = mscorlib.Types.FirstOrDefault(tr => tr.Namespace == type.Namespace && tr.Name == type.Name);
            if (typeReference != null)
            {
                return moduleDefinition.ImportReference(typeReference);
            }
        }

        return moduleDefinition.ImportReference(type);
    }
}