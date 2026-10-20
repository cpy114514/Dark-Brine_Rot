using System;
using System.IO;
using System.Reflection;
using UnityEngine;
public static class PreviewTutorialCameraV222
{
    public static object Start(){new GameObject("Tutorial camera comparison").AddComponent<TutorialCameraComparisonV222>();return "Waiting for paused attack";}
}
[DefaultExecutionOrder(32000)]
public class TutorialCameraComparisonV222:MonoBehaviour
{
    void LateUpdate()
    {
        var battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
        if(battle==null||!battle.TutorialWaiting||battle.CurrentLesson!=Story1WreckBattle.Lesson.Attack)return;
        var sequence=battle.GetComponent<Story1SharkCollisionSequence>();var camera=sequence.storyCamera;
        var animator=sequence.sahur.CharacterAnimator;Vector3 focus=animator.GetBoneTransform(HumanBodyBones.Chest).position;
        var fields=BindingFlags.NonPublic|BindingFlags.Instance;
        Vector3 forward=(Vector3)typeof(Story1WreckBattle).GetField("redForward",fields).GetValue(battle),right=(Vector3)typeof(Story1WreckBattle).GetField("redRight",fields).GetValue(battle);
        var position=camera.transform.position;var rotation=camera.transform.rotation;var previous=camera.targetTexture;var active=RenderTexture.active;
        var target=new RenderTexture(960,540,24);target.Create();var pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
        try
        {
            camera.transform.SetPositionAndRotation(focus+right*10+Vector3.up*1.5f-forward*16,Quaternion.LookRotation(forward*16-right*10-Vector3.up*1.5f));
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();
            File.WriteAllBytes(".codex/encounter-redesign-20261018-v222/attack-low-camera.png",pixels.EncodeToPNG());
        }
        finally{camera.transform.SetPositionAndRotation(position,rotation);camera.targetTexture=previous;RenderTexture.active=active;target.Release();Destroy(target);Destroy(pixels);Destroy(gameObject);}
    }
}
