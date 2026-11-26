using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
public static class AuditSharkBitePoseV068
{
    public static object Inspect()
    {
        // Isolated, synchronous preview only: never touch the live actor or
        // interrupt a user's Play session to read the imported bone poses.
        var scene=EditorSceneManager.NewPreviewScene();GameObject actor=null;PlayableGraph graph=default;
        try
        {
            var set=AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>("Assets/Resources/SharkAnimation/TralaleroAnimations.asset");
            actor=UnityEngine.Object.Instantiate(set.rigPrefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
            var animator=actor.GetComponentInChildren<Animator>();animator.applyRootMotion=false;animator.Rebind();animator.Update(0);
            var bones=actor.GetComponentsInChildren<Transform>(true);
            Transform B(string name)=>bones.Single(b=>b.name==name);
            var rows=new System.Collections.Generic.List<object>();
            foreach(float t in new[]{0f,.2f,.35f,.52f,.65f,.9f,1.29f})
            {
                animator.Play("Bite_Lunge",0,t/1.3f);animator.Update(0);
                rows.Add(new{t,body=B("Body").position.ToString("F6"),head=B("Head").position.ToString("F6"),nose=B("Nose_Marker").position.ToString("F6"),noseMinusBody=(B("Nose_Marker").position-B("Body").position).ToString("F6"),headRotation=B("Head").localRotation.ToString("F6"),rig=animator.transform.localEulerAngles.ToString(),scale=animator.transform.lossyScale.ToString("F6")});
            }
            var result=new{native=true,source=AssetDatabase.GetAssetPath(set.rigPrefab),rows};
            File.WriteAllText(".codex/opening-optimization-20261012/shark-bite-native-pose-v068.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{if(graph.IsValid())graph.Destroy();if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
