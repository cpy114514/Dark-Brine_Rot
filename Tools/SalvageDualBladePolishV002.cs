using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine;
public static class SalvageDualBladePolishV002
{
    public static object Run()
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("TestDualBladePolishV002")).Last(t=>t!=null);
        var flags=BindingFlags.NonPublic|BindingFlags.Static;
        var frames=(IList)type.GetField("frames",flags).GetValue(null);var rows=new List<object>();
        foreach(var frame in frames)
        {
            var fields=new Dictionary<string,object>();
            foreach(var property in frame.GetType().GetProperties())
            {
                var value=property.GetValue(frame);
                fields[property.Name]=value is Vector3 p?new[]{p.x,p.y,p.z}:value is Quaternion q?new[]{q.x,q.y,q.z,q.w}:value;
            }
            rows.Add(fields);
        }
        var data=new{passed=false,reason="Review-phase recoil sampling occurred before LateUpdate; raw captures retained",fps=60,checks=type.GetField("checks",flags).GetValue(null),frames=rows};
        File.WriteAllText(".codex/dual-blade-polish-20261019/verification-60hz/tests-partial.json",Newtonsoft.Json.JsonConvert.SerializeObject(data));
        return new{salvaged=rows.Count};
    }
}
