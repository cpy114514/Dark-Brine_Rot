using System;
using System.Linq;
using System.Reflection;
using Mavis;
using UnityEngine;
using UnityEditor;

public static class ProbeAbduction052
{
    public static string Pose()
    {
        if(!Application.isPlaying||!EditorApplication.isPaused)throw new Exception("Paused owned review required");
        var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var animator=player.CharacterAnimator;var root=player.transform;var position=root.position;var rotation=root.rotation;
        using(var pose=new SahurAbductionPose(player))
        {
            pose.Sample(.2f);var left=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);var lower=animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);var hand=animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var right=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);var rightLower=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);var rightHand=animator.GetBoneTransform(HumanBodyBones.RightHand);
            float height=player.GetComponent<CharacterController>().height*Mathf.Abs(root.lossyScale.y);
            Vector3 centre=pose.Shoulders+Vector3.up*pose.ArmReach*.74f+root.forward*height*.02f;
            var before=new{left=left.position,lower=lower.position,hand=hand.position,right=right.position,rightLower=rightLower.position,rightHand=rightHand.position,leftReach=pose.ArmReach,rightReach=Vector3.Distance(right.position,rightLower.position)+Vector3.Distance(rightLower.position,rightHand.position),targetL=centre-root.right*height*.12f,targetR=centre+root.right*height*.12f,height};
            pose.Hold(centre,root.right,height*.12f,1);
            var result=new{before,after=new{left=left.position,lower=lower.position,hand=hand.position,right=right.position,rightLower=rightLower.position,rightHand=rightHand.position,pose.GripError},scales=new{left=left.lossyScale,lower=lower.lossyScale,root=root.lossyScale}};
            root.SetPositionAndRotation(position,rotation);return Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented,new Vectors052());
        }
    }
}
public sealed class Vectors052:Newtonsoft.Json.JsonConverter
{
    public override bool CanConvert(Type type)=>type==typeof(Vector3);
    public override void WriteJson(Newtonsoft.Json.JsonWriter writer,object value,Newtonsoft.Json.JsonSerializer serializer){var v=(Vector3)value;serializer.Serialize(writer,new[]{v.x,v.y,v.z});}
    public override object ReadJson(Newtonsoft.Json.JsonReader reader,Type type,object existing,Newtonsoft.Json.JsonSerializer serializer)=>throw new NotImplementedException();
}
