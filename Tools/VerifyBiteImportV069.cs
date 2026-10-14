using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
public static class VerifyBiteImportV069
{
    public static object Check()
    {
        var scene=EditorSceneManager.NewPreviewScene();GameObject actor=null;PlayableGraph graph=default;
        try
        {
            var set=AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>("Assets/Resources/SharkAnimation/TralaleroAnimations.asset");
            actor=UnityEngine.Object.Instantiate(set.rigPrefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
            var animator=actor.GetComponentInChildren<Animator>();animator.applyRootMotion=false;animator.Rebind();animator.Update(0);
            var bones=actor.GetComponentsInChildren<Transform>().Where(t=>t.name!="Head").ToArray();
            var nose=actor.GetComponentsInChildren<Transform>().Single(t=>t.name=="Nose_Marker");
            var source=AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Encounter/v057/CH1_SharkBite_v057/CH1_SharkBite_v057.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
            var revised=AssetDatabase.LoadAllAssetsAtPath("Assets/AnimationStaging/CH1_SharkBite_v069/CH1_SharkBite_v069.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
            graph=PlayableGraph.Create("Original native bite import comparison");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var mixer=AnimationMixerPlayable.Create(graph,2);AnimationPlayableOutput.Create(graph,"native",animator).SetSourcePlayable(mixer);
            var a=AnimationClipPlayable.Create(graph,source);var b=AnimationClipPlayable.Create(graph,revised);a.SetSpeed(0);b.SetSpeed(0);mixer.ConnectInput(0,a,0);mixer.ConnectInput(1,b,0);graph.Play();
            float maxRotation=0,maxPosition=0,rise=0;var rows=new System.Collections.Generic.List<object>();
            foreach(float t in new[]{0f,.2f,.35f,.52f,.65f,.9f,1.29f})
            {
                a.SetTime(t);b.SetTime(t);mixer.SetInputWeight(0,1);mixer.SetInputWeight(1,0);graph.Evaluate(0);
                var rotation=bones.Select(n=>n.localRotation).ToArray();var position=bones.Select(n=>n.localPosition).ToArray();Vector3 oldNose=nose.position;
                mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,1);graph.Evaluate(0);
                float r=0,p=0;for(int i=0;i<bones.Length;i++){r=Mathf.Max(r,Quaternion.Angle(rotation[i],bones[i].localRotation));p=Mathf.Max(p,Vector3.Distance(position[i],bones[i].localPosition));}
                maxRotation=Mathf.Max(maxRotation,r);maxPosition=Mathf.Max(maxPosition,p);if(Mathf.Abs(t-.52f)<.001f)rise=nose.position.y-oldNose.y;
                rows.Add(new{t,unchangedLocalDegrees=r,unchangedLocalMetres=p,noseDelta=(nose.position-oldNose).ToString("F8")});
            }
            bool pass=revised.frameRate==60 && Mathf.Abs(revised.length-1.3f)<.001f && maxRotation<.05f && maxPosition<.00001f && rise>.007f;
            var result=new{native=true,pass,originalRig=true,frameRate=revised.frameRate,length=revised.length,maxRotation,maxPosition,contactRiseInOriginalPrefabMetres=rise,rows};
            File.WriteAllText(".codex/opening-optimization-20261012/bite-import-v069.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            if(!pass)throw new Exception("Native preserved-rig bite validation failed");return result;
        }
        finally{if(graph.IsValid())graph.Destroy();if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
