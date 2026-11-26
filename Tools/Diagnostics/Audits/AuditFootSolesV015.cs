using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
public static class AuditFootSolesV015
{
    public static async Task<object> Inspect(bool fromWater=false)
    {
        var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=sequence.GetComponent<Story1WreckBattle>();var hero=sequence.sahur;
        if(fromWater)
        {
            var water=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();Vector3 point=hero.transform.position+Vector3.right*60;
            point.y=water.SampleSurfaceHeight(point,Time.time)-2-hero.LowestFootWorldOffset;
            hero.GetComponent<Story1WreckRider>().Release();hero.RestoreSavedPose(point,hero.transform.rotation);await Task.Delay(400);
        }
        typeof(Story1WreckBattle).GetMethod("BeginFinale",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(battle,null);
        while(battle.Finale.CinematicTime<.1f)await Task.Delay(30);
        var film=battle.Finale;var animator=hero.CharacterAnimator;var rows=new System.Collections.Generic.List<object>();
        foreach(bool left in new[]{true,false})
        {
            var foot=animator.GetBoneTransform(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
            var toe=animator.GetBoneTransform(left ? HumanBodyBones.LeftToes : HumanBodyBones.RightToes);
            var sole=(Vector3)typeof(Story1WreckFinale).GetField(left ? "leftSole" : "rightSole",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(film);
            float minimum=float.PositiveInfinity;int count=0;
            foreach(var skin in hero.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                int index=Array.IndexOf(skin.bones,foot),toeIndex=Array.IndexOf(skin.bones,toe);if(index<0)continue;
                var mesh=new Mesh();skin.BakeMesh(mesh,true);var vertices=mesh.vertices;var weights=skin.sharedMesh.boneWeights;
                for(int i=0;i<vertices.Length;i++)
                {
                    var w=weights[i];float influence=0;
                    bool Match(int bone)=>bone==index || toeIndex>=0 && bone==toeIndex;
                    if(Match(w.boneIndex0))influence+=w.weight0;if(Match(w.boneIndex1))influence+=w.weight1;
                    if(Match(w.boneIndex2))influence+=w.weight2;if(Match(w.boneIndex3))influence+=w.weight3;
                    if(influence<.25f)continue;
                    minimum=Mathf.Min(minimum,skin.transform.TransformPoint(vertices[i]).y);count++;
                }
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            rows.Add(new{left,count,bone=foot.position.ToString("F4"),marker=foot.TransformPoint(sole).ToString("F4"),actualMeshMinimumY=minimum,markerAboveMesh=foot.TransformPoint(sole).y-minimum,localSole=sole.ToString("F4")});
        }
        var result=new{fromWater,film.LeadInSeconds,rows};System.IO.File.WriteAllText(".codex/encounter-v015/sole-audit-"+fromWater+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
