using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Mavis
{
    public static class PlasticMenuProbe
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

                if (plasticAsm == null)
                {
                    sb.AppendLine("NO_ASM");
                }
                else
                {
                    int count = 0;
                    foreach (var t in plasticAsm.GetTypes())
                    {
                        var methods = t.GetMethods(
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Static | BindingFlags.Instance);
                        foreach (var m in methods)
                        {
                            if (m.DeclaringType != t) continue;
                            var n = m.Name;
                            if (n.IndexOf("Accept", StringComparison.Ordinal) >= 0 ||
                                n.IndexOf("Apply", StringComparison.Ordinal) >= 0 ||
                                n.IndexOf("Download", StringComparison.Ordinal) >= 0 ||
                                n.IndexOf("Update", StringComparison.Ordinal) >= 0 ||
                                n.IndexOf("Merge", StringComparison.Ordinal) >= 0 ||
                                n.IndexOf("Resolve", StringComparison.Ordinal) >= 0 ||
                                n.IndexOf("Refresh", StringComparison.Ordinal) >= 0)
                            {
                                count++;
                                var pars = m.GetParameters();
                                var sig = string.Join(", ",
                                    pars.Select(p => p.ParameterType.Name + " " + p.Name));
                                sb.AppendLine(t.FullName + "." + n + "(" + sig + ")");
                            }
                        }
                    }
                    sb.AppendLine("count: " + count);
                }
            }
            catch (Exception e) { sb.AppendLine("EX: " + e); }
            File.WriteAllText("Temp/plastic_methods.txt", sb.ToString(),
                new UTF8Encoding(false));
            Debug.Log("[Mavis] wrote Temp/plastic_methods.txt");
        }
    }
}