if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode required");
var oldTimeScale=Time.timeScale;Time.timeScale=0;
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var originalScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene=UnityEngine.SceneManagement.SceneManager.CreateScene("Temporary continuous crocodile flight checks");
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
var report=new System.Collections.Generic.List<string>();
void Check(bool ok,string message){if(!ok)throw new System.Exception(message);}
try
{
    var center=new Vector3(2500,100,2500);
    var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Temporary flight ground";
    floor.transform.position=center+Vector3.down*.5f;floor.transform.localScale=new Vector3(250,1,250);
    var player=new GameObject("Temporary flight target");player.transform.position=center;
    var capsule=player.AddComponent<CharacterController>();capsule.height=2;capsule.center=Vector3.up;capsule.radius=.3f;
    var hp=player.AddComponent<Mavis.PlayerHealth>();hp.maxHealth=hp.currentHealth=100000;
    Physics.SyncTransforms();
    var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab");
    var actor=UnityEngine.Object.Instantiate(prefab,center,Quaternion.identity);
    var boss=actor.GetComponent<Mavis.BombardinoBoss>();var health=actor.GetComponent<Mavis.Health>();
    boss.target=player.transform;
    foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>())if(b!=boss)b.enabled=false;
    Check(boss.attackInterval==5f,"Prefab attack interval is not five seconds");
    Check(boss.exposedSeconds==0f,"Prefab still has an engine cooling duration");
    var starts=new System.Collections.Generic.List<float>();
    var kinds=new System.Collections.Generic.List<Mavis.BombardinoBoss.AttackKind>();
    float minClearance=float.MaxValue,minMovement=float.MaxValue;
    int warnings=0;bool seenEnraged=false,rushTargetPlaced=false,rushDamageChecked=false;
    float rushHealth=0;
    var lastState=boss.CurrentState;
    for(int i=0;i<1200;i++)
    {
        if(i==600){health.currentHealth=health.maxHealth*.4f;seenEnraged=boss.Enraged;}
        var before=boss.transform.position;boss.Tick(.05f);Physics.SyncTransforms();
        minMovement=Mathf.Min(minMovement,Vector3.ProjectOnPlane(boss.transform.position-before,Vector3.up).magnitude);
        minClearance=Mathf.Min(minClearance,boss.transform.position.y-boss.Ground(boss.transform.position).y);
        Check(!boss.IsExposed&&boss.CurrentState!=Mavis.BombardinoBoss.State.Exposed,"Boss entered landing/cooling state");
        float radius=Vector3.ProjectOnPlane(boss.transform.position-center,Vector3.up).magnitude;
        Check(Mathf.Abs(radius-boss.patrolRadius)<.01f,"Flight left its circular orbit");
        if(boss.CurrentState==Mavis.BombardinoBoss.State.Telegraph&&lastState!=boss.CurrentState)
        {
            Check(boss.GetComponentsInChildren<LineRenderer>().Any(l=>l.enabled),"Attack lost its ground warning");warnings++;
            if(boss.CurrentAttack==Mavis.BombardinoBoss.AttackKind.SpeedRun&&!rushTargetPlaced)
            {
                // Stand midway along the announced arc, rather than at the orbit's centre.
                float angle=(float)typeof(Mavis.BombardinoBoss).GetField("orbitAngle",flags).GetValue(boss);
                float at=(float)typeof(Mavis.BombardinoBoss).GetField("attackAt",flags).GetValue(boss);
                float now=(float)typeof(Mavis.BombardinoBoss).GetField("clock",flags).GetValue(boss);
                angle+=boss.cruiseSpeed/boss.patrolRadius*(at-now)+boss.rushSpeed/boss.patrolRadius*.8f;
                player.transform.position=center+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*boss.patrolRadius;
                Physics.SyncTransforms();rushHealth=hp.currentHealth;rushTargetPlaced=true;
            }
            else if(boss.CurrentAttack==Mavis.BombardinoBoss.AttackKind.Missiles&&rushTargetPlaced&&!rushDamageChecked)
            {
                Check(boss.RushHits==1&&Mathf.Abs(rushHealth-hp.currentHealth-boss.rushDamage)<.01f,"Announced curved strafe failed to hit exactly once");
                rushDamageChecked=true;player.transform.position=center;Physics.SyncTransforms();
            }
        }
        if(boss.CurrentState==Mavis.BombardinoBoss.State.Attack&&lastState!=boss.CurrentState)
        {
            starts.Add((float)typeof(Mavis.BombardinoBoss).GetField("clock",flags).GetValue(boss));kinds.Add(boss.CurrentAttack);
        }
        lastState=boss.CurrentState;
        foreach(var payload in UnityEngine.Object.FindObjectsByType<Mavis.BombardinoOrdnance>(FindObjectsSortMode.None))
            if(payload.gameObject.scene==scene)payload.Tick(.05f);
    }
    Check(starts.Count>=12,"Not enough repeated attack cycles");
    for(int i=1;i<starts.Count;i++)Check(Mathf.Abs(starts[i]-starts[i-1]-5f)<.055f,"Attack cadence drifted: "+(starts[i]-starts[i-1]));
    Check(kinds.Distinct().Count()==4,"A previous attack type stopped running");
    Check(warnings>=starts.Count,"A scheduled attack was not telegraphed");
    Check(minMovement>.9f,"Aircraft stopped during a warning, burst or interval");
    Check(minClearance>4.9f,"Living aircraft landed on the ground");
    Check(boss.BombsReleased>=18&&boss.MissilesLaunched>=9&&boss.FlameZonesCreated>=21,"Bomb, missile or flame bursts failed");
    Check(seenEnraged,"Enraged cadence was not checked");
    Check(rushDamageChecked,"Curved strafe damage was not checked");
    report.Add("60 seconds: "+starts.Count+" attack starts; all intervals 5s within one simulation frame, including enraged phase.");
    report.Add("All four attacks and ground warnings retained; bombs="+boss.BombsReleased+", missiles="+boss.MissilesLaunched+", flame zones="+boss.FlameZonesCreated+".");
    report.Add("Orbit radius remained "+boss.patrolRadius+"m; minimum movement="+minMovement.ToString("F2")+"m/frame; minimum altitude="+minClearance.ToString("F2")+"m; no Exposed/landing state.");
    report.Add("A player on the announced curved strafe received exactly one hit for "+boss.rushDamage+" damage.");
    var atPause=boss.transform.position;var clockAtPause=(float)typeof(Mavis.BombardinoBoss).GetField("clock",flags).GetValue(boss);
    boss.Tick(0);Check(boss.transform.position==atPause&&(float)typeof(Mavis.BombardinoBoss).GetField("clock",flags).GetValue(boss)==clockAtPause,"Pause advanced aircraft/attack timer");
    player.transform.position=center+Vector3.right*300;Physics.SyncTransforms();boss.Tick(.05f);
    Check(!boss.FightActive&&boss.CurrentState==Mavis.BombardinoBoss.State.Returning,"Leaving arena did not reset combat");
    for(int i=0;i<30;i++)boss.Tick(.05f);
    Check(boss.CurrentState==Mavis.BombardinoBoss.State.Patrol,"Disengaged boss did not resume circular patrol");
    Check(Vector3.ProjectOnPlane(boss.transform.position-center,Vector3.up).magnitude>31.9f,"Reset parked the aircraft");
    player.transform.position=center;Physics.SyncTransforms();for(int i=0;i<50;i++)boss.Tick(.05f);
    Check(boss.FightActive&&boss.CurrentAttack==Mavis.BombardinoBoss.AttackKind.HighBombing,"Re-engagement failed to restart attack cycle");
    health.currentHealth=0;health.OnDeath.Invoke();Check(boss.IsDead&&boss.CurrentState==Mavis.BombardinoBoss.State.Dead,"Defeat no longer stops attacks");
    report.Add("Pause, arena reset, re-engagement and defeat cleanup passed.");
}
finally
{
    foreach(var root in scene.GetRootGameObjects())if(root)UnityEngine.Object.DestroyImmediate(root);
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(originalScene);
    UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
    Time.timeScale=oldTimeScale;
}
System.IO.File.WriteAllLines("Temp/CrocContinuousOrbitVerification.txt",report);
return string.Join("\n",report);
