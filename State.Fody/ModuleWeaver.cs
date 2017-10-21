using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

public partial class ModuleWeaver
{
    List<TypeDefinition> allClasses;
    List<MethodNode> nodes;

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
                var needsPropertyCreation = ValidateAddStateProperty(typeDefinition, method, argument);

                nodes.Add(new MethodNode()
                {
                    TypeDefinition = typeDefinition,
                    MethodDefinition = method,
                    StatePropertyName = argument,
                    AddProperty = needsPropertyCreation
                });
            }
        }

        // create backing properties where required
        foreach (var node in nodes)
        {
            if (node.AddProperty)
            {
                CreateProperty(node.TypeDefinition, node.StatePropertyName);
            }
        }
    }

    bool ValidateAddStateProperty(TypeDefinition typeDefinition, MethodDefinition method, string statePropertyName)
    {
        var typeProperties = GetProperties(typeDefinition);
        var propertyDefinition = typeProperties.FirstOrDefault(x => x.Name == statePropertyName);
        if (propertyDefinition == null)
        {
            LogDebug($"Could not find property {statePropertyName} - used in method {method.Name}. It will be autocreated");
            return true;
        }
        else if (propertyDefinition.PropertyType.FullName != ModuleDefinition.TypeSystem.Boolean.FullName)
        {
            throw new WeavingException($"AddState property {statePropertyName} for method {method.Name} should be of type Bool");
        }

        return false;
    }

    PropertyDefinition CreateProperty(TypeDefinition typeDefinition, string statePropertyName)
    {
        PropertyDefinition propertyDefinition;
        var propertyType = ModuleDefinition.TypeSystem.Boolean;
        var voidType = ModuleDefinition.TypeSystem.Void;

        // create backing field
        var fieldDefinition = new FieldDefinition($"<{statePropertyName}>k_BackingField", FieldAttributes.Private, propertyType);
        typeDefinition.Fields.Add(fieldDefinition);

        var parameterDefinition = new ParameterDefinition(propertyType);

        // create property
        var attributes = MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName;

        var getMethod = new MethodDefinition("get_" + statePropertyName, attributes, propertyType)
        {
            IsGetter = true,
            SemanticsAttributes = MethodSemanticsAttributes.Getter
        };
        var setMethod = new MethodDefinition("set_" + statePropertyName, attributes, voidType)
        {
            IsSetter = true,
            SemanticsAttributes = MethodSemanticsAttributes.Setter
        };
        setMethod.Parameters.Add(parameterDefinition);

        var getter = getMethod.Body.GetILProcessor();
        getter.Emit(OpCodes.Ldarg_0);
        getter.Emit(OpCodes.Ldfld, fieldDefinition);
        getter.Emit(OpCodes.Ret);

        var setter = setMethod.Body.GetILProcessor();
        setter.Emit(OpCodes.Ldarg_0);
        setter.Emit(OpCodes.Ldarg, parameterDefinition);
        setter.Emit(OpCodes.Stfld, fieldDefinition);
        setter.Emit(OpCodes.Ret);

        typeDefinition.Methods.Add(getMethod);
        typeDefinition.Methods.Add(setMethod);

        propertyDefinition = new PropertyDefinition(statePropertyName, PropertyAttributes.None, propertyType)
        {
            HasThis = true,
            GetMethod = getMethod,
            SetMethod = setMethod
        };

        typeDefinition.Properties.Add(propertyDefinition);
        return propertyDefinition;
    }

    List<PropertyDefinition> GetProperties(TypeDefinition typeDefinition)
    {
        if (typeDefinition == null)
            return null;

        var properties = new List<PropertyDefinition>(typeDefinition.Properties);
        if (typeDefinition.BaseType != null &&
           typeDefinition.BaseType.FullName != "System.Object")
        {
            var baseType = Resolve(typeDefinition.BaseType);
            var baseProperties = GetProperties(baseType);
            if (baseProperties != null)
            {
                properties.AddRange(baseProperties);
            }
        }
        return properties;
    }
}