using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

public partial class ModuleWeaver
{
    void AddStateToMethod(MethodNode node)
    {
        var methodDefinition = node.MethodDefinition;

        if (methodDefinition.Body == null ||
            methodDefinition.Body.Instructions.Count <= 0)
        {
            LogInfo($"{methodDefinition.Name} has no body or instructions.");
            return;
        }

        methodDefinition.Body.SimplifyMacros();

        var asyncStateMachine = node.MethodDefinition.CustomAttributes.Get("System.Runtime.CompilerServices.AsyncStateMachineAttribute");
        if (asyncStateMachine != null)
        {
            AddStateToAsyncMethod(node, asyncStateMachine);
        }
        else
        {
            AddStateToSyncMethod(node);
        }

        methodDefinition.Body.OptimizeMacros();
    }

    /// <summary>
    /// asynchronous methods update state on start, on result and on error
    /// </summary>
    /// <param name="methodDefinition">Method definition.</param>
    void AddStateToAsyncMethod(MethodNode node, CustomAttribute asyncStateMachine)
    {
        var asyncType = asyncStateMachine.ConstructorArguments[0].Value as TypeReference;
        var asyncTypeDefinition = asyncType.Resolve();
        var asyncTypeMethod = asyncTypeDefinition.Methods.Get("MoveNext");

        if (asyncTypeMethod == null)
        {
            LogError($"Unable to find the method MoveNext of async method {node.MethodDefinition.Name}");
            return;
        }

        var methodBodyFirstInstruction = GetMethodFirstInstruction(asyncTypeMethod);
        var methodBodyReturnInstruction = asyncTypeMethod.Body.Instructions.FirstOrDefault(x => x.OpCode == OpCodes.Ret);
        var tryCatchLeaveInstructions = GetTryCatchLeaveInstructions(methodBodyReturnInstruction);

        var stateField = asyncTypeDefinition.Fields.FirstOrDefault(x => x.FieldType == ModuleDefinition.TypeSystem.Int32);
        var thisField = asyncTypeDefinition.Fields.FirstOrDefault(x => x.FieldType == node.TypeDefinition);
        var nopInstruction = Instruction.Create(OpCodes.Nop);
        // We need to check on state to avoid changing state when execution isn't finished
        var finalInstructions = new List<Instruction>()
        {
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldfld, stateField),
            Instruction.Create(OpCodes.Stloc_0),
            Instruction.Create(OpCodes.Ldloc_0),
            Instruction.Create(OpCodes.Brfalse_S, nopInstruction)
        };
        finalInstructions.AddRange(GetSetterStateInstructions(node, 0, thisField));
        finalInstructions.Add(Instruction.Create(OpCodes.Endfinally));

        var processor = asyncTypeMethod.Body.GetILProcessor();
        processor.InsertBefore(methodBodyFirstInstruction, GetSetterStateInstructions(node, 1, thisField));
        processor.InsertBefore(methodBodyReturnInstruction, tryCatchLeaveInstructions);
        processor.InsertBefore(methodBodyReturnInstruction, finalInstructions);
        processor.InsertBefore(methodBodyReturnInstruction, nopInstruction);

        var handler = new ExceptionHandler(ExceptionHandlerType.Finally)
        {
            TryStart = methodBodyFirstInstruction,
            TryEnd = tryCatchLeaveInstructions.Last().Next,
            HandlerStart = finalInstructions.First(),
            HandlerEnd = finalInstructions.Last().Next
        };

