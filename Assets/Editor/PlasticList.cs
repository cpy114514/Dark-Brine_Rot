using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Mavis
{
    public static class PlasticList
    {
        public static void Run()
        {
            var sb = new StringBuilder();
            try
            {
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                Assembly plasticAsm = null;
                foreach (var a in asms)
                    if (a.GetName().Name == "Unity.PlasticSCM.Editor")
                    {
                        plasticAsm = a;
                        break;
                    }
                if (plasticAsm == null) { sb.AppendLine("NO_ASM"); }
                else
                {
                    int n = 0;
                    foreach (var t in plasticAsm.GetTypes())
                    {
                        if (t.IsClass && t.IsAbstract && t.IsSealed) // static only
                        {
                            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                            {
                                if (m.DeclaringType != t) continue;
                                var name = m.Name;
                                if (name.StartsWith("get_") || name.StartsWith("set_")) continue;
                                if (name.IndexOf("Accept", StringComparison.Ordinal) >= 0 ||
                                    name.IndexOf("Apply", StringComparison.Ordinal) >= 0 ||
                                    name.IndexOf("UpdateWorkspace", StringComparison.Ordinal) >= 0 ||
                                    name.IndexOf("Merge", StringComparison.Ordinal) >= 0 ||
                                    name.IndexOf("Resolve", StringComparison.Ordinal) >= 0 ||
                                    name.IndexOf("Download", StringComparison.Ordinal) >= 0 ||
                                    name.IndexOf("Incoming", StringComparison.Ordinal) >= 0 ||
                                    name.IndexOf("Checkin", StringComparison.Ordinal) >= 0 ||
                                    name.IndexOf("Submit", StringComparison.Ordinal) >= 0)
                                {
                                    n++;
                                    var pars = m.GetParameters();
                                    var sig = string.Join(",", pars.Select(p => p.ParameterType.Name));
                                    sb.AppendLine(t.Name + "." + name + "(" + sig + ") -> " + m.ReturnType.Name);
                                }
                            }
                        }
                    }
                    sb.AppendLine("count: " + n);
                }
            }
            catch (Exception e) { sb.AppendLine("EX: " + e); }
            File.WriteAllText("Temp/plastic_list.txt", sb.ToString(),
                new UTF8Encoding(false));
        }
    }
}