using System;
using System.IO;
using UnityEngine;
public static class PrototypeFootSkinV123
{
    public static object Check()
    {
        var hero=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>().sahur;
        var animator=hero.GetComponentInChildren<Animator>();var skin=hero.GetComponentInChildren<SkinnedMeshRenderer>();
        var bones=skin.bones;var mesh=skin.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes;
        int left=Array.IndexOf(bones,animator.GetBoneTransform(HumanBodyBones.LeftFoot)),right=Array.IndexOf(bones,animator.GetBoneTransform(HumanBodyBones.RightFoot));
        int leftToe=Array.IndexOf(bones,animator.GetBoneTransform(HumanBodyBones.LeftToes)),rightToe=Array.IndexOf(bones,animator.GetBoneTransform(HumanBodyBones.RightToes));
        bool Foot(int b)=>b==left || b==right || b==leftToe || b==rightToe;
        var matrices=new Matrix4x4[bones.Length];for(int i=0;i<bones.Length;i++)matrices[i]=skin.transform.worldToLocalMatrix*bones[i].localToWorldMatrix*bind[i];
        Mesh baked=new Mesh();int samples=0;float rawError=0,lossyError=0,localError=0;
        try
        {
            skin.BakeMesh(baked,true);var reference=baked.vertices;
            for(int i=0;i<vertices.Length;i++)
            {
                var w=weights[i];float total=(Foot(w.boneIndex0)?w.weight0:0)+(Foot(w.boneIndex1)?w.weight1:0)+(Foot(w.boneIndex2)?w.weight2:0)+(Foot(w.boneIndex3)?w.weight3:0);
                if(total<.25f)continue;
                Vector3 p=matrices[w.boneIndex0].MultiplyPoint3x4(vertices[i])*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(vertices[i])*w.weight1+
                    matrices[w.boneIndex2].MultiplyPoint3x4(vertices[i])*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(vertices[i])*w.weight3;
                var expected=skin.transform.TransformPoint(reference[i]);
                rawError=Mathf.Max(rawError,Vector3.Distance(expected,skin.transform.TransformPoint(p)));
                lossyError=Mathf.Max(lossyError,Vector3.Distance(expected,skin.transform.TransformPoint(Vector3.Scale(p,skin.transform.lossyScale))));
                localError=Mathf.Max(localError,Vector3.Distance(expected,skin.transform.TransformPoint(Vector3.Scale(p,skin.transform.localScale))));samples++;
            }
            var result=new{actualUserSkin=true,vertices=vertices.Length,footVertices=samples,bones=bones.Length,scale=skin.transform.lossyScale.ToString(),rawError,lossyError,localError,releaseCodeChanged=false};
            File.WriteAllText(".codex/opening-optimization-20261012/foot-skin-prototype-v123.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{UnityEngine.Object.DestroyImmediate(baked);}
    }
}
