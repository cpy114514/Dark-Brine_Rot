using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class RepairShipHullMaterials
{
    const string Folder="Assets/Game/Prefabs/Environment/Props/Ship/Materials/";
    public static object Run()
    {
        var changed=new List<string>();
        for(int i=0;i<14;i++)
        {
            string path=Folder+"acmat_"+i+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null || !mat.HasProperty("_Cull")) throw new Exception("Expected ship material: "+path);
            var texture=mat.GetTexture("_BaseMap");
            Undo.RecordObject(mat,"Render both sides of ship planks and sails");
            mat.SetFloat("_Cull",(float)UnityEngine.Rendering.CullMode.Off);
            mat.doubleSidedGI=true;
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssetIfDirty(mat);
            if(mat.GetFloat("_Cull")!=0 || mat.GetTexture("_BaseMap")!=texture || mat.GetFloat("_Surface")!=0)
                throw new Exception("Material repair validation failed: "+path);
            foreach(var error in ShaderUtil.GetShaderMessages(mat.shader))
                if(error.severity.ToString()=="Error") throw new Exception(error.message);
            changed.Add(path);
        }
        return new { materials=changed, twoSided=true, opaque=true, originalTexturesPreserved=true };
    }
}
