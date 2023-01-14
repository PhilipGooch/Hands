using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Core.GameSystems
{
    public static class TypeManager
    {
        public static bool IsSystemAGroup(Type t)
        {
            return t.IsSubclassOf(typeof(GameSystemGroup));
        }

        public static Attribute[] GetSystemAttributes(Type systemType, Type attributeType)
        {
            Attribute[] attributes;
            var kDisabledCreationAttribute = typeof(DisableAutoCreationAttribute);
            if (attributeType == kDisabledCreationAttribute)
            {
                // We do not want to inherit DisableAutoCreation from some parent type (as that attribute explicitly states it should not be inherited)
                var objArr = systemType.GetCustomAttributes(attributeType, false);
                var attrList = new List<Attribute>();

                var alreadyDisabled = false;
                for (int i = 0; i < objArr.Length; i++)
                {
                    var attr = objArr[i] as Attribute;
                    attrList.Add(attr);

                    if (attr.GetType() == kDisabledCreationAttribute)
                        alreadyDisabled = true;
                }

                if (!alreadyDisabled && systemType.Assembly.GetCustomAttributes(attributeType, true) != null)
                {
                    attrList.Add(new DisableAutoCreationAttribute());
                }
                attributes = attrList.ToArray();
            }
            else
            {
                var objArr = systemType.GetCustomAttributes(attributeType, true);
                attributes = new Attribute[objArr.Length];
                for (int i = 0; i < objArr.Length; i++)
                {
                    attributes[i] = objArr[i] as Attribute;
                }
            }

            return attributes;
        }

        internal static bool FilterSystemType(Type type)
        {
            // the entire assembly can be marked for no-auto-creation (test assemblies are good candidates for this)
            var disableAllAutoCreation = Attribute.IsDefined(type.Assembly, typeof(DisableAutoCreationAttribute));
            var disableTypeAutoCreation = Attribute.IsDefined(type, typeof(DisableAutoCreationAttribute), false);

            // these types obviously cannot be instantiated
            if (type.IsAbstract || type.ContainsGenericParameters)
            {
                if (disableTypeAutoCreation)
                    Debug.LogWarning($"Invalid [DisableAutoCreation] on {type.FullName} (only concrete types can be instantiated)");

                return false;
            }

            // only derivatives of ComponentSystemBase and structs implementing ISystemBase are systems
            if (!type.IsSubclassOf(typeof(GameSystemBase)))
                throw new System.ArgumentException($"{type} must already be filtered by {nameof(GameSystemBase)}");

            // the auto-creation system instantiates using the default ctor, so if we can't find one, exclude from list
            if (type.IsClass && type.GetConstructor(Type.EmptyTypes) == null)
            {
                // we want users to be explicit
                if (!disableTypeAutoCreation && !disableAllAutoCreation)
                    Debug.LogWarning($"Missing default ctor on {type.FullName} (or if you don't want this to be auto-creatable, tag it with [DisableAutoCreation])");

                return false;
            }

            if (disableTypeAutoCreation || disableAllAutoCreation)
            {
                if (disableTypeAutoCreation && disableAllAutoCreation)
                    Debug.LogWarning($"Redundant [DisableAutoCreation] on {type.FullName} (attribute is already present on assembly {type.Assembly.GetName().Name}");

                return false;
            }

            return true;
        }
    }
}
