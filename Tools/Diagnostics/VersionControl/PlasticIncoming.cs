using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Mavis
{
    public static class PlasticIncoming
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
                    // 找 IncomingChangesView 及其基类的 public/non-public 方法
                    var types = plasticAsm.GetTypes()
                        .Where(t => t.Name.Contains("IncomingChangesView") ||
                                    t.Name.Contains("IncomingChange") ||
                                    t.Name.Contains("IIncomingChangesTab"))
                        .ToList();
                    foreach (var t in types)
                    {
                        sb.AppendLine("=== " + t.FullName + " ===");
                        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                        {
                            if (m.DeclaringType != t) continue;
                            var n = m.Name;
                            if (n.StartsWith("get_") || n.StartsWith("set_")) continue;
                            if (n.Length < 3) continue;
                            var pars = m.GetParameters();
                            var sig = string.Join(",", pars.Select(p => p.ParameterType.Name));
                            sb.AppendLine("  " + n + "(" + sig + ") -> " + m.ReturnType.Name);
                        }
                    }

                    // PlasticAPI 的所有静态方法
                    var api = plasticAsm.GetType("Unity.PlasticSCM.Editor.PlasticAPI");
                    if (api != null)
                    {
                        sb.AppendLine("=== PlasticAPI ===");
                        foreach (var m in api.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                        {
                            var pars = m.GetParameters();
                            var sig = string.Join(",", pars.Select(p => p.ParameterType.Name));
                            sb.AppendLine("  " + m.Name + "(" + sig + ") -> " + m.ReturnType.Name);
                        }
                    }

                    // PlasticClient / IPlasticAPI 接口
                    foreach (var t in plasticAsm.GetTypes())
                    {
                        if (t.Name == "IPlasticAPI" || t.Name == "PlasticClient")
                        {
                            sb.AppendLine("=== " + t.FullName + " ===");
                            foreach (var m in t.GetMethods())
                            {
                                if (m.DeclaringType != t) continue;
                                var pars = m.GetParameters();
                                var sig = string.Join(",", pars.Select(p => p.ParameterType.Name));
                                sb.AppendLine("  " + m.Name + "(" + sig + ") -> " + m.ReturnType.Name);
                            }
                        }
                    }
                }
            }
            catch (Exception e) { sb.AppendLine("EX: " + e); }
            File.WriteAllText("Temp/plastic_incoming.txt", sb.ToString(),
                new UTF8Encoding(false));
        }
    }
}