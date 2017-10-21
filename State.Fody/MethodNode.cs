using System;
using Mono.Cecil;

public class MethodNode
{
    public TypeDefinition TypeDefinition;
    public MethodDefinition MethodDefinition;
    public string StatePropertyName;
    public bool AddProperty;
}