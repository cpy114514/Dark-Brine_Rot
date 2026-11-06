using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Mavis;
public static class BuildEnemyDeathSupport069
{
    public static object Build()
    {
        if(Application.isPlaying)throw new Exception("Stopped Editor required");
        const string folder="Assets/Resources/EnemyDeath";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var directions=new List<Vector3>();var keys=new HashSet<Vector3Int>();
        void Direction(Vector3 v){if(v.sqrMagnitude<.1f)return;v.Normalize();if(keys.Add(Vector3Int.RoundToInt(v*1000)))directions.Add(v);}
        for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)Direction(new Vector3(x,y,z));
        for(int a=0;a<16;a++){float t=a*Mathf.PI/8;Direction(new Vector3(Mathf.Cos(t),Mathf.Sin(t),0));Direction(new Vector3(Mathf.Cos(t),0,Mathf.Sin(t)));Direction(new Vector3(0,Mathf.Cos(t),Mathf.Sin(t)));}
        var entries=new Dictionary<string,EnemyDeathSupportLibrary.Entry>();
        var paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Game/Prefabs/Enemies"}).Select(AssetDatabase.GUIDToAssetPath).Concat(new[]{"Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab","Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab","Assets/Game/Prefabs/Bosses/PigRonin/Pig Ronin Boss.prefab","Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab"}).Distinct().ToArray();
        void Add(Mesh mesh,int boneCount)
        {
            if(!mesh)return;string key=EnemyDeathSupportLibrary.Key(mesh,boneCount);if(entries.ContainsKey(key))return;
            var vertices=mesh.vertices;var weights=boneCount>0?mesh.boneWeights:null;var bindposes=boneCount>0?mesh.bindposes:new Matrix4x4[0];
            int groups=Mathf.Max(1,boneCount),d=directions.Count;
            var values=Enumerable.Repeat(float.NegativeInfinity,groups*d).ToArray();var indices=Enumerable.Repeat(-1,groups*d).ToArray();
            for(int i=0;i<vertices.Length;i++)
            {
                int group=0;Vector3 p=vertices[i];
                if(boneCount>0)
                {
                    var w=weights[i];group=w.boneIndex0;float largest=w.weight0;
                    if(w.weight1>largest){group=w.boneIndex1;largest=w.weight1;}if(w.weight2>largest){group=w.boneIndex2;largest=w.weight2;}if(w.weight3>largest)group=w.boneIndex3;
                    p=bindposes[group].MultiplyPoint3x4(p);
                }
                for(int j=0;j<d;j++){float value=Vector3.Dot(p,directions[j]);int n=group*d+j;if(value>values[n]){values[n]=value;indices[n]=i;}}
            }
            var selected=new HashSet<int>(indices.Where(i=>i>=0));for(int i=0;i<128;i++)selected.Add(i*(vertices.Length-1)/127);
            var points=selected.OrderBy(i=>i).Select(i=>new EnemyDeathSupportLibrary.Point{vertex=vertices[i],weight=boneCount>0?weights[i]:default}).ToArray();
            entries[key]=new EnemyDeathSupportLibrary.Entry{key=key,bindposes=bindposes,points=points};
        }
        foreach(var path in paths)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var skin in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(skin.enabled&&!skin.name.ToLowerInvariant().Contains("katana"))Add(skin.sharedMesh,skin.bones.Length);
            foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true))if(filter.GetComponent<MeshRenderer>())Add(filter.sharedMesh,0);
        }
        string asset=folder+"/SupportLibrary.asset";var library=AssetDatabase.LoadAssetAtPath<EnemyDeathSupportLibrary>(asset);
        if(!library){library=ScriptableObject.CreateInstance<EnemyDeathSupportLibrary>();AssetDatabase.CreateAsset(library,asset);}
        library.entries=entries.Values.ToArray();EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
        var result=new{entries=entries.Count,samples=entries.Values.Sum(e=>e.points.Length),profiles=entries.Values.Select(e=>new{e.key,points=e.points.Length,bones=e.bindposes.Length}).ToArray()};
        File.WriteAllText(".codex/enemy-death069/support-library.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
