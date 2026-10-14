using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class InspectSnapshotV039
{
    public static object Inspect()
    {
        return new { shaders=new[]{"Assets/Ocean/ProceduralOcean.shader","Assets/Shaders/ShipWindCloth.shader"}.Select(p=>{var shader=AssetDatabase.LoadAssetAtPath<Shader>(p);return new{path=p,supported=shader!=null && shader.isSupported,messages=shader==null ? new string[]{"Shader missing"} : ShaderUtil.GetShaderMessages(shader).Select(m=>m.severity+": "+m.message).ToArray()};}).ToArray()};
    }
}
