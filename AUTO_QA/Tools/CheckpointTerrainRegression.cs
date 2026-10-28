using System;using System.IO;using System.Reflection;using System.Collections.Generic;using System.Threading.Tasks;using UnityEngine;using UnityEngine.SceneManagement;using Mavis;
public static class AutoQaCheckpointTerrain
{
 public static async Task<object> Run(string take)
 {
  if(!Application.isPlaying||SceneManager.sceneCount!=1||!string.IsNullOrEmpty(SceneManager.GetActiveScene().path)||CombatFeelPreview.Player)throw new Exception("Isolated unowned Play required");
  var rows=new List<object>();string error=null;
  try
  {
   CombatFeelPreview.Spawn();var actor=CombatFeelPreview.Player;var motor=actor.GetComponent<ThirdPersonPlayerController>();motor.ExternalControlLock=true;
   int frame=Time.frameCount+4;var limit=DateTime.UtcNow.AddSeconds(8);while(Time.frameCount<frame){if(DateTime.UtcNow>limit)throw new Exception("Native frame timeout");await Task.Delay(5);}motor.enabled=false;
   var body=actor.GetComponent<CharacterController>();var death=actor.GetComponent<PlayerDeathRespawn>();var root=CombatFeelPreview.Camera.transform.parent;
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="AUTO_QA slope platform";floor.transform.SetParent(root);floor.transform.localScale=new Vector3(18,1,18);
   var pointObject=new GameObject("AUTO_QA terrain checkpoint");pointObject.transform.SetParent(root);var point=pointObject.AddComponent<IslandRespawnPoint>();point.enabled=false;point.isDefault=false;
   var farObject=new GameObject("AUTO_QA far point");farObject.transform.SetParent(root);farObject.transform.position=new Vector3(2100,540,2100);var far=farObject.AddComponent<IslandRespawnPoint>();far.enabled=false;
   var update=typeof(IslandRespawnPoint).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic);
   foreach(float slope in new[]{0f,15f,30f,40f})
   {
    floor.transform.SetPositionAndRotation(new Vector3(2050,510+slope*.25f,2050),Quaternion.Euler(0,0,slope));Physics.SyncTransforms();
    if(!floor.GetComponent<Collider>().Raycast(new Ray(floor.transform.position+Vector3.up*20,Vector3.down),out var ground,40))throw new Exception("Platform ray control missed");
    float sole=(body.center.y-body.height*.5f)*actor.transform.lossyScale.y;body.enabled=false;actor.transform.SetPositionAndRotation(ground.point+Vector3.up*(2-sole),Quaternion.identity);body.enabled=true;Physics.SyncTransforms();
    var flags=body.Move(Vector3.down*3);Physics.SyncTransforms();Vector3 foot=body.bounds.center-Vector3.up*body.bounds.extents.y;
    rows.Add(new{ok=(flags&CollisionFlags.Below)!=0,label="Native capsule lands on platform",slope});
    pointObject.transform.position=ground.point;death.SetCheckpoint(far);update.Invoke(point,null);
    rows.Add(new{ok=death.Checkpoint==point,label="Standing at slope/platform checkpoint activates",slope,footDistance=Vector3.Distance(foot,ground.point)});
    death.SetCheckpoint(far);pointObject.transform.position=foot-Vector3.up*3.25f;update.Invoke(point,null);
    rows.Add(new{ok=death.Checkpoint==far,label="Separate floor remains excluded",slope});
   }
  }
  catch(Exception e){error=e.ToString();}finally{CombatFeelPreview.Clear();}
  var result=new{error,rows};File.WriteAllText("AUTO_QA/Evidence/"+take+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
