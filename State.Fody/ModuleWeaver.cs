using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

public partial class ModuleWeaver
{
    List<TypeDefinition> allClasses;
    List<MethodNode> nodes;

    public Action<string> LogError { get; set; }
    public Action<string> LogInfo { get; set; }
    public Action<string> LogDebug { get; set; }

    public ModuleDefinition ModuleDefinition { get; set; }

    public ModuleWeaver()
    {
        LogError = s => { };
        LogInfo = s => { };
        LogDebug = s => { };
    }

    public void Execute()
    {
        // find methods with AddState attribute

        BuildMethodNodes();
    }

    void BuildMethodNodes()
    {
        // get all relevant classes
        allClasses = ModuleDefinition
            .GetTypes()
            .Where(x => x.IsClass && x.BaseType != null)
            .ToList();

        // get all methods with our attribute
        nodes = new List<MethodNode>();

        foreach (var typeDefinition in allClasses)
        {
            foreach (var method in typeDefinition.Methods)
            {
                var stateAttribute = method.CustomAttributes
                          .FirstOrDefault(x => x.Constructor.DeclaringType.FullName == "State.Fody.AddStateAttribute");
                if (stateAttribute == null)
                    continue;

                var argument = (string)stateAttribute.ConstructorArguments[0].Value;
                var methodNode = new MethodNode()
                {
                    TypeDefinition = typeDefinition,
                    MethodDefinition = method,
                    StatePropertyName = argument
                };

                methodNode.AddProperty = !ValidateHasProperty(typeDefinition, method, argument, ref methodNode);
                nodes.Add(methodNode);
            }
        }

        // cleanup new requested properties for duplicates
        var lookup = nodes.Where(x => x.AddProperty)
                          .ToLookup(x => x.StatePropertyName);

        foreach (var kvp in lookup)
        {
            foreach (var node in kvp)
            {
                foreach (var nodeSecondPass in kvp)
                {
                    if (node == nodeSecondPass)
                        continue;

                    if (node.TypeDefinition.IsAssignableFrom(nodeSecondPass.TypeDefinition))
                    {
                        nodeSecondPass.AddProperty = false;
                        node.AddProperty = true;
                        node.LinkedNodes.Add(nodeSecondPass);
                        node.LinkedNodes.AddRange(nodeSecondPass.LinkedNodes);
                    }
                }
            }
        }

        // create backing properties where required
        foreach (var node in nodes)
        {
            if (node.AddProperty)
            {
                node.PropertyReference = CreateProperty(node.TypeDefinition, node.StatePropertyName).SetMethod;
                foreach (var linkedNode in node.LinkedNodes)
                {
                    linkedNode.PropertyReference = node.PropertyReference;
                }
            }
        }

        // apply state pattern to methods
        foreach (var node in nodes)
        {
            AddStateToMethod(node);
        }
    }
}