        asyncTypeMethod.Body.ExceptionHandlers.Add(handler);
        asyncTypeMethod.Body.InitLocals = true;
    }

    /// <summary>
    /// synchronous methods are wrapped in a try/finally block which sets the state
    /// </summary>
    /// <param name="methodDefinition">Method definition.</param>
    void AddStateToSyncMethod(MethodNode node)
    {
        var methodDefinition = node.MethodDefinition;

        var processor = methodDefinition.Body.GetILProcessor();
        var methodBodyFirstInstruction = GetMethodFirstInstruction(methodDefinition);

        VariableDefinition retvalVariableDefinition = null;
        if (methodDefinition.ReturnType.FullName != "System.Void")
            retvalVariableDefinition = AddVariableDefinition(methodDefinition, methodDefinition.ReturnType);

        var variableDefinition = AddVariableDefinition(methodDefinition, ModuleDefinition.TypeSystem.Boolean);

        var saveRetvalInstructions = GetSaveRetvalInstructions(processor, retvalVariableDefinition);
        var methodBodyReturnInstructions = GetMethodBodyReturnInstructions(processor, retvalVariableDefinition);
        var methodBodyReturnInstruction = methodBodyReturnInstructions.First();
        var tryCatchLeaveInstructions = GetTryCatchLeaveInstructions(methodBodyReturnInstruction);
        var finallyInstructions = GetSetterStateInstructions(node, 0);
        finallyInstructions.Add(processor.Create(OpCodes.Endfinally));

        var customRevalInstructions = new Instruction[] { processor.Create(OpCodes.Ldloc_S, variableDefinition) };
        ReplaceRetInstructions(processor, saveRetvalInstructions.Concat(customRevalInstructions).First());

        processor.InsertBefore(methodBodyFirstInstruction, GetSetterStateInstructions(node, 1));

        processor.InsertAfter(methodDefinition.Body.Instructions.Last(), methodBodyReturnInstructions);
        processor.InsertBefore(methodBodyReturnInstruction, saveRetvalInstructions);

        processor.InsertBefore(methodBodyReturnInstruction, customRevalInstructions);
        processor.InsertBefore(methodBodyReturnInstruction, tryCatchLeaveInstructions);

        processor.InsertBefore(methodBodyReturnInstruction, finallyInstructions);

        var handler = new ExceptionHandler(ExceptionHandlerType.Finally)
        {
            TryStart = methodBodyFirstInstruction,
            TryEnd = tryCatchLeaveInstructions.Last().Next,
            HandlerStart = finallyInstructions.First(),
            HandlerEnd = finallyInstructions.Last().Next
        };

        methodDefinition.Body.ExceptionHandlers.Add(handler);
        methodDefinition.Body.InitLocals = true;
    }

    VariableDefinition AddVariableDefinition(MethodDefinition method, TypeReference variableType)
    {
        var variableDefinition = new VariableDefinition(variableType);
        method.Body.Variables.Add(variableDefinition);
        return variableDefinition;
    }

    Instruction GetMethodFirstInstruction(MethodDefinition methodDefinition)
    {
        var methodBodyFirstInstruction = methodDefinition.Body.Instructions.First();
        if (methodDefinition.IsConstructor &&
            methodDefinition.Body.Instructions.Any(i => i.OpCode == OpCodes.Call))
        {
            methodBodyFirstInstruction = methodDefinition.Body.Instructions.First(i => i.OpCode == OpCodes.Call).Next;
        }
        return methodBodyFirstInstruction;
    }

    IList<Instruction> GetMethodBodyReturnInstructions(ILProcessor processor, VariableDefinition retvalVariableDefinition)
    {
        var instructions = new List<Instruction>();
        if (retvalVariableDefinition != null)
            instructions.Add(processor.Create(OpCodes.Ldloc_S, retvalVariableDefinition));
        instructions.Add(processor.Create(OpCodes.Ret));
        return instructions;
    }

    IList<Instruction> GetSaveRetvalInstructions(ILProcessor processor, VariableDefinition retvalVariableDefinition)
    {
        return retvalVariableDefinition == null || processor.Body.Instructions.All(i => i.OpCode != OpCodes.Ret) ?
            new Instruction[0] : new[] { processor.Create(OpCodes.Stloc_S, retvalVariableDefinition) };
    }

    IList<Instruction> GetTryCatchLeaveInstructions(Instruction methodBodyReturnInstruction)
    {
        return new[] { Instruction.Create(OpCodes.Leave_S, methodBodyReturnInstruction) };
    }

    List<Instruction> GetSetterStateInstructions(MethodNode methodNode, int value, FieldDefinition targetField = null)
    {
        var setterList = new List<Instruction>
        {
            Instruction.Create(OpCodes.Ldarg_0)
        };
        if (targetField != null)
        {
            setterList.Add(Instruction.Create(OpCodes.Ldfld, targetField));
        }
        setterList.Add(Instruction.Create(OpCodes.Ldc_I4, value));
        if (methodNode.PropertyReference != null)
        {
            setterList.Add(Instruction.Create(OpCodes.Call, methodNode.PropertyReference));
        }
        else
        {
            setterList.Add(Instruction.Create(OpCodes.Stfld, methodNode.FieldReference));
        }
        return setterList;
    }

    void ReplaceRetInstructions(ILProcessor processor, Instruction methodEpilogueFirstInstruction)
    {
        // We cannot call ret inside a try/catch block. Replace all ret instructions with
        // an unconditional branch to the start of the OnExit epilogue
        var retInstructions = (from i in processor.Body.Instructions
                               where i.OpCode == OpCodes.Ret
                               select i).ToList();

        foreach (var instruction in retInstructions)
        {
            instruction.OpCode = OpCodes.Br_S;
            instruction.Operand = methodEpilogueFirstInstruction;
        }
    }
}
