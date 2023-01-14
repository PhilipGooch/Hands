using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using NBG.Core.CodeGen;

namespace NBG.Net.CodeGen
{
    /// <summary>
    /// Generates a helper function on a class which implements IEventSerializer<T> and is marked with [NetEventBusSerializer].
    /// Serializers expect events to be value-types, hence this extra complexity.
    /// 
    /// In normal JIT environments a call to a generic method could be achieved using reflection.
    /// In AOT/IL2CPP environments such code will not be emitted automatically.
    /// 
    /// </summary>
    /// <code>
    /// [UnityEngine.Scripting.Preserve]
    /// void _NetEventBus_EventRegistrator_(NetEventBus netEventBus, uint32 netId)
    /// {
    ///     netEventBus.Declare<T>(netId, this);
    /// }
    /// </code>
    class NetEventBusRegistratorGenerator
    {
        AssemblyDefinition asmDef;
        TypeReference typeIEventSerializer;
        TypeReference typeNetEventBus;
        MethodReference typeNetEventBus_Declare;
        TypeReference typeNetEventBusSerializer;
        MethodReference typePreserve_Constructor;

        internal NetEventBusRegistratorGenerator(AssemblyDefinition asmDef, IEnumerable<string> defines)
        {
            System.Console.WriteLine($"[NetEventBusRegistratorGenerator] Injecting bindings to {asmDef.FullName}");

            this.asmDef = asmDef;
            this.typeIEventSerializer = asmDef.MainModule.ImportReference(typeof(NBG.Core.Events.IEventSerializer<>));
            this.typeNetEventBus = asmDef.MainModule.ImportReference(typeof(NBG.Net.Systems.NetEventBus));
            this.typeNetEventBus_Declare = asmDef.MainModule.ImportReference(typeof(NBG.Net.Systems.NetEventBus).GetMethod("Declare"));
            this.typeNetEventBusSerializer = asmDef.MainModule.ImportReference(typeof(NBG.Net.NetEventBusSerializerAttribute));
            this.typePreserve_Constructor = asmDef.MainModule.ImportReference(typeof(UnityEngine.Scripting.PreserveAttribute).GetConstructor(new Type[] { }));
        }

        internal bool Inject()
        {
            var write = false;

            var typeDefs = asmDef.MainModule.Types.ToList();
            foreach (var typeDef in typeDefs)
            {
                write |= InjectForTypeRecursive(typeDef);
            }

            return write;
        }

        internal bool InjectForTypeRecursive(TypeDefinition typeDef)
        {
            var write = false;

            var attr = typeDef.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == typeNetEventBusSerializer.FullName);
            if (attr != null)
            {
                System.Console.WriteLine($"[{nameof(NetEventBusRegistratorGenerator)}] Found {typeDef.FullName} in {asmDef.FullName}");
                InjectForType(asmDef, typeDef);

                write = true;
            }

            foreach (var nestedType in typeDef.NestedTypes)
            {
                write |= InjectForTypeRecursive(nestedType);
            }

            return write;
        }

        void InjectForType(AssemblyDefinition asmDef, TypeDefinition typeDef)
        {
            var returnType = string.Empty;
            var paramTypes = string.Empty;
            if (typeDef.Methods.Any(x => x.Name == NBG.Net.Systems.NetEventBus.RegistratorFuncName))
                throw new InvalidOperationException($"{nameof(NetEventBusRegistratorGenerator)} trying to insert a duplicate registrator function for {typeDef.FullName} in {asmDef.FullName}.");

            MethodReference targetMethodType = null;
            try
            {
                // Find the IEventSerializer<T> interface this type implements
                TypeReference iface = FindInterface(typeDef, typeIEventSerializer);
                // Find the actual type of T
                var giface = new GenericInstanceType(iface);
                var get = (GenericInstanceType)giface.ElementType;
                TypeReference eventTypeRef = get.GenericArguments[0];
                // Build Declare<T> with the actual type of T
                targetMethodType = typeNetEventBus_Declare.MakeGenericInstanceMethod(eventTypeRef);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"{nameof(NetEventBusRegistratorGenerator)} failed to determine the event structure type for {typeDef.FullName} in {asmDef.FullName}.");
            }

            // *** Generate method
            var md = new MethodDefinition(NBG.Net.Systems.NetEventBus.RegistratorFuncName, MethodAttributes.Public | MethodAttributes.HideBySig, asmDef.MainModule.TypeSystem.Void);
            md.CustomAttributes.Add(new CustomAttribute(typePreserve_Constructor)); // Preserve attribute (IL2CPP)
            typeDef.Methods.Add(md);

            // Parameters
            md.Parameters.Add(new ParameterDefinition(typeNetEventBus));
            md.Parameters[0].Name = "netEventBus";
            md.Parameters.Add(new ParameterDefinition(asmDef.MainModule.TypeSystem.UInt32));
            md.Parameters[1].Name = "netId";

            // Code
            ILProcessor processor = md.Body.GetILProcessor();
            processor.Append(processor.Create(OpCodes.Nop));
            processor.Append(processor.Create(OpCodes.Ldarg_1));
            processor.Append(processor.Create(OpCodes.Ldarg_2));
            processor.Append(processor.Create(OpCodes.Ldarg_0));
            processor.Append(processor.Create(OpCodes.Callvirt, targetMethodType));
            processor.Append(processor.Create(OpCodes.Nop));
            processor.Append(processor.Create(OpCodes.Ret));
        }

        TypeReference FindInterface(TypeDefinition typeDef, TypeReference interfaceRef)
        {
            foreach (var ifaceImpl in typeDef.Interfaces)
            {
                var iface = ifaceImpl.InterfaceType;
                if (iface.IsSame(interfaceRef))
                {
                    return iface;
                }
                
                if (iface.IsGenericInstance)
                {
                    var giface = new GenericInstanceType(iface);
                    var ifaceTypeRef = giface.ElementType.Resolve();
                    if (ifaceTypeRef.IsSame(interfaceRef))
                    {
                        return iface;
                    }
                }
            }

            var baseType = typeDef.BaseType;
            if (baseType != null)
            {
                return FindInterface(baseType.Resolve(), interfaceRef);
            }
            else
            {
                return null;
            }
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
