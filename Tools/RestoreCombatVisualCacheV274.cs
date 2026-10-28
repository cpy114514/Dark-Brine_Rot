using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Mavis;

// Reconnect surviving native pool objects after a Play-mode domain reload.
// No source changes, new pools, animation changes, input changes or time changes.
public static class RestoreCombatVisualCacheV274
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    public static object Run()
    {
        var repaired=new List<object>();
        foreach(var owner in UnityEngine.Object.FindObjectsByType<CombatImpactVisuals>(FindObjectsSortMode.None))
        {
            var type=owner.GetType();var pulses=(Array)type.GetField("pulses",F).GetValue(owner);
            var ribbons=(Array)type.GetField("ribbons",F).GetValue(owner);
            if(ribbons.GetValue(0)!=null&&pulses.GetValue(0)!=null)continue;
            var rings=Enumerable.Range(0,pulses.Length).Select(i=>owner.transform.Find("Pooled pressure ring "+i)).ToArray();
            var trails=new[]{owner.transform.Find("Weapon pressure ribbon"),owner.transform.Find("Offhand pressure ribbon")};
            if(rings.Any(t=>!t||!t.GetComponent<MeshRenderer>()||!t.GetComponent<MeshFilter>())||
                trails.Any(t=>!t||!t.GetComponent<MeshRenderer>()||!t.GetComponent<MeshFilter>()))
                throw new Exception("Incomplete surviving visual pool: "+owner.name);
            for(int i=0;i<pulses.Length;i++)
            {
                var pulse=Activator.CreateInstance(type.GetNestedType("Pulse",BindingFlags.NonPublic),true);
                var renderer=rings[i].GetComponent<MeshRenderer>();renderer.enabled=false;
                Set(pulse,"transform",rings[i]);Set(pulse,"renderer",renderer);pulses.SetValue(pulse,i);
            }
            for(int i=0;i<ribbons.Length;i++)
            {
                var ribbon=Activator.CreateInstance(type.GetNestedType("Ribbon",BindingFlags.NonPublic),true);
                Set(ribbon,"mesh",trails[i].GetComponent<MeshFilter>().sharedMesh);Set(ribbon,"renderer",trails[i].GetComponent<MeshRenderer>());
                var indices=(int[])ribbon.GetType().GetField("indices",F).GetValue(ribbon);
                for(int j=0;j<15;j++){int k=j*6,v=j*2;indices[k]=v;indices[k+1]=v+2;indices[k+2]=v+1;indices[k+3]=v+1;indices[k+4]=v+2;indices[k+5]=v+3;}
                ribbons.SetValue(ribbon,i);
            }
            Set(owner,"ringMaterial",rings[0].GetComponent<MeshRenderer>().sharedMaterial);
            Set(owner,"trailMaterial",trails[0].GetComponent<MeshRenderer>().sharedMaterial);
            Set(owner,"ringMesh",rings[0].GetComponent<MeshFilter>().sharedMesh);
            owner.ClearTrail();type.GetMethod("RefreshFeatures",F).Invoke(owner,null);
            repaired.Add(new{owner=owner.name,pulses=pulses.Length,ribbons=ribbons.Length});
        }
        return new{repaired};
    }
}
