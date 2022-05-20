using System;
using System.Collections.Generic;
using System.Linq;
using static System.String;

namespace DoxyTags
{
    internal static class TypeAnalisis
    {
        private static readonly Queue<Type> ifaceQueue = new Queue<Type>();

        internal static bool TypeIsNotPublic(this Type type)
        {
            for (; type is Type; type = type.DeclaringType)
            {
                if (type.IsNotPublic) return true;
            }
            return false;
        }

        internal static bool InterfaceInherited(this Type iface, Type baseType, Type[] ifaces)
        {
            ifaceQueue.Clear();

            if (baseType is Type) foreach (Type diface in baseType.GetInterfaces())
                {
                    ifaceQueue.Enqueue(diface);
                }

            foreach (Type diface in from it in ifaces
                                    from bit in it.GetInterfaces()
                                    select bit)
            {
                ifaceQueue.Enqueue(diface);
            }

            while (ifaceQueue.Count > 0)
            {
                Type it = ifaceQueue.Dequeue();
                if (it == iface) return true;

                foreach (Type diface in it.GetInterfaces()) ifaceQueue.Enqueue(diface);
            }

            return false;
        }

        internal static string GetDefinedName(this Type t)
        {
            return t.IsGenericParameter
                 ? t.Name
                 : t.IsGenericType && !t.IsGenericTypeDefinition
                 ? $"{t.Namespace}.{t.Name}[[{Join(", ", t.GetGenericArguments().Select(GetDefinedName))}]]"
                 : t.FullName ?? $"{t.Namespace}.{t.Name}";
        }
    }
}
