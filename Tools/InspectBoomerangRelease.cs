using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Mavis;

public static class InspectBoomerangRelease
{
    public static object Run(){
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),new Vector3(0,500,0),Quaternion.Euler(0,90,0));
        float time=Time.timeScale;
        try{
            Time.timeScale=0;var attack=root.GetComponent<SahurAttack>();var boom=root.GetComponent<SahurBoomerang>();
            var before=root.transform.forward;if(!boom.TryThrow())throw new Exception("Throw refused");
            var animator=attack.animator;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var stick=root.GetComponent<SahurSwimmingWeapon>().stick;var mesh=stick.GetComponent<MeshFilter>();
            var samples=new List<object>();
            for(float phase=.20f;phase<.341f;phase+=.01f){
                animator.Play("Boomerang Throw",0,phase);animator.Update(0);
                samples.Add(new{phase,hand=root.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightHand).position).ToString("F3"),center=root.transform.InverseTransformPoint(stick.TransformPoint(mesh.sharedMesh.bounds.center)).ToString("F3")});
            }
            return new{before=before.ToString(),after=root.transform.forward.ToString(),camera=Camera.main?.transform.forward.ToString(),samples};
        }finally{Time.timeScale=time;UnityEngine.Object.DestroyImmediate(root);}
    }
}
