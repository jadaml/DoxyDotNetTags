using System;
using System.Collections.Generic;
using System.Reflection;
using static System.IO.Path;

namespace DoxyTags
{
    internal class AssemblyPriority : IComparer<Assembly>
    {
        public static readonly AssemblyPriority Comparer = new AssemblyPriority();

        public int Compare(Assembly asmA, Assembly asmB)
        {
            if (asmA is null) throw new ArgumentNullException(nameof(asmA));
            if (asmB is null) throw new ArgumentNullException(nameof(asmB));

            string fnA = GetFileNameWithoutExtension(asmA.CodeBase);
            string fnB = GetFileNameWithoutExtension(asmB.CodeBase);

            if (fnA.Equals(fnB, StringComparison.Ordinal)) return 0;

            if (fnA.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
             && !fnA.Equals(fnB, StringComparison.Ordinal))
            {
                return -1;
            }

            if (fnB.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
             && !fnA.Equals(fnB, StringComparison.Ordinal))
            {
                return 1;
            }

            if (fnA.StartsWith("System", StringComparison.OrdinalIgnoreCase)
             && !fnB.StartsWith("System", StringComparison.OrdinalIgnoreCase))
            {
                return -1;
            }

            if (!fnA.StartsWith("System", StringComparison.OrdinalIgnoreCase)
             && fnB.StartsWith("System", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            return fnA.CompareTo(fnB);
        }
    }
}
