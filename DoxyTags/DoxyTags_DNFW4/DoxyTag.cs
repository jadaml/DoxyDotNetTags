using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using static System.String;

namespace DoxyTags
{
    internal static class DoxyTag
    {
        static readonly Regex regxSeparators = new Regex(@"[\.\+]");
        static readonly Regex regxGenericCnt = new Regex(@"`\d+");
        static readonly Regex regxAnchorInvl = new Regex(@"[\.\+:, _]+");

        internal static string CleanAnchor(string value)
        {
            value = value         .ToLower();
            value = regxSeparators.Replace(value, "-");
            value = regxGenericCnt.Replace(value, "");
            value = regxAnchorInvl.Replace(value, "-");
            value = value         .Replace('[', '(');
            value = value         .Replace(']', ')');
            value = value         .Replace("&", "@");
            return value;
        }

        internal static string CleanDoxy(string typeName)
        {
            typeName = regxSeparators.Replace(typeName, "::");
            typeName = regxGenericCnt.Replace(typeName, "");
            typeName = typeName.Replace("[[", "< ");
            typeName = typeName.Replace("]]", " >");
            return typeName;
        }

        internal static string CleanUrl(string typeName) => typeName.ToLower().Replace('`', '-');

        internal static string GetAnchor(this MemberInfo member)
        {
            return member is MethodInfo mi
                 ? GetAnchor(mi)
                 : CleanAnchor($"{member.DeclaringType.GetDefinedName()}-{member.Name}");
        }

        internal static string GetAnchorFile(this MemberInfo member)
        {
            return CleanUrl($"{member.DeclaringType.GetDefinedName()}.{member.Name}");
        }

        internal static IEnumerable<string> GetArgList(this MethodInfo mi)
        {
            return from pi in mi.GetParameters()
                   select $"{GetArgModifier(pi)}{pi.ParameterType.Name.Replace("&", "")} {pi.Name}";
        }

        private static string GetAnchor(MethodInfo member)
        {
            return CleanAnchor($"{member.DeclaringType.GetDefinedName()}-{member.Name}{GetAnchorMethodSign(member)}");
        }

        private static string GetAnchorMethodArgs(ParameterInfo pi)
        {
            return pi.ParameterType.IsGenericParameter
                 ? $"-{pi.ParameterType.GenericParameterPosition}"
                 : pi.ParameterType.GetDefinedName();
        }

        private static string GetAnchorMethodSign(MethodInfo member)
        {
            ParameterInfo[] args = member.GetParameters();
            if (args.Length == 0) return null;
            return $"({Join("-", args.Select(GetAnchorMethodArgs)).Replace("--", "-")})";
        }

        private static string GetArgModifier(ParameterInfo pi)
        {
            if (pi.IsOut) return "out ";
            if (pi.ParameterType.IsByRef) return "ref ";
            return null;
        }
    }
}
