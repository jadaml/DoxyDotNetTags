using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using static DoxyTags.DoxyTag;
using static System.Console;
using static System.IO.Path;
using static System.String;
using Version = System.Version;

namespace DoxyTags
{
    internal static class Program
    {
        private const string m_urlBase = "https://docs.microsoft.com/en-us/dotnet/api/";

        private static Version m_fwVersion = new Version(4, 0);
        private static string m_view;
        private static int m_typeIndex;
        private static int m_typeCount;

        static void Main(string[] args)
        {
            string output = null;

            if (args.Length > 0)
            {
                output = args[0];
            }
            if (args.Length > 1 && Version.TryParse(args[1], out Version inputVer))
            {
                m_fwVersion = inputVer;
            }

            m_view = $"netframework-{m_fwVersion}";
            Assembly[] asms = (from asmf in Directory.GetFiles(Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET", "Assembly", "GAC_MSIL"), "*.dll", SearchOption.AllDirectories)
                               where !GetFileNameWithoutExtension(asmf).Equals("Microsoft.Isam.Esent.Interop")
                               let asm = LoadAssembly(asmf)
                               where asm is Assembly
                                  && (asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company?.Equals("Microsoft Corporation") ?? false)
                                  && asm.GlobalAssemblyCache
                                  && !asm.IsDynamic
                                  && asm.CodeBase.StartsWith(new Uri(Environment.GetFolderPath(Environment.SpecialFolder.Windows)).AbsoluteUri, StringComparison.CurrentCultureIgnoreCase)
                               select asm)
                              .Append(typeof(object).Assembly)
                              .OrderBy(asm => asm, AssemblyPriority.Comparer)
                              .ToArray();

            var types = from asm in asms
                        from type in GetTypes(asm)
                        select type;
            var nstypes = from type in types
                                      .Append(typeof(object))
                          where type.IsPublic
                          orderby (type.Name, type.IsGenericTypeDefinition)
                          group type by type.Namespace into ns
                          orderby ns.Key
                          select ns;

            m_typeIndex = 0;
            m_typeCount = nstypes.Sum(grp => grp.Count());

            Trace.WriteLine($"Type count = {m_typeCount}", "DoxyTag");

            using (TextWriter txtw = IsNullOrEmpty(output)
                                   ? new StringWriter() as TextWriter
                                   : new StreamWriter(output, false, new UTF8Encoding(false)))
            using (XmlTextWriter writer = new XmlTextWriter(txtw))
            {
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 2;
                writer.WriteStartDocument(true);
                writer.WriteStartElement("tagfile");

                foreach (var ns in nstypes)
                {
                    IEnumerable<Type> iface, clasz, strux;

                    iface = from t in ns
                            where t.IsInterface
                            select t;
                    clasz = from t in ns
                            where t.IsClass
                            select t;
                    strux = from t in ns
                            where t.IsValueType
                               && !t.IsEnum
                            select t;

                    ProduceNameSpace(writer, ns);

                    foreach (Type t in iface) ProduceType(writer, t);
                    foreach (Type t in strux) ProduceType(writer, t);
                    foreach (Type t in clasz) ProduceType(writer, t);
                }

                writer.WriteEndElement();

                CursorLeft = 0;
                Write(new string(' ', WindowWidth - 1));
                CursorLeft = 0;

                if (txtw is StringWriter) WriteLine(txtw.ToString());
                else WriteLine($"{output}={m_urlBase}");
            }
        }

        private static Assembly LoadAssembly(string file)
        {
            if (GetFileNameWithoutExtension(file).Equals("Microsoft.SmartDevice.ConnectivityWrapper.11", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            try
            {
                return Assembly.LoadFile(file);
            }
            catch (Exception ex) when (ex is IOException || ex is BadImageFormatException)
            {
                return null;
            }
        }

        private static Type[] GetTypes(Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException)
            {
                return Array.Empty<Type>();
            }
        }

        private static void ProduceNameSpace(XmlWriter writer, IGrouping<string, Type> ns)
        {
            writer.WriteStartElement("compound");
            writer.WriteAttributeString("kind", "namespace");

            writer.WriteElementString("name", CleanDoxy(ns.Key));
            string enumFile = $"{CleanUrl(ns.Key)}?view={m_view}";
            writer.WriteElementString("filename", enumFile);

            foreach (Type type in ns.Where(t => !t.IsEnum))
            {
                writer.WriteStartElement("class");
                /**/ if (type.IsClass)     writer.WriteAttributeString("kind", "class");
                else if (type.IsInterface) writer.WriteAttributeString("kind", "interface");
                else                       writer.WriteAttributeString("kind", "struct");
                writer.WriteString(CleanDoxy(type.GetDefinedName()));
                writer.WriteEndElement();
            }

            foreach (Type type in ns.Where(t => t.IsEnum))
            {
                writer.WriteStartElement("member");
                writer.WriteAttributeString("kind", "enumeration");
                writer.WriteElementString("type", CleanDoxy(Enum.GetUnderlyingType(type).GetDefinedName()));
                string typeName = type.GetDefinedName();
                writer.WriteElementString("name", CleanDoxy(typeName));
                writer.WriteElementString("anchorfile", CleanUrl(typeName));
                writer.WriteElementString("anchor", null);
                writer.WriteElementString("arglist", null);
                foreach (string enumValue in Enum.GetNames(type))
                {
                    writer.WriteStartElement("enumvalue");
                    writer.WriteAttributeString("file", enumFile);
                    writer.WriteAttributeString("anchor", CleanAnchor($"{typeName}-{enumValue}"));
                    writer.WriteString(CleanDoxy(enumValue));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private static void ProduceMember(XmlWriter writer, MemberInfo member)
        {
            writer.WriteStartElement("member");

            switch (member)
            {
                case FieldInfo fi:
                    writer.WriteAttributeString("kind", "field");
                    if (fi.IsFamily) writer.WriteAttributeString("protection", "protected");
                    if (fi.IsStatic) writer.WriteAttributeString("static", "yes");
                    writer.WriteElementString("type", CleanDoxy(fi.FieldType.GetDefinedName()));
                    break;
                case MethodInfo mi:
                    writer.WriteAttributeString("kind", "function");
                    if (mi.IsFamily) writer.WriteAttributeString("protection", "protected");
                    if (mi.IsStatic) writer.WriteAttributeString("static", "yes");
                    if (mi.IsVirtual) writer.WriteAttributeString("virtualness", "virtual");
                    writer.WriteElementString("type", mi.ReturnType == typeof(void) ? "" : CleanDoxy(mi.ReturnType.GetDefinedName()));
                    break;
                case PropertyInfo pi:
                    writer.WriteAttributeString("kind", "property");
                    if (pi.CanRead && pi.GetMethod.IsFamily || pi.CanWrite && pi.SetMethod.IsFamily)
                    {
                        writer.WriteAttributeString("protection", "protected");
                    }
                    if (pi.CanRead && pi.GetMethod.IsStatic || pi.CanWrite && pi.SetMethod.IsStatic)
                    {
                        writer.WriteAttributeString("static", "yes");
                    }
                    if (pi.CanRead && pi.GetMethod.IsVirtual || pi.CanWrite && pi.SetMethod.IsVirtual)
                    {
                        writer.WriteAttributeString("virtualness", "virtual");
                    }
                    writer.WriteElementString("type", CleanDoxy(pi.PropertyType.GetDefinedName()));
                    break;
                case EventInfo ei:
                    writer.WriteAttributeString("kind", "event");
                    if (ei.AddMethod.IsFamily || ei.RemoveMethod.IsFamily)
                    {
                        writer.WriteAttributeString("protection", "protected");
                    }
                    if (ei.AddMethod.IsStatic || ei.RemoveMethod.IsStatic)
                    {
                        writer.WriteAttributeString("static", "yes");
                    }
                    if (ei.AddMethod.IsVirtual || ei.RemoveMethod.IsVirtual)
                    {
                        writer.WriteAttributeString("virtualness", "virtual");
                    }
                    writer.WriteElementString("type", CleanDoxy(ei.EventHandlerType.GetDefinedName()));
                    break;
            }

            writer.WriteElementString("name", member.Name);

            writer.WriteElementString("anchorfile", member.GetAnchorFile());
            writer.WriteElementString("anchor", member.GetAnchor());

            switch (member)
            {
                case MethodInfo mi:
                    writer.WriteElementString("arglist", $"({Join(", ", mi.GetArgList())})");
                    break;
                case PropertyInfo pi:
                    writer.WriteElementString("arglist", "");
                    break;
                case EventInfo ei:
                    writer.WriteElementString("arglist", "");
                    break;
            }

            writer.WriteEndElement();
        }

        private static void ProduceType(XmlWriter writer, Type t)
        {
            writer.WriteStartElement("compound");

            /**/ if (t.IsInterface) writer.WriteAttributeString("kind", "interface");
            else if (t.IsValueType) writer.WriteAttributeString("kind", "struct");
            else                    writer.WriteAttributeString("kind", "class");

            string typeName = t.GetDefinedName();

            writer.WriteElementString("name", CleanDoxy(typeName));
            writer.WriteElementString("filename", $"{CleanUrl(typeName)}?view={m_view}");

            if (t.IsGenericTypeDefinition) foreach (Type genType in t.GetGenericArguments())
            {
                writer.WriteElementString("templarg", genType.Name);
            }

            if (t != typeof(object)
             && t.BaseType is Type
             && t.BaseType != typeof(object))
            {
                string baseTypeName = CleanDoxy(t.BaseType.GetDefinedName());
                if (t.BaseType.IsGenericType)
                {
                    baseTypeName = $"{baseTypeName}< {Join(", ", from gt in t.BaseType.GetGenericArguments() select gt.GetDefinedName())} >";
                }
                writer.WriteElementString("base", CleanDoxy(t.BaseType.GetDefinedName()));
            }

            Type[] ifaces = t.GetInterfaces();
            foreach (Type it in t.GetInterfaces())
            {
                if (it.TypeIsNotPublic() || it.InterfaceInherited(t.BaseType, ifaces)) continue;
                string baseTypeName = CleanDoxy(it.GetDefinedName());
                if (it.IsGenericType)
                {
                    baseTypeName = $"{baseTypeName}< {Join(", ", from gt in it.GetGenericArguments() select gt.GetDefinedName())} >";
                }
                writer.WriteElementString("base", CleanDoxy(baseTypeName));
            }

            IEnumerable<MemberInfo> members = from mi in t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                                              where (!(mi as MethodInfo)?.IsSpecialName ?? true)
                                                 && mi.DeclaringType == t
                                              orderby mi.Name
                                              select mi;

            IEnumerable<FieldInfo>    fis = members.OfType<FieldInfo>();
            IEnumerable<MethodInfo>   cis = members.OfType<MethodInfo>().Where(mi => mi.IsConstructor);
            IEnumerable<MethodInfo>   mis = members.OfType<MethodInfo>().Where(mi => !mi.IsConstructor);
            IEnumerable<PropertyInfo> pis = members.OfType<PropertyInfo>();
            IEnumerable<EventInfo>    eis = members.OfType<EventInfo>();

            foreach (FieldInfo    fi in fis) ProduceMember(writer, fi);
            foreach (MethodInfo   mi in cis) ProduceMember(writer, mi);
            foreach (MethodInfo   mi in mis) ProduceMember(writer, mi);
            foreach (PropertyInfo pi in pis) ProduceMember(writer, pi);
            foreach (EventInfo    ei in eis) ProduceMember(writer, ei);

            writer.WriteEndElement();
            ConsoleProgress.Percent = (double)++m_typeIndex / m_typeCount;
        }
    }
}
