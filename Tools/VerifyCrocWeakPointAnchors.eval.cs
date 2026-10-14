if(!UnityEngine.Application.isPlaying)throw new System.Exception("Requires Play Mode.");
var report=new System.Collections.Generic.List<string>();
void Check(bool condition,string message){if(!condition)throw new System.Exception(message);}
var holder=new UnityEngine.GameObject("Temporary aircraft weak point QA");holder.SetActive(false);
UnityEngine.GameObject actor=null,attacker=null;
try
{
    var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab");
    actor=UnityEngine.Object.Instantiate(prefab,holder.transform);
    actor.transform.position=new UnityEngine.Vector3(7000,100,7000);
    foreach(var behaviour in actor.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true))behaviour.enabled=false;
    holder.SetActive(true);
    var boss=actor.GetComponent<Mavis.BombardinoBoss>();var setup=actor.GetComponent<Mavis.BossWeaknessSetup>();
    var health=actor.GetComponent<Mavis.Health>();
    var engines=actor.GetComponentsInChildren<UnityEngine.BoxCollider>().Where(c=>c.name=="Left engine"||c.name=="Right engine").ToArray();
    Check(engines.Length==2,"Expected two physical engines");
    UnityEngine.Physics.SyncTransforms();
    actor.transform.position+=UnityEngine.Vector3.up*55;
    Check(UnityEngine.Vector3.Distance(engines[0].bounds.center,engines[0].transform.TransformPoint(engines[0].center))>50,"Did not reproduce stale physics bounds");
    setup.Initialize();setup.Initialize();
    Check(actor.GetComponentsInChildren<Mavis.BossWeakPoint>().Length==2,"Engine points missing or duplicated");
    foreach(float scale in new[]{1f,2f,3f})
    {
        actor.transform.localScale=UnityEngine.Vector3.one*scale;
        foreach(float bank in new[]{-40f,0f,40f})
        foreach(float heading in new[]{0f,90f,210f})
        {
            actor.transform.SetPositionAndRotation(new UnityEngine.Vector3(7000+heading,100+bank,7000),UnityEngine.Quaternion.Euler(0,heading,0));
            boss.visual.localRotation=UnityEngine.Quaternion.Euler(8,180,bank);
            setup.Initialize();
            foreach(var engine in engines)
            {
                var weak=engine.GetComponentInChildren<Mavis.BossWeakPoint>();
                var offset=engine.center-UnityEngine.Vector3.up*(engine.size.y*.325f);
                Check(UnityEngine.Vector3.Distance(weak.transform.position,engine.transform.TransformPoint(offset))<.001f,"Weak point detached from engine");
                Check(weak.transform.parent==engine.transform,"Weak point attached to wrong body");
                Check(engine.transform.InverseTransformPoint(weak.transform.position).y>engine.center.y-engine.size.y*.5f,"Weak point below engine");
            }
        }
    }
    report.Add("27 scale/heading/bank poses: both hit volumes remain on engine undersides, without Physics.SyncTransforms.");
    foreach(var weak in actor.GetComponentsInChildren<Mavis.BossWeakPoint>())weak.transform.localPosition=UnityEngine.Vector3.down*100;
    setup.Initialize();Check(actor.GetComponentsInChildren<Mavis.BossWeakPoint>().Length==2,"Refit duplicated existing points");
    report.Add("Existing misplaced points refit in place; repeated initialization does not duplicate them.");
    UnityEngine.Physics.SyncTransforms();
    attacker=new UnityEngine.GameObject("Temporary weak point hit source");
    foreach(var weak in actor.GetComponentsInChildren<Mavis.BossWeakPoint>())
    {
        float before=health.currentHealth;
        Mavis.CombatHitFeedback.Apply(attacker,health,10,weak.transform.position,Mavis.CombatHitKind.ComboOne);
        Check(UnityEngine.Mathf.Abs(before-health.currentHealth-15)<.01f,"Engine weakness did not cause 1.5x damage");
    }
    float ordinaryBefore=health.currentHealth;
    Mavis.CombatHitFeedback.Apply(attacker,health,10,actor.transform.position+UnityEngine.Vector3.up*100,Mavis.CombatHitKind.ComboOne);
    Check(UnityEngine.Mathf.Abs(ordinaryBefore-health.currentHealth-10)<.01f,"Ordinary hit received weakness multiplier");
    report.Add("Both engine hits: 15 damage from base 10; ordinary hit: 10 damage.");
    System.IO.File.WriteAllText("ProjectRepairBackups/CrocWeakPoint-20261012/verification.txt",string.Join(System.Environment.NewLine,report));
    return report;
}
finally
{
    if(attacker)UnityEngine.Object.DestroyImmediate(attacker);
    UnityEngine.Object.DestroyImmediate(holder);
}
