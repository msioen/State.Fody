using System.Collections.Generic;
using Mono.Cecil;

public class MethodNode
{
    public List<MethodNode> LinkedNodes = new List<MethodNode>();

    public TypeDefinition TypeDefinition;
    public MethodDefinition MethodDefinition;
    public string StatePropertyName;
    public bool AddProperty;

    public FieldReference FieldReference;
    public MethodReference PropertyReference;
}