//#define INJECT_LOGGING
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NBG.LogicGraph.CodeGen
{
    class UserlandBindingsGenerator
    {
        AssemblyDefinition asmDef;
        ulong asmHash;
        uint currentEventId;

        TypeReference typeVector3;
        TypeReference typeUnityObject;
        TypeReference typeQuaternion;
        TypeReference typeUnityColor;
        TypeReference typeNodeAPI;

        MethodReference typePreserve_Constructor;
        MethodReference typeAlwaysLink_Constructor;

        TypeReference typeIStack;
        MethodReference typeIStack_Pop;
        MethodReference typeIStack_PopBool;
        MethodReference typeIStack_PopInt;
        MethodReference typeIStack_PopString;
        MethodReference typeIStack_PopFloat;
        MethodReference typeIStack_PopVector3;
        MethodReference typeIStack_PopUnityObject;
        MethodReference typeIStack_PopQuaternion;
        MethodReference typeIStack_PopUnityColor;
        MethodReference typeIStack_PushBool;
        MethodReference typeIStack_PushInt;
        MethodReference typeIStack_PushString;
        MethodReference typeIStack_PushFloat;
        MethodReference typeIStack_PushVector3;
        MethodReference typeIStack_PushUnityObject;
        MethodReference typeIStack_PushQuaternion;
        MethodReference typeIStack_PushUnityColor;

        MethodReference typeLogicGraph_OnEvent;
        MethodReference typeStack_GetForCurrentThread;

        const ulong HashMask = 0x000003FF; //TODO: this gives us max id of 1023

        internal UserlandBindingsGenerator(AssemblyDefinition asmDef, IEnumerable<string> defines)
        {
            var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(asmDef.FullName));
            var asmHash0 = BitConverter.ToUInt64(hash, 0);
            var asmHash1 = BitConverter.ToUInt64(hash, 2);
            asmHash = asmHash0 ^ asmHash1; //TODO: perhaps store this as assembly-global id to check for any (unlikely) collisions
            asmHash = asmHash & ~HashMask;

            System.Console.WriteLine($"[UserlandBindingsGenerator] Injecting bindings to {asmDef.FullName} ({asmHash})");

            this.asmDef = asmDef;

            typeVector3 = asmDef.MainModule.ImportReference(typeof(UnityEngine.Vector3));
            typeUnityObject = asmDef.MainModule.ImportReference(typeof(UnityEngine.Object));
            typeQuaternion = asmDef.MainModule.ImportReference(typeof(UnityEngine.Quaternion));
            typeUnityColor = asmDef.MainModule.ImportReference(typeof(UnityEngine.Color));
            typeNodeAPI = asmDef.MainModule.ImportReference(typeof(NodeAPIAttribute));

            typePreserve_Constructor = asmDef.MainModule.ImportReference(typeof(UnityEngine.Scripting.PreserveAttribute).GetConstructor(new Type[] { }));
            typeAlwaysLink_Constructor = asmDef.MainModule.ImportReference(typeof(UnityEngine.Scripting.AlwaysLinkAssemblyAttribute).GetConstructor(new Type[] { }));

            typeIStack = asmDef.MainModule.ImportReference(typeof(IStack));
            typeIStack_Pop = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("Pop"));
            typeIStack_PopBool = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("PopBool"));
            typeIStack_PopInt = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("PopInt"));
            typeIStack_PopFloat = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("PopFloat"));
            typeIStack_PopString = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("PopString"));
            typeIStack_PopVector3 = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("PopVector3"));
            typeIStack_PopUnityObject = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("PopObject"));
            typeIStack_PopQuaternion = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("PopQuaternion"));
            typeIStack_PopUnityColor = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("PopColor"));

            typeIStack_PushBool = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("PushBool"));
            typeIStack_PushInt = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("PushInt"));
            typeIStack_PushFloat = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("PushFloat"));
            typeIStack_PushString = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("PushString"));
            typeIStack_PushVector3 = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("PushVector3"));
            typeIStack_PushUnityObject = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("PushObject"));
            typeIStack_PushQuaternion = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("PushQuaternion"));
            typeIStack_PushUnityColor = asmDef.MainModule.ImportReference(typeof(IStack).GetMethod("PushColor"));

            typeLogicGraph_OnEvent = asmDef.MainModule.ImportReference(typeof(LogicGraphBindings).GetMethod("OnEvent"));
            typeStack_GetForCurrentThread = asmDef.MainModule.ImportReference(typeof(StackBindings).GetMethod("GetForCurrentThread"));
        }

        internal bool Inject()
        {
            var write = false;

            var typeDefs = asmDef.MainModule.Types.ToList();
            foreach (var typeDef in typeDefs)
            {
                write |= InjectForTypeRecursive(typeDef);
            }

            if (write)
            {
                var existing = asmDef.MainModule.GetCustomAttributes().SingleOrDefault(attr => attr.Constructor == typeAlwaysLink_Constructor);
                if (existing == null)
                {
                    System.Console.WriteLine($"[UserlandBindingsGenerator] Adding [AlwaysLinkAssembly] to {asmDef.FullName}");
                    asmDef.MainModule.CustomAttributes.Add(new CustomAttribute(typeAlwaysLink_Constructor));
                }
            }

            return write;
        }

        internal bool InjectForTypeRecursive(TypeDefinition typeDef)
        {
            var write = false;

            var methods = typeDef.Methods.ToList();
            foreach (var method in methods)
            {
                var attr = method.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == typeNodeAPI.FullName);
                if (attr == null)
                    continue;

                System.Console.WriteLine($"[UserlandBindingsGenerator] Found {method.FullName} in {asmDef.FullName}");
                if (method.IsPublic)
                {
                    InjectBindingForMethod(typeDef, method);
                    write = true;
                }
                else
                {
                    System.Console.WriteLine($"[UserlandBindingsGenerator] Skipping {method.FullName} in {asmDef.FullName} because it is not public.");
                }
            }

            var properties = typeDef.Properties.ToList();
            foreach (var property in properties)
            {
                var attr = property.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == typeNodeAPI.FullName);
                if (attr == null)
                    continue;

                var pub = false;
                System.Console.WriteLine($"[UserlandBindingsGenerator] Found {property.FullName} in {asmDef.FullName}");
                if (property.GetMethod != null && property.GetMethod.IsPublic)
                {
                    InjectBindingForMethod(typeDef, property.GetMethod);
                    write = true;
                    pub = true;
                }
                
                if (property.SetMethod != null && property.SetMethod.IsPublic)
                {
                    InjectBindingForMethod(typeDef, property.SetMethod);
                    write = true;
                    pub = true;
                }

                if (!pub)
                {
                    System.Console.WriteLine($"[UserlandBindingsGenerator] Skipping {property.FullName} in {asmDef.FullName} because it has no public methods.");
                }
            }

            var events = typeDef.Events.ToList();
            foreach (var evt in events)
            {
                var attr = evt.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == typeNodeAPI.FullName);
                if (attr == null)
                    continue;

                System.Console.WriteLine($"[UserlandBindingsGenerator] Found {evt.FullName} in {asmDef.FullName}");
                if (evt.AddMethod.IsPublic)
                {
                    ++currentEventId;
                    if (currentEventId > 1023)
                        throw new InvalidOperationException("UserlandBindingsGenerator can only do 1023 events per assembly.");
                    var recordedEventId = asmHash | (currentEventId & HashMask);
                    InjectBindingForEvent(typeDef, evt, unchecked((long)recordedEventId));
                    write = true;
                }
                else
                {
                    System.Console.WriteLine($"[UserlandBindingsGenerator] Skipping {evt.FullName} in {asmDef.FullName} because it is not public.");
                }
            }

            foreach (var nestedType in typeDef.NestedTypes)
            {
                write |= InjectForTypeRecursive(nestedType);
            }

            return write;
        }

        void InjectBindingForMethod(TypeDefinition typeDef, MethodDefinition method)
        {
            // *** Generate name
            var returnType = string.Empty; //$"{method.ReturnType.Name}_"; //Legacy: for override support
            var paramTypes = string.Empty;
            //foreach (var param in method.Parameters) //Legacy: for override support
            //{
            //    paramTypes = $"{paramTypes}_{param.ParameterType.Name}";
            //}
            var bindingName = $"{UserlandBindings.Prefix}CALL_{returnType}{method.Name}{paramTypes}";
            if (typeDef.Methods.Any(x => x.Name == bindingName))
                throw new InvalidOperationException($"UserlandBindingsGenerator trying to generate a duplicate binding for {method.FullName} in {asmDef.FullName}: {bindingName}");

            // *** Generate method
            var md = new MethodDefinition(bindingName, MethodAttributes.Static | MethodAttributes.HideBySig, asmDef.MainModule.TypeSystem.Void);
            md.CustomAttributes.Add(new CustomAttribute(typePreserve_Constructor)); // Preserve attribute (IL2CPP)
            typeDef.Methods.Add(md);

            //Legacy: for override support
            //var attributeConstructor = asmDef.MainModule.ImportReference(typeof(NodeBindingAttribute).GetConstructor(new Type[] { typeof(string), typeof(NodeBindingType) }));
            //var attribute = new CustomAttribute(attributeConstructor);
            //attribute.ConstructorArguments.Add(new CustomAttributeArgument(asmDef.MainModule.TypeSystem.String, method.Name));
            //attribute.ConstructorArguments.Add(new CustomAttributeArgument(typeNodeBindingType, NodeBindingType.NBT_Call));
            //md.CustomAttributes.Add(attribute);

            // Parameters
            md.Parameters.Add(new ParameterDefinition(asmDef.MainModule.TypeSystem.Object));
            md.Parameters[0].Name = "target";
            md.Parameters.Add(new ParameterDefinition(typeIStack));
            md.Parameters[1].Name = "context";

            ILProcessor processor = md.Body.GetILProcessor();

            // Add locals
            byte argOffset = 0;
            bool hasReturn = !method.ReturnType.IsSame(asmDef.MainModule.TypeSystem.Void);
            if (hasReturn)
            {
                argOffset = 1;
                md.Body.Variables.Add(new VariableDefinition(method.ReturnType));
            }

            foreach (var param in method.Parameters)
            {
                var localType = param.ParameterType.GetElementType();
                var popRef = TypeToStackPopMethod(localType);
                md.Body.Variables.Add(new VariableDefinition(localType));
            }

#if INJECT_LOGGING
            {
                var debugLogMethod = typeof(UnityEngine.Debug).GetMethod("Log", new System.Type[] { typeof(string) });
                var writeLine = asmDef.MainModule.ImportReference(debugLogMethod);

                processor.Append(processor.Create(OpCodes.Ldstr, $"This is a LogicGraph generated binding for {method.Name}"));
                processor.Append(processor.Create(OpCodes.Call, writeLine));
            }
#endif

            var isExtensionMethod = method.CustomAttributes.Any(x => x.AttributeType.Name == nameof(ExtensionAttribute));

            // Pop values from IStack into locals
            byte localIndex = argOffset;
            foreach (var param in method.Parameters)
            {
                if (param.IsOut) // out arguments are not expected to be provided
                {
                    ++localIndex;
                    continue;
                }
                
                MethodReference popRef = null;
                if (param.Sequence == 0 && isExtensionMethod)
                {
                    processor.Append(processor.Create(OpCodes.Ldarg_0));
                }
                else
                {
                    processor.Append(processor.Create(OpCodes.Ldarg_1));
                    popRef = TypeToStackPopMethod(param.ParameterType);
                    processor.Append(processor.Create(OpCodes.Callvirt, popRef));
                }

                if (popRef == typeIStack_PopUnityObject && !param.ParameterType.IsSame(typeUnityObject)) // type-safety check for UnityEngine.Object derivatives
                {
                    processor.Append(processor.Create(OpCodes.Castclass, param.ParameterType));
                    processor.Append(processor.Create(OpCodes.Stloc_S, localIndex));
                }
                else
                {
                    processor.Append(processor.Create(OpCodes.Stloc_S, localIndex)); // implied type-safety check 
                }

                ++localIndex;
            }

            // Load the first arg into stack, cast it from object to our type, and call the intended method
            if (!method.IsStatic)
            {
                processor.Append(processor.Create(OpCodes.Ldarg_0));
                processor.Append(processor.Create(OpCodes.Castclass, method.DeclaringType));
            }

            // Load arguments into stack
            localIndex = argOffset;
            foreach (var param in method.Parameters)
            {
                if (param.IsOut)
                {
                    processor.Append(processor.Create(OpCodes.Ldloca_S, localIndex)); // Load address of local
                }
                else
                {
                    processor.Append(processor.Create(OpCodes.Ldloc_S, localIndex)); // Load local
                }
                ++localIndex;
            }

            // Call the actual method
            processor.Append(processor.Create(OpCodes.Call, method));

            // Return value
            if (hasReturn)
            {
                processor.Append(processor.Create(OpCodes.Stloc_0));

                processor.Append(processor.Create(OpCodes.Ldarg_1));
                processor.Append(processor.Create(OpCodes.Ldloc_0));

                var pushRef = TypeToStackPushMethod(method.ReturnType);
                processor.Append(processor.Create(OpCodes.Callvirt, pushRef));
            }

            // Out parameters
            localIndex = argOffset;
            foreach (var param in method.Parameters)
            {
                if (param.IsOut)
                {
                    processor.Append(processor.Create(OpCodes.Ldarg_1));
                    processor.Append(processor.Create(OpCodes.Ldloc_S, localIndex)); // Load local

                    var localType = param.ParameterType.GetElementType();
                    var pushRef = TypeToStackPushMethod(localType);
                    processor.Append(processor.Create(OpCodes.Callvirt, pushRef));
                }
                ++localIndex;
            }

            processor.Append(processor.Create(OpCodes.Ret));
        }

        void InjectEventIdGetterMethod(TypeDefinition typeDef, EventDefinition evt, long eventId)
        {
            // *** Generate name
            var methodName = $"{UserlandBindings.Prefix}GET_EVENTID_{evt.Name}";
            if (typeDef.Methods.Any(x => x.Name == methodName))
                throw new InvalidOperationException($"UserlandBindingsGenerator trying to generate a duplicate method for {evt.FullName} in {asmDef.FullName}: {methodName}");

            // *** Generate method
            var md = new MethodDefinition(methodName, MethodAttributes.Static | MethodAttributes.HideBySig, asmDef.MainModule.TypeSystem.Int64);
            md.CustomAttributes.Add(new CustomAttribute(typePreserve_Constructor)); // Preserve attribute (IL2CPP)
            typeDef.Methods.Add(md);

            ILProcessor processor = md.Body.GetILProcessor();
            processor.Append(processor.Create(OpCodes.Ldc_I8, eventId));
            processor.Append(processor.Create(OpCodes.Ret));
        }

        void InjectBindingForEvent(TypeDefinition typeDef, EventDefinition evt, long eventId)
        {
            InjectEventIdGetterMethod(typeDef, evt, eventId); // NOTE: currently generating a getter function, as static fields are more tricky to initialize.

            // *** Generate Event handler method
            var handlerName = $"{UserlandBindings.Prefix}HANDLE_{evt.Name}";
            if (typeDef.Methods.Any(x => x.Name == handlerName))
                throw new InvalidOperationException($"UserlandBindingsGenerator trying to generate a duplicate handler method for {evt.FullName} in {asmDef.FullName}: {handlerName}");
            var md = new MethodDefinition(handlerName, MethodAttributes.Public | MethodAttributes.HideBySig, asmDef.MainModule.TypeSystem.Void);
            md.CustomAttributes.Add(new CustomAttribute(typePreserve_Constructor)); // Preserve attribute (IL2CPP)
            md.Body.InitLocals = true;
            typeDef.Methods.Add(md);

            // Parameters
            var parameters = ExtractEventDelegateArgumentTypes(evt.EventType);
            foreach (var param in parameters)
            {
                var localType = param.GetElementType();
                md.Parameters.Add(new ParameterDefinition(localType));
            }

            ILProcessor processor = md.Body.GetILProcessor();

            // Get IStack into a variable
            var varStack = new VariableDefinition(typeIStack);
            md.Body.Variables.Add(varStack);
            processor.Append(processor.Create(OpCodes.Call, typeStack_GetForCurrentThread));
            processor.Append(processor.Create(OpCodes.Stloc, varStack));

            // Push values from args into IStack
            byte localIndex = 1; // Offset by one due to 'this'
            foreach (var param in parameters)
            {
                processor.Append(processor.Create(OpCodes.Ldloc, varStack));
                processor.Append(processor.Create(OpCodes.Ldarg_S, localIndex));

                var pushRef = TypeToStackPushMethod(param);
                processor.Append(processor.Create(OpCodes.Callvirt, pushRef));

                ++localIndex;
            }

            // Call LogicGraph.OnEvent(sender, eventId)
            processor.Append(processor.Create(OpCodes.Ldarg_0));
            processor.Append(processor.Create(OpCodes.Ldc_I8, eventId));
            processor.Append(processor.Create(OpCodes.Call, typeLogicGraph_OnEvent));

            // Pop values from IStack
            foreach (var param in parameters)
            {
                processor.Append(processor.Create(OpCodes.Ldloc, varStack));
                processor.Append(processor.Create(OpCodes.Callvirt, typeIStack_Pop));
            }

            processor.Append(processor.Create(OpCodes.Ret));
        }

        static List<TypeReference> ExtractEventDelegateArgumentTypes(TypeReference delegateType)
        {
            List<TypeReference> parameters;
            var delegateGenInst = delegateType as GenericInstanceType;
            if (delegateGenInst != null)
            {
                parameters = delegateGenInst.GenericArguments.ToList();
            }
            else
            {
                var delegateTypeDef = delegateType.Resolve();
                var invokeMethod = delegateTypeDef.Methods.Single(m => m.Name.Equals("Invoke"));
                parameters = invokeMethod.Parameters.Select(x => x.ParameterType).ToList();
            }
            return parameters;
        }

        bool InheritsUnityObject(TypeReference typeReference)
        {
            var td = typeReference.Resolve();
            while (td.BaseType != null)
            {
                if (td.BaseType.IsSame(typeUnityObject))
                    return true;
                td = td.BaseType.Resolve();
            }
            return false;
        }

        MethodReference TypeToStackPopMethod(TypeReference typeReference)
        {
            if (typeReference.IsSame(asmDef.MainModule.TypeSystem.Boolean))
                return typeIStack_PopBool;
            else if (typeReference.IsSame(asmDef.MainModule.TypeSystem.Int32))
                return typeIStack_PopInt;
            else if (typeReference.IsSame(asmDef.MainModule.TypeSystem.Single))
                return typeIStack_PopFloat;
            else if (typeReference.IsSame(asmDef.MainModule.TypeSystem.String))
                return typeIStack_PopString;
            else if (typeReference.IsSame(typeVector3))
                return typeIStack_PopVector3;
            else if (typeReference.IsSame(typeUnityObject) || InheritsUnityObject(typeReference))
                return typeIStack_PopUnityObject;
            else if (typeReference.IsSame(typeQuaternion))
                return typeIStack_PopQuaternion;
            else if (typeReference.IsSame(typeUnityColor))
                return typeIStack_PopUnityColor;
            else
                throw new NotSupportedException($"[UserlandBindingsGenerator] Parameter type {typeReference.Name} is not supported.");
        }

        MethodReference TypeToStackPushMethod(TypeReference typeReference)
        {
            if (typeReference.IsSame(asmDef.MainModule.TypeSystem.Boolean))
                return typeIStack_PushBool;
            else if (typeReference.IsSame(asmDef.MainModule.TypeSystem.Int32))
                return typeIStack_PushInt;
            else if (typeReference.IsSame(asmDef.MainModule.TypeSystem.Single))
                return typeIStack_PushFloat;
            else if (typeReference.IsSame(asmDef.MainModule.TypeSystem.String))
                return typeIStack_PushString;
            else if (typeReference.IsSame(typeVector3))
                return typeIStack_PushVector3;
            else if (typeReference.IsSame(typeUnityObject) || InheritsUnityObject(typeReference))
                return typeIStack_PushUnityObject;
            else if (typeReference.IsSame(typeQuaternion))
                return typeIStack_PushQuaternion;
            else if (typeReference.IsSame(typeUnityColor))
                return typeIStack_PushUnityColor;
            else
                throw new NotSupportedException($"[UserlandBindingsGenerator] Parameter type {typeReference.Name} is not supported.");
        }
    }

    static class TypeReferenceExtensions
    {
        public static bool IsSame(this TypeReference lhs, TypeReference rhs)
        {
            if (lhs.FullName != rhs.FullName)
                return false;

            return true;
        }
    }
}
