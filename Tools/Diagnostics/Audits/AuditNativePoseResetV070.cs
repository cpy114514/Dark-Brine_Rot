using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class AuditNativePoseResetV070
{
    public static object Check()
    {
        var scene=EditorSceneManager.NewPreviewScene();GameObject actor=null;
        try
        {
            var set=AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>("Assets/Resources/SharkAnimation/TralaleroAnimations.asset");
            actor=UnityEngine.Object.Instantiate(set.rigPrefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
            var animator=actor.GetComponentInChildren<Animator>();animator.applyRootMotion=false;animator.speed=0;animator.Rebind();animator.Update(0);
            var bones=actor.GetComponentsInChildren<Transform>().Where(b=>b.name=="Tail_Tip" || b.name=="Head" || b.name=="Spine_Rear").ToArray();
            animator.Play("Tail_Strike",0,.65f/1.5f);animator.Update(0);
            var positions=bones.Select(b=>b.localPosition).ToArray();var rotations=bones.Select(b=>b.localRotation).ToArray();
            foreach(var bone in bones){bone.localPosition+=Vector3.up*.1f;bone.localRotation*=Quaternion.Euler(0,30,0);}
            animator.Play("Tail_Strike",0,.65f/1.5f);animator.Update(0);
            return bones.Select((bone,i)=>new{name=bone.name,positionError=Vector3.Distance(positions[i],bone.localPosition),rotationError=Quaternion.Angle(rotations[i],bone.localRotation)}).ToArray();
        }
        finally{if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
