using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class ReviewSecondIslandTimelines
{
    public static object Run()
    {
        var reports=new List<object>();
        foreach(string key in new[]{"BoarWalk","BoarCharge","BoarTail","BajieRake","BajieDodge","HuangSweep","HuangSlam","HuangPalm","HuangMandala"})
        {
            bool huang=key.StartsWith("Huang"),boar=key.StartsWith("Boar");
            var preview=new PreviewRenderUtility();
            try
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(huang?"Assets/Resources/SecondIslandHuangMei/Huang Mei Boss.prefab":"Assets/Resources/SecondIslandZhuBaJie/ZhuBaJie Boss.prefab");
                var actor=UnityEngine.Object.Instantiate(source);preview.AddSingleGO(actor);
                var boss=actor.GetComponent<ZhuBaJieBoss>();
                GameObject model=huang?actor.transform.Find("Huang Mei Weighted Model").gameObject:boar?boss.boarForm.GetComponentInChildren<Animator>(true).gameObject:boss.humanForm.transform.Find("Human Model").gameObject;
                if(!huang){boss.boarForm.SetActive(boar);boss.humanForm.SetActive(!boar);}
                foreach(var animator in actor.GetComponentsInChildren<Animator>(true))animator.enabled=false;
                var clip=Resources.Load<AnimationClip>("SecondIslandMotion/"+key);
                var bones=model.GetComponentsInChildren<Transform>();var root=bones.First(t=>t.name=="root");var before=root.localPosition;
                var skin=model.GetComponentsInChildren<Renderer>();var bounds=skin[0].bounds;foreach(var r in skin)bounds.Encapsulate(r.bounds);
                foreach(var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>()){skinned.updateWhenOffscreen=true;skinned.forceMatrixRecalculationPerRender=true;}
                var center=bounds.center;float distance=Mathf.Max(bounds.size.y,bounds.size.x)*2.0f;
                preview.camera.transform.position=center+(actor.transform.forward+actor.transform.right*.7f+Vector3.up*.22f).normalized*distance;
                preview.camera.transform.LookAt(center);preview.camera.fieldOfView=38;preview.camera.nearClipPlane=.05f;preview.camera.farClipPlane=200;
                preview.camera.clearFlags=CameraClearFlags.Color;preview.camera.backgroundColor=new Color(.06f,.07f,.08f);
                preview.ambientColor=new Color(.45f,.45f,.45f);preview.lights[0].intensity=1.5f;preview.lights[0].transform.rotation=Quaternion.Euler(35,145,0);
                preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=Quaternion.Euler(20,-35,0);
                const int size=360;var sheet=new Texture2D(size*4,size*2,TextureFormat.RGB24,false);float drift=0;
                for(int frame=0;frame<8;frame++)
                {
                    clip.SampleAnimation(model,clip.length*frame/7);
                    foreach(var bone in bones)if(!float.IsFinite(bone.localPosition.x)||!float.IsFinite(bone.localRotation.w))throw new Exception("Invalid pose "+key);
                    drift=Mathf.Max(drift,Vector3.Distance(root.localPosition,before));
                    preview.BeginStaticPreview(new Rect(0,0,size,size));preview.Render(true);var rendered=preview.EndStaticPreview();
                    sheet.SetPixels(frame%4*size,(1-frame/4)*size,size,size,rendered.GetPixels());UnityEngine.Object.DestroyImmediate(rendered);
                }
                sheet.Apply();string path=".codex/island2-upgrade/review/"+key+"-timeline.png";
                File.WriteAllBytes(path,sheet.EncodeToPNG());UnityEngine.Object.DestroyImmediate(sheet);
                reports.Add(new{key,duration=clip.length,frames=8,skeletonRootDrift=drift,path});
            }
            finally{preview.Cleanup();}
        }
        File.WriteAllText(".codex/island2-upgrade/review/timelines.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));return reports;
    }
}
