using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

public static class Extensions
{
    public static void InsertBefore(this ILProcessor processor, Instruction target, IEnumerable<Instruction> instructions)
    {
        foreach (var instruction in instructions)
            processor.InsertBefore(target, instruction);
    }

    public static void InsertAfter(this ILProcessor processor, Instruction target, IEnumerable<Instruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            processor.InsertAfter(target, instruction);
            target = instruction;
        }
    }

    public static CustomAttribute Get(this Mono.Collections.Generic.Collection<CustomAttribute> collection, string name)
    {
        if (name.IndexOf('.') > 0)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                var fullname = collection[i].AttributeType.FullName;
                if (fullname.GetHashCode() == name.GetHashCode() && fullname == name)
                    return collection[i];
            }
        }
        else
        {
            for (int i = 0; i < collection.Count; i++)
            {
                var shortname = collection[i].AttributeType.Name;
                if (shortname.GetHashCode() == name.GetHashCode() && shortname == name)
                    return collection[i];
            }
        }

        return null;
    }

    public static T Get<T>(this IEnumerable<T> source, string name) where T : MemberReference
    {
        var collection = source.ToArray();
        if (name.IndexOf('.') > 0)
        {
            for (int i = 0; i < collection.Length; i++)
            {
                var fullname = collection[i].FullName;
                if (fullname.GetHashCode() == name.GetHashCode() && fullname == name)
                    return collection[i];
            }
        }
        else
        {
            for (int i = 0; i < collection.Length; i++)
            {
                var shortname = collection[i].Name;
                if (shortname.GetHashCode() == name.GetHashCode() && shortname == name)
                    return collection[i];
            }
        }

        return null;
    }

    public static bool IsAssignableFrom(this TypeDefinition target, TypeDefinition source)
    {
        return target == source
           || target.MetadataToken == source.MetadataToken
           || source.IsSubclassOf(target);
    }

    static bool IsSubclassOf(this TypeDefinition childTypeDef, TypeDefinition parentTypeDef)
    {
        return childTypeDef.MetadataToken
            != parentTypeDef.MetadataToken
            && childTypeDef
           .EnumerateBaseClasses()
           .Any(b => b.MetadataToken == parentTypeDef.MetadataToken);
    }

    static IEnumerable<TypeDefinition> EnumerateBaseClasses(this TypeDefinition klassType)
    {
        for (var typeDefinition = klassType; typeDefinition != null; typeDefinition = typeDefinition.BaseType?.Resolve())
        {
            yield return typeDefinition;
        }
    }
}