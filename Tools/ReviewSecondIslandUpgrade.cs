using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class ReviewSecondIslandUpgrade
{
    public static object Poses()
    {
        string folder=".codex/island2-upgrade/review";Directory.CreateDirectory(folder);
        var paths=new System.Collections.Generic.List<string>();
        foreach(bool huang in new[]{true,false})
        {
            string prefix=huang?"Huang":"Bajie";
            foreach(var sample in huang?new[]{("Idle",.2f),("Sweep",1.31f),("Slam",1.68f),("Mandala",2.69f)}:new[]{("Idle",.2f),("Rake",1.05f),("Rake",1.8f)})
            {
                var preview=new PreviewRenderUtility();
                try
                {
                    var source=AssetDatabase.LoadAssetAtPath<GameObject>(huang?"Assets/Resources/SecondIslandHuangMei/Huang Mei Boss.prefab":"Assets/Resources/SecondIslandZhuBaJie/ZhuBaJie Boss.prefab");
                    var actor=UnityEngine.Object.Instantiate(source);preview.AddSingleGO(actor);
                    var model=huang?actor.transform.Find("Huang Mei Weighted Model").gameObject:actor.GetComponent<ZhuBaJieBoss>().humanForm.transform.Find("Human Model").gameObject;
                    if(!huang){actor.GetComponent<ZhuBaJieBoss>().boarForm.SetActive(false);model.transform.parent.gameObject.SetActive(true);}
                    foreach(var a in actor.GetComponentsInChildren<Animator>(true))a.enabled=false;
                    foreach(var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>()){skinned.updateWhenOffscreen=true;skinned.forceMatrixRecalculationPerRender=true;}
                    var clip=Resources.Load<AnimationClip>("SecondIslandMotion/"+prefix+sample.Item1);if(!clip)throw new Exception("Missing motion "+prefix+sample.Item1);
                    clip.SampleAnimation(model,sample.Item2);
                    var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
                    foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    float height=Mathf.Max(8,bounds.size.y),distance=height*1.65f;
                    preview.camera.transform.position=bounds.center+new Vector3(.65f,.2f,-1).normalized*distance;
                    preview.camera.transform.LookAt(bounds.center);preview.camera.nearClipPlane=.05f;preview.camera.farClipPlane=200;
                    preview.camera.fieldOfView=38;preview.camera.clearFlags=CameraClearFlags.Color;preview.camera.backgroundColor=new Color(.035f,.04f,.05f);
                    preview.ambientColor=new Color(.35f,.35f,.35f);
                    preview.lights[0].intensity=1.4f;preview.lights[0].transform.rotation=Quaternion.Euler(35,-35,0);
                    preview.lights[1].intensity=.7f;preview.lights[1].transform.rotation=Quaternion.Euler(20,140,0);
                    preview.BeginStaticPreview(new Rect(0,0,700,700));preview.Render(true);
                    var image=preview.EndStaticPreview();string path=folder+"/"+prefix+sample.Item1+sample.Item2.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png";
                    File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);paths.Add(path);
                }
                finally{preview.Cleanup();}
            }
        }
        return paths;
    }
}
