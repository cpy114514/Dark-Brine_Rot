using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
public static class VerifyTailImportV186
{
    public static object Check()
    {
        var scene=EditorSceneManager.NewPreviewScene();GameObject actor=null;PlayableGraph graph=default;
        try
        {
            var set=AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>("Assets/Resources/SharkAnimation/TralaleroAnimations.asset");
            actor=UnityEngine.Object.Instantiate(set.rigPrefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
            var animator=actor.GetComponentInChildren<Animator>();animator.applyRootMotion=false;animator.Rebind();animator.Update(0);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Encounter/v006/EncounterShark.controller");
            var bones=actor.GetComponentsInChildren<Transform>();var marker=bones.Single(t=>t.name=="Tail_Marker");
            var revisedBones=new HashSet<string>{"Spine_Rear","Tail_Base","Tail_Mid"};
            graph=PlayableGraph.Create("Original native tail import comparison");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var mixer=AnimationMixerPlayable.Create(graph,2);AnimationPlayableOutput.Create(graph,"native",animator).SetSourcePlayable(mixer);graph.Play();
            var rows=new List<object>();bool pass=true;
            foreach(string suffix in new[]{"Tail"})
            {
                string state=suffix=="Tail"?"Tail_Strike":"Ship_Smash";
                var source=(AnimationClip)controller.layers[0].stateMachine.states.Single(s=>s.state.name==state).state.motion;
                string asset="Assets/AnimationStaging/CH1_Shark"+suffix+"_v186/CH1_Shark"+suffix+"_v186.fbx";
                var revised=AssetDatabase.LoadAllAssetsAtPath(asset).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
                var a=AnimationClipPlayable.Create(graph,source);var b=AnimationClipPlayable.Create(graph,revised);a.SetSpeed(0);b.SetSpeed(0);mixer.ConnectInput(0,a,0);mixer.ConnectInput(1,b,0);
                float maximumUnchangedRotation=0,maximumPosition=0,maximumChangedRotation=0,contactRotation=0,contactMarker=0,peakStep=0;
                Vector3 contactDelta=Vector3.zero; Quaternion[] previous=null;
                for(int frame=0;frame<=90;frame++)
                {
                    float t=Mathf.Min(1.499999f,frame/60f);a.SetTime(t);b.SetTime(t);
                    mixer.SetInputWeight(0,1);mixer.SetInputWeight(1,0);graph.Evaluate(0);
                    var rotation=bones.Select(n=>n.localRotation).ToArray();var position=bones.Select(n=>n.localPosition).ToArray();Vector3 oldMarker=marker.position;
                    mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,1);graph.Evaluate(0);
                    for(int i=0;i<bones.Length;i++)
                    {
                        float angle=Quaternion.Angle(rotation[i],bones[i].localRotation);
                        if(revisedBones.Contains(bones[i].name))maximumChangedRotation=Mathf.Max(maximumChangedRotation,angle);
                        else maximumUnchangedRotation=Mathf.Max(maximumUnchangedRotation,angle);
                        maximumPosition=Mathf.Max(maximumPosition,Vector3.Distance(position[i],bones[i].localPosition));
                        if(frame==39)contactRotation=Mathf.Max(contactRotation,angle);
                        if(previous!=null)peakStep=Mathf.Max(peakStep,Quaternion.Angle(previous[i],bones[i].localRotation));
                    }
                    if(frame==39){contactMarker=Vector3.Distance(oldMarker,marker.position);contactDelta=marker.position-oldMarker;}
                    previous=bones.Select(n=>n.localRotation).ToArray();
                }
                bool valid=revised.frameRate==60 && Mathf.Abs(revised.length-1.5f)<.001f && !revised.isLooping && maximumUnchangedRotation<.06f && maximumPosition<.00001f && maximumChangedRotation>.5f && contactRotation>2 && contactRotation<4.2f && contactMarker>.0001f && contactMarker<2 && contactDelta.y>0 && peakStep<10.1f;
                rows.Add(new{state,source=source.name,revised=revised.name,revised.frameRate,revised.length,curves=AnimationUtility.GetCurveBindings(revised).Length,
                    maximumUnchangedRotation,maximumPosition,maximumChangedRotation,contactRotation,contactMarker,contactDelta=new[]{contactDelta.x,contactDelta.y,contactDelta.z},peakStep,valid});pass&=valid;
                mixer.DisconnectInput(0);mixer.DisconnectInput(1);graph.DestroyPlayable(a);graph.DestroyPlayable(b);
            }
            var result=new{native=true,pass,originalRig=true,sourceFps=60,contactSeconds=.65f,rows};
            File.WriteAllText(".codex/animation-continuation-20261014/tail-import-v186.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            if(!pass)throw new Exception("Native original-rig tail comparison failed");return result;
        }
        finally{if(graph.IsValid())graph.Destroy();if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
