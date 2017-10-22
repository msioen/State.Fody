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

        if (methodDefinition.ReturnType.FullName.StartsWith("System.Threading.Tasks.Task", StringComparison.Ordinal))
        {
            AddStateToAsyncMethod(node);
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
    void AddStateToAsyncMethod(MethodNode node)
    {
        // update state when statemachine finishes
        var asyncStateMachine = node.MethodDefinition.CustomAttributes.Get("System.Runtime.CompilerServices.AsyncStateMachineAttribute");
        if (asyncStateMachine != null)
        {
            var asyncType = asyncStateMachine.ConstructorArguments[0].Value as TypeReference;
            var asyncTypeMethod = asyncType.Resolve().Methods.Get("MoveNext");

            if (asyncTypeMethod == null)
            {
                LogError($"Unable to find the method MoveNext of async method {node.MethodDefinition.Name}");
                return;
            }

            var instructions = asyncTypeMethod.Body.Instructions;
            for (var index = 0; index < instructions.Count; index++)
            {
                var line = instructions[index];
                if (line.OpCode != OpCodes.Call)
                {
                    continue;
                }
                var methodReference = line.Operand as MethodReference;
                if (methodReference == null)
                {
                    continue;
                }

                if (IsSetExceptionMethod(methodReference) ||
                    IsSetResultMethod(methodReference))
                {
                    //var previous = instructions[index - 1];
                    var setInstructions = GetSetterStateInstructions(node, 0);
                    foreach (var setInstruction in setInstructions)
                    {
                        instructions.Insert(index + 1, setInstruction);
                        index++;
                    }
                }
            }
        }

        // set state at start of method
        var methodBodyFirstInstruction = GetMethodFirstInstruction(node.MethodDefinition);

        var processor = node.MethodDefinition.Body.GetILProcessor();
        processor.InsertBefore(methodBodyFirstInstruction, GetSetterStateInstructions(node, 1));
    }

    bool IsSetExceptionMethod(MethodReference methodReference)
    {
        return
            methodReference.Name == "SetException" &&
            methodReference.DeclaringType.FullName.StartsWith("System.Runtime.CompilerServices.AsyncTaskMethodBuilder", StringComparison.Ordinal);
    }

    bool IsSetResultMethod(MethodReference methodReference)
    {
        return
            methodReference.Name == "SetResult" &&
            methodReference.DeclaringType.FullName.StartsWith("System.Runtime.CompilerServices.AsyncTaskMethodBuilder", StringComparison.Ordinal);
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
        var tryCatchLeaveInstructions = GetTryCatchLeaveInstructions(processor, methodBodyReturnInstruction);
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

    IList<Instruction> GetTryCatchLeaveInstructions(ILProcessor processor, Instruction methodBodyReturnInstruction)
    {
        return new[] { Instruction.Create(OpCodes.Leave_S, methodBodyReturnInstruction) };
    }

    List<Instruction> GetSetterStateInstructions(MethodNode methodNode, int value)
    {
        var setterList = new List<Instruction>
        {
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldc_I4, value)
        };
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
