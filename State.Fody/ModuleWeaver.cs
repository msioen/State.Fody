﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Mono.Cecil;

public partial class ModuleWeaver
{
    List<TypeDefinition> allClasses;
    List<MethodNode> nodes;

    public XElement Config { get; set; }

    public Action<string> LogError { get; set; }
    public Action<string> LogInfo { get; set; }
    public Action<string> LogDebug { get; set; }

    public ModuleDefinition ModuleDefinition { get; set; }

    MethodReference _countersAddReference;
    MethodReference _countersRemoveReference;

    public ModuleWeaver()
    {
        LogError = s => { };
        LogInfo = s => { };
        LogDebug = s => { };
    }

    public void Execute()
    {
        ResolveConfig();
        BuildMethodNodes();
        TrimPropertyCreation();
        CreateProperties();
        WeaveState();
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
    }

    void TrimPropertyCreation()
    {
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
                        if (node.MethodDefinition.IsStatic != nodeSecondPass.MethodDefinition.IsStatic &&
                           nodeSecondPass.MethodDefinition.IsStatic)
                        {
                            MergeNodeCreation(nodeSecondPass, node);
                        }
                        else
                        {
                            MergeNodeCreation(node, nodeSecondPass);
                        }
                    }
                }
            }
        }
    }

    static void MergeNodeCreation(MethodNode node, MethodNode nodeToMerge)
    {
        nodeToMerge.AddProperty = false;
        node.AddProperty = true;
        node.LinkedNodes.Add(nodeToMerge);
        node.LinkedNodes.AddRange(nodeToMerge.LinkedNodes);
    }

    void CreateProperties()
    {
        foreach (var node in nodes)
        {
            if (node.AddProperty)
            {
                node.PropertyReference = CreateProperty(node.TypeDefinition, node.StatePropertyName, node.MethodDefinition.IsStatic).SetMethod;
                foreach (var linkedNode in node.LinkedNodes)
                {
                    linkedNode.PropertyReference = node.PropertyReference;
                }
            }
        }

        if (CountNestedStateChanges)
        {
            _countersAddReference = ModuleDefinition
                .ImportReference(typeof(State.Fody.StateCounters)
                                 .GetMethod(nameof(State.Fody.StateCounters.AddLoading)));
            _countersRemoveReference = ModuleDefinition
                .ImportReference(typeof(State.Fody.StateCounters)
                                 .GetMethod(nameof(State.Fody.StateCounters.RemoveLoading)));
        }
    }

    void WeaveState()
    {
        // apply state pattern to methods
        foreach (var node in nodes)
        {
            AddStateToMethod(node);
        }
    }
}