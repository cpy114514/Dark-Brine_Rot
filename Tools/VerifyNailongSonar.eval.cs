if(!Application.isPlaying)throw new System.Exception("Play required");
Time.timeScale=0;
foreach(var menu in UnityEngine.Object.FindObjectsByType<PauseSettingsMenu>(FindObjectsSortMode.None)){if(PauseSettingsMenu.IsOpen)menu.Resume();menu.enabled=false;}
Time.timeScale=0;
foreach(var script in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(script.GetType().Name=="FirstIslandArrival"){script.StopAllCoroutines();script.enabled=false;}
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
void Set(object obj,string name,object value)=>obj.GetType().GetField(name,flags).SetValue(obj,value);
var notes=new System.Collections.Generic.List<string>();
void Check(bool ok,string message){notes.Add((ok?"PASS ":"FAIL ")+message);if(!ok)throw new System.Exception(message);}
var player=UnityEngine.Object.FindFirstObjectByType<Mavis.PlayerHealth>();var move=player.GetComponent<ThirdPersonPlayerController>();
var nail=UnityEngine.Object.FindFirstObjectByType<Mavis.NailongAI>();var source=nail.GetComponent<Mavis.NailongAttack>();
foreach(var ai in UnityEngine.Object.FindObjectsByType<Mavis.CappuccinoAI>(FindObjectsSortMode.None))ai.enabled=false;
foreach(var ai in UnityEngine.Object.FindObjectsByType<Mavis.BombardinoBoss>(FindObjectsSortMode.None))ai.enabled=false;
nail.enabled=false;source.enabled=true;
var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Sonar verification floor";floor.transform.position=new Vector3(0,499.5f,0);floor.transform.localScale=new Vector3(100,1,100);
move.enabled=true;move.ExternalControlLock=false;move.ExternalMovementLock=true;player.GetComponent<CharacterController>().enabled=true;
void Place(float distance,float clearance=0)
{
    move.RestoreSavedPose(new Vector3(distance,503.5f,0),Quaternion.identity);move.ExternalMovementLock=true;
    Physics.SyncTransforms();var body=player.GetComponent<CharacterController>();player.transform.position+=Vector3.up*(500.02f+clearance-body.bounds.min.y);Physics.SyncTransforms();
    player.currentHealth=100;Set(player,"protectedUntil",0f);
}
Mavis.NailongGroundSlam Wave(int count=3)
{
    var go=new GameObject("Sonar verification wave");go.transform.position=new Vector3(0,500,0);
    var wave=go.AddComponent<Mavis.NailongGroundSlam>();wave.Initialize(source,12,count,1,2.4f,.4f);return wave;
}
Place(6);var stacked=player.gameObject.AddComponent<SphereCollider>();stacked.isTrigger=true;stacked.center=player.GetComponent<CharacterController>().center;stacked.radius=.2f;
var grounded=Wave();for(int i=0;i<44;i++)grounded.Tick(.1f);
Check(Mathf.Abs(player.currentHealth-55)<.01f && grounded.DamageHits==3,"Three expanding rings deal exactly 15 damage each, never multiplied by extra colliders");
grounded.gameObject.SetActive(false);stacked.enabled=false;
Place(6,1.5f);var jumping=Wave();for(int i=0;i<44;i++)jumping.Tick(.1f);
Check(player.currentHealth==100 && jumping.JumpDodges==3,"Feet above the sonar clear all three pulses without damage");jumping.gameObject.SetActive(false);
Place(6);var distant=Wave();Place(16);for(int i=0;i<44;i++)distant.Tick(.1f);
Check(player.currentHealth==100,"Player outside the final radius takes no damage");distant.gameObject.SetActive(false);
Place(6);var late=Wave(1);late.Tick(2.4f);
Check(player.currentHealth==85 && late.DamageHits==1,"A long frame sweeping across a grounded player still deals one 15-point hit");late.gameObject.SetActive(false);
Place(6,1.5f);var land=Wave(1);land.Tick(1.3f);player.transform.position-=Vector3.up*1.5f;Physics.SyncTransforms();land.Tick(.1f);
Check(player.currentHealth==100 && land.JumpDodges==1,"Landing after successfully jumping a ring does not cause a delayed hit");land.gameObject.SetActive(false);
Place(6);var paused=Wave(1);paused.Tick(0);Check(paused.PulsesReleased==0 && player.currentHealth==100,"Zero delta does not advance sonar or damage");paused.gameObject.SetActive(false);
Place(6);var enraged=Wave(1);float baseDamage=source.damage;source.damage=999;enraged.Tick(1.4f);
Check(player.currentHealth==85,"Sonar stays at 15 damage even when the owner's other attacks are amplified");source.damage=baseDamage;enraged.gameObject.SetActive(false);
Place(6);var show=Wave();show.Tick(2.1f);
Check(show.PulsesReleased==3 && show.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled)==3,"Three differently sized concentric rings are visible simultaneously");
var meshes=show.GetComponentsInChildren<MeshFilter>();
Check(meshes.All(m=>m.sharedMesh.vertexCount==195) && meshes.Select(m=>m.sharedMesh.bounds.extents.x).Distinct().Count()==3,"Bounded dynamic meshes track the different wave radii");
show.gameObject.name="Sonar visual review";player.currentHealth=100;
var aiType=nail.GetType();var pattern=System.Enum.Parse(aiType.GetNestedType("AttackPattern",System.Reflection.BindingFlags.NonPublic),"GroundSlam");
Set(nail,"target",player.transform);aiType.GetMethod("BeginAttack",flags).Invoke(nail,new[]{pattern});
Check(nail.AttackName=="WAVE BREAKER" && nail.CombatHint.Contains("JUMP OVER EACH RING"),"English boss hint explains jumping over each pulse");
float duration=(float)aiType.GetMethod("AttackDuration",flags).Invoke(nail,new[]{pattern});
Check(duration+.001f>=nail.slamWindup+nail.SonarLifetime,"Boss waits for the sonar volley before starting another attack");
nail.GetComponent<Mavis.NailongAttackMotion>().Stop();
System.IO.File.WriteAllLines("ProjectRepairBackups/NailongSonar-20261007/verification.txt",notes);return notes;
