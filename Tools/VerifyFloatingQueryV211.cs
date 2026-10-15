using System;using System.Linq;using System.IO;using System.Reflection;using System.Collections;using UnityEngine;
public static class VerifyFloatingQueryV211{public static object Start(){var go=new GameObject("Native float geometry verification");UnityEngine.Object.DontDestroyOnLoad(go);go.AddComponent<FloatGeometryVerificationV211>();return "Waiting for initialized real query and real timber";}}
public sealed class FloatGeometryVerificationV211:MonoBehaviour{
IEnumerator Start(){Story1WreckFinale film=null;CapsuleCollider probe=null;var flags=BindingFlags.Instance|BindingFlags.NonPublic;
while(probe==null){film=UnityEngine.Object.FindFirstObjectByType<Story1WreckFinale>();if(film!=null)probe=(CapsuleCollider)typeof(Story1WreckFinale).GetField("floatingProbe",flags).GetValue(film);yield return null;}
var board=(Story1WreckPlank)typeof(Story1WreckFinale).GetField("board",flags).GetValue(film);var part=board.Collision.Parts[0];var vertices=part.sharedMesh.vertices;Vector3 mean=Vector3.zero;foreach(var v in vertices)mean+=v;mean/=vertices.Length;var at=part.transform.TransformPoint(mean);
float radius=probe.radius,height=probe.height;
try{probe.radius=.2f;probe.height=.4f;bool hit=Physics.ComputePenetration(probe,at,Quaternion.identity,part,board.Collision.PartPosition(0),board.Collision.PartRotation(0),out var direction,out float depth);
File.WriteAllText("D:/indiegameDev/unity/Dark Brine_Rot/.codex/float-swim-20261014/native-query-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{hit,depth,direction=direction.ToString("F6"),board=board.name,probe.enabled,scale=probe.transform.lossyScale.ToString("F6"),realConvexGeometry=part.convex,vertices=vertices.Length},Newtonsoft.Json.Formatting.Indented));}
finally{probe.radius=radius;probe.height=height;}Destroy(gameObject);
}}
