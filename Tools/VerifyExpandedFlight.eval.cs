if(!Application.isPlaying)throw new System.Exception("Play Mode required");
Time.timeScale=0;
var notes=new System.Collections.Generic.List<string>();
void Check(bool pass,string message){if(!pass)throw new System.Exception(message);notes.Add("PASS "+message);}
var scene=UnityEngine.SceneManagement.SceneManager.CreateScene("Expanded aircraft weapon checks");var old=UnityEngine.SceneManagement.SceneManager.GetActiveScene();UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
try
{
    var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,499.5f,0);floor.transform.localScale=new Vector3(180,1,180);
    var player=new GameObject("Flight weapon victim");player.transform.position=new Vector3(0,500,0);player.tag="Player";
    var capsule=player.AddComponent<CharacterController>();capsule.height=2;capsule.radius=.3f;capsule.center=Vector3.up;var hp=player.AddComponent<Mavis.PlayerHealth>();hp.maxHealth=hp.currentHealth=1000;
    var source=UnityEngine.Object.FindFirstObjectByType<Mavis.BombardinoBoss>();Physics.SyncTransforms();
    var bombObj=new GameObject("63m bombing check");bombObj.transform.position=new Vector3(0,563,0);
    var bomb=bombObj.AddComponent<Mavis.BombardinoOrdnance>();bomb.Launch(source,Mavis.BombardinoOrdnance.Kind.Bomb,Vector3.down,player.transform,player.transform.position,28,4.3f);
    for(int i=0;i<180&&bomb;i++)bomb.Tick(.03f);
    Check(hp.currentHealth<1000,"Bombs from new 63m attack altitude reach and damage the ground target before expiry");
    hp.currentHealth=1000;var missileObj=new GameObject("Expanded missile standoff check");missileObj.transform.position=new Vector3(0,533,-42);
    var missile=missileObj.AddComponent<Mavis.BombardinoOrdnance>();Vector3 aim=capsule.bounds.center;missile.Launch(source,Mavis.BombardinoOrdnance.Kind.Missile,aim-missileObj.transform.position,player.transform,aim,23,3.5f);
    for(int i=0;i<180&&missile;i++)missile.Tick(.025f);
    Check(hp.currentHealth<1000,"Missiles reach targets from the expanded 42m/33m standoff");
    notes.Add("Existing close-range Exposed landing uses lowAltitude="+source.lowAltitude+" rather than cruiseAltitude="+source.cruiseAltitude);
}
finally{UnityEngine.SceneManagement.SceneManager.SetActiveScene(old);UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);}
System.IO.File.WriteAllLines("ProjectRepairBackups/CapriWeapons-20261007/flight-verification.txt",notes);return notes;
