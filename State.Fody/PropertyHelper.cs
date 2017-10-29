using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

public partial class ModuleWeaver
{
    bool ValidateHasProperty(TypeDefinition typeDefinition, MethodDefinition method, string statePropertyName, ref MethodNode methodNode)
    {
        var typeProperties = GetItems(typeDefinition, x => x.Properties, x => x.SetMethod != null && !x.SetMethod.IsPrivate);
        var propertyDefinition = typeProperties.FirstOrDefault(x => x.Name == statePropertyName);
        if (propertyDefinition == null)
        {
            LogDebug($"Could not find property {statePropertyName} - used in method {method.Name}. Checking on fields.");
            return ValidateHasField(typeDefinition, method, statePropertyName, ref methodNode);
        }
        else if (propertyDefinition.PropertyType.FullName != ModuleDefinition.TypeSystem.Boolean.FullName)
        {
            throw new WeavingException($"AddState property {statePropertyName} for method {method.Name} should be of type Bool");
        }
        else if (propertyDefinition.SetMethod == null)
        {
            throw new WeavingException($"Properties without setters are not supported");
        }
        else if (method.IsStatic && !propertyDefinition.SetMethod.IsStatic)
        {
            throw new WeavingException($"Non-static property states for static methods are not supported");
        }

        methodNode.PropertyReference = propertyDefinition.SetMethod;
        return true;
    }

    bool ValidateHasField(TypeDefinition typeDefinition, MethodDefinition method, string statePropertyName, ref MethodNode methodNode)
    {
        var typeFields = GetItems(typeDefinition, x => x.Fields, x => !x.IsPrivate);
        var fieldDefinition = typeFields.FirstOrDefault(x => x.Name == statePropertyName);
        if (fieldDefinition == null)
        {
            LogDebug($"Could not find field {statePropertyName} - used in method {method.Name}. A property will be made.");
            return false;
        }
        else if (fieldDefinition.FieldType.FullName != ModuleDefinition.TypeSystem.Boolean.FullName)
        {
            throw new WeavingException($"AddState field {statePropertyName} for method {method.Name} should be of type Bool");
        }
        else if (method.IsStatic && !fieldDefinition.IsStatic)
        {
            throw new WeavingException($"Non-static field states for static methods are not supported");
        }

        methodNode.FieldReference = fieldDefinition;
        return true;
    }

    PropertyDefinition CreateProperty(TypeDefinition typeDefinition, string statePropertyName, bool isStatic)
    {
        PropertyDefinition propertyDefinition;
        var propertyType = ModuleDefinition.TypeSystem.Boolean;
        var voidType = ModuleDefinition.TypeSystem.Void;

        // create backing field
        var fieldDefinition = new FieldDefinition($"<{statePropertyName}>k_BackingField", FieldAttributes.Private, propertyType)
        {
            IsStatic = isStatic
        };
        typeDefinition.Fields.Add(fieldDefinition);

        var parameterDefinition = new ParameterDefinition("value", ParameterAttributes.None, propertyType);

        // create property
        var attributes = MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName;

        var getMethod = new MethodDefinition("get_" + statePropertyName, attributes, propertyType)
        {
            IsGetter = true,
            SemanticsAttributes = MethodSemanticsAttributes.Getter,
            IsStatic = isStatic,
            HasThis = !isStatic
        };
        var setMethod = new MethodDefinition("set_" + statePropertyName, attributes, voidType)
        {
            IsSetter = true,
            SemanticsAttributes = MethodSemanticsAttributes.Setter,
            IsStatic = isStatic,
            HasThis = !isStatic
        };
        setMethod.Parameters.Add(parameterDefinition);

        var getter = getMethod.Body.GetILProcessor();
        if (isStatic)
        {
            getter.Emit(OpCodes.Ldsfld, fieldDefinition);
        }
        else
        {
            getter.Emit(OpCodes.Ldarg_0);
            getter.Emit(OpCodes.Ldfld, fieldDefinition);
        }
        getter.Emit(OpCodes.Ret);

        var setter = setMethod.Body.GetILProcessor();
        if (isStatic)
        {
            setter.Emit(OpCodes.Ldarg, parameterDefinition);
            setter.Emit(OpCodes.Stsfld, fieldDefinition);
        }
        else
        {
            setter.Emit(OpCodes.Ldarg_0);
            setter.Emit(OpCodes.Ldarg, parameterDefinition);
            setter.Emit(OpCodes.Stfld, fieldDefinition);
        }
        setter.Emit(OpCodes.Ret);

        typeDefinition.Methods.Add(getMethod);
        typeDefinition.Methods.Add(setMethod);

        propertyDefinition = new PropertyDefinition(statePropertyName, PropertyAttributes.None, propertyType)
        {
            HasThis = !isStatic,
            GetMethod = getMethod,
            SetMethod = setMethod
        };

        typeDefinition.Properties.Add(propertyDefinition);
        return propertyDefinition;
    }

    List<T> GetItems<T>(TypeDefinition typeDefinition, Func<TypeDefinition, IEnumerable<T>> selector,
                        Func<T, bool> baseSelector, bool isBaseType = false)
    {
        if (typeDefinition == null)
            return null;

        var selection = selector(typeDefinition);
        if (isBaseType && baseSelector != null)
            selection = selection.Where(baseSelector);

        var properties = new List<T>(selection);
        if (typeDefinition.BaseType != null &&
           typeDefinition.BaseType.FullName != "System.Object")
        {
            var baseType = Resolve(typeDefinition.BaseType);
            var baseProperties = GetItems(baseType, selector, baseSelector, true);
            if (baseProperties != null)
            {
                properties.AddRange(baseProperties);
            }
        }
        return properties;
    }
}