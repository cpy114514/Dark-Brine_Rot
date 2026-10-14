if(!UnityEngine.Application.isPlaying)throw new System.Exception("Requires Play Mode");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var holder=new UnityEngine.GameObject("Temporary twin-blade hit QA");holder.SetActive(false);
var actor=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),holder.transform);
actor.transform.position=new UnityEngine.Vector3(7000,500,7000);
foreach(var behaviour in actor.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true))behaviour.enabled=false;
holder.SetActive(true);
var target=new UnityEngine.GameObject("Temporary blade contact target");target.transform.SetParent(holder.transform,true);
var hp=target.AddComponent<Mavis.Health>();hp.maxHealth=hp.currentHealth=1000;var sphere=target.AddComponent<UnityEngine.SphereCollider>();sphere.radius=.08f;
var rows=new System.Collections.Generic.List<string>();
void Check(bool pass,string message){if(!pass)throw new System.Exception(message);rows.Add("PASS: "+message);}
try
{
    var primary=UnityEngine.Object.FindObjectsByType<ThirdPersonPlayerController>(UnityEngine.FindObjectsSortMode.None).FirstOrDefault(p=>p.gameObject!=actor);
    if(primary&&!primary.GetComponent<Mavis.GameSaveExcluded>())primary.gameObject.AddComponent<Mavis.GameSaveExcluded>();
    actor.AddComponent<Mavis.GameSaveExcluded>();
    var movement=actor.GetComponent<ThirdPersonPlayerController>();movement.enabled=true;movement.ExternalControlLock=false;movement.ExternalMovementLock=false;
    var weapons=actor.GetComponent<Mavis.SahurWeaponLoadout>();weapons.enabled=true;weapons.Initialize();
    actor.GetComponent<Mavis.EquipmentInventory>().Collect(UnityEngine.Resources.Load<Mavis.EquipmentItem>("Equipment/Capri/CapriTwinBlades"));
    Check(weapons.TryEquip(Mavis.SahurWeaponLoadout.Weapon.CapriTwinBlades),"Real loadout equips both sword colliders");
    var attack=actor.GetComponent<Mavis.SahurAttack>();attack.enemyTag="";attack.hitMask=~0;
    void Set(string name,object value)=>typeof(Mavis.SahurAttack).GetField(name,flags).SetValue(attack,value);
    var left=(UnityEngine.CapsuleCollider)typeof(Mavis.SahurAttack).GetField("twinLeft",flags).GetValue(attack);
    var right=(UnityEngine.CapsuleCollider)typeof(Mavis.SahurAttack).GetField("twinRight",flags).GetValue(attack);
    var hits=(System.Collections.Generic.HashSet<Mavis.IDamageable>)typeof(Mavis.SahurAttack).GetField("hitThisSwing",flags).GetValue(attack);
    var update=typeof(Mavis.SahurAttack).GetMethod("UpdateHitbox",flags);
    var scan=typeof(Mavis.SahurAttack).GetMethod("ScanHits",flags);
    (UnityEngine.Vector3 a,UnityEngine.Vector3 b,float radius) Volume(UnityEngine.Collider weapon)
    {
        var old=attack.stickHitbox;attack.stickHitbox=weapon;var args=new object[]{UnityEngine.Vector3.zero,UnityEngine.Vector3.zero,0f};
        try{typeof(Mavis.SahurAttack).GetMethod("GetWeaponVolume",flags).Invoke(attack,args);return ((UnityEngine.Vector3)args[0],(UnityEngine.Vector3)args[1],(float)args[2]);}
        finally{attack.stickHitbox=old;}
    }
    float Distance(UnityEngine.Vector3 p,UnityEngine.Vector3 a,UnityEngine.Vector3 b){var d=b-a;return UnityEngine.Vector3.Distance(p,a+d*UnityEngine.Mathf.Clamp01(UnityEngine.Vector3.Dot(p-a,d)/UnityEngine.Mathf.Max(.00001f,d.sqrMagnitude)));}
    void Reset(){hp.currentHealth=1000;hits.Clear();Set("hasWeaponSample",false);Set("observedAttackPhase",-1f);}
    for(int stage=0;stage<3;stage++)
    {
        int hash=UnityEngine.Animator.StringToHash("Base Layer.Capri Combo "+(stage+1));
        Set("attacking",true);Set("comboStep",stage);Set("activeAttackHash",hash);Set("swingDamage",10f);
        bool found=false;UnityEngine.Vector3 isolated=default;
        for(int step=30;step<=60&&!found;step+=5)
        {
            attack.animator.Play(hash,0,step/100f);attack.animator.Update(0);UnityEngine.Physics.SyncTransforms();
            var l=Volume(left);var r=Volume(right);
            foreach(float t in new[]{.2f,.5f,.8f}){var p=UnityEngine.Vector3.Lerp(l.a,l.b,t);if(Distance(p,r.a,r.b)>r.radius+.18f){isolated=p;found=true;break;}}
        }
        Check(found,"Combo "+(stage+1)+" has a physically isolated left-sword contact");
        target.transform.position=isolated;Reset();Set("secondBlade",null);UnityEngine.Physics.SyncTransforms();update.Invoke(attack,null);
        Check(hp.currentHealth==1000,"Old right-only binding misses the left-sword target, combo "+(stage+1));
        Reset();Set("secondBlade",left);UnityEngine.Physics.SyncTransforms();update.Invoke(attack,null);update.Invoke(attack,null);
        Check(UnityEngine.Mathf.Abs(hp.currentHealth-990)<.01f,"Corrected left sword hits once, combo "+(stage+1));
        var rNow=Volume(right);target.transform.position=(rNow.a+rNow.b)*.5f;Reset();UnityEngine.Physics.SyncTransforms();update.Invoke(attack,null);update.Invoke(attack,null);
        Check(UnityEngine.Mathf.Abs(hp.currentHealth-990)<.01f,"Right sword still hits once, combo "+(stage+1));
    }
    int guardedCombo=UnityEngine.Animator.StringToHash("Base Layer.Capri Combo 1");
    Set("comboStep",0);Set("activeAttackHash",guardedCombo);Set("swingDamage",10f);
    attack.animator.Play(guardedCombo,0,.4f);attack.animator.Update(0);
    var leftContact=Volume(left);target.transform.position=(leftContact.a+leftContact.b)*.5f;Reset();
    UnityEngine.Physics.SyncTransforms();
    var origin=actor.GetComponent<UnityEngine.CharacterController>().bounds.center;
    var wall=UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);wall.transform.SetParent(holder.transform,true);
    wall.transform.SetPositionAndRotation((origin+target.transform.position)*.5f,UnityEngine.Quaternion.LookRotation(target.transform.position-origin));wall.transform.localScale=new UnityEngine.Vector3(5,5,.15f);
    UnityEngine.Physics.SyncTransforms();update.Invoke(attack,null);
    Check(hp.currentHealth==1000,"Left-sword contact is blocked by a solid wall");UnityEngine.Object.DestroyImmediate(wall);
    var weak=Mavis.BossWeakPoint.Create(target.transform,UnityEngine.Vector3.zero,.12f);Reset();UnityEngine.Physics.SyncTransforms();update.Invoke(attack,null);update.Invoke(attack,null);
    Check(UnityEngine.Mathf.Abs(hp.currentHealth-985)<.01f,"Left-sword weak-point contact applies one 1.5x hit");UnityEngine.Object.DestroyImmediate(weak.gameObject);
    var rBoth=Volume(right);target.transform.position=((leftContact.a+leftContact.b)+(rBoth.a+rBoth.b))*.25f;
    sphere.radius=UnityEngine.Vector3.Distance((leftContact.a+leftContact.b)*.5f,(rBoth.a+rBoth.b)*.5f)*.5f+1;
    Reset();UnityEngine.Physics.SyncTransforms();update.Invoke(attack,null);update.Invoke(attack,null);
    Check(hp.currentHealth==990,"Both swords touching the same target still cause one hit");
    foreach(float phase in new[]{.1f,.95f})
    {
        attack.animator.Play(guardedCombo,0,phase);attack.animator.Update(0);Reset();UnityEngine.Physics.SyncTransforms();update.Invoke(attack,null);
        Check(hp.currentHealth==1000&&!left.enabled&&!right.enabled,"No damage during anticipation/recovery at phase "+phase);
    }
    sphere.radius=.08f;target.transform.position=actor.transform.position+UnityEngine.Vector3.back*12;
    attack.animator.Play(guardedCombo,0,.4f);attack.animator.Update(0);Reset();UnityEngine.Physics.SyncTransforms();update.Invoke(attack,null);
    Check(hp.currentHealth==1000,"A distant target behind the player receives no damage");
    // A small target crosses the middle of the left blade between damaging frames,
    // clear of both the handle and tip trails and the other sword.
    Set("comboStep",0);Set("swingDamage",10f);int combo=UnityEngine.Animator.StringToHash("Base Layer.Capri Combo 1");Set("activeAttackHash",combo);
    var centre=actor.transform.position+new UnityEngine.Vector3(0,3,3);
    var axis=left.direction==0?UnityEngine.Vector3.right:left.direction==1?UnityEngine.Vector3.up:UnityEngine.Vector3.forward;
    left.transform.rotation=UnityEngine.Quaternion.FromToRotation(axis,UnityEngine.Vector3.up);
    void PlaceLeft(UnityEngine.Vector3 c)=>left.transform.position=c-left.transform.TransformVector(left.center);
    right.transform.position=actor.transform.position+UnityEngine.Vector3.right*20;
    target.transform.position=centre;Reset();left.enabled=right.enabled=true;
    attack.animator.Play(combo,0,.35f);PlaceLeft(centre-UnityEngine.Vector3.right*4);UnityEngine.Physics.SyncTransforms();scan.Invoke(attack,null);
    Check(hp.currentHealth==1000,"First pose does not touch the between-frame target");
    attack.animator.Play(combo,0,.45f);PlaceLeft(centre+UnityEngine.Vector3.right*4);UnityEngine.Physics.SyncTransforms();scan.Invoke(attack,null);
    Check(UnityEngine.Mathf.Abs(hp.currentHealth-990)<.01f,"Whole left-blade sweep catches the middle contact between frames");
    scan.Invoke(attack,null);Check(hp.currentHealth==990,"Repeated queries do not multiply one swing's damage");
    var cancel=typeof(Mavis.SahurAttack).GetMethod("CancelForMovement",flags);cancel.Invoke(attack,null);
    Check(!left.enabled&&!right.enabled,"Cancellation disables both blade hitboxes");
    Check(weapons.TryEquip(Mavis.SahurWeaponLoadout.Weapon.Stick)&&!attack.UsesTwinBlades,"Switching back restores the stick");
    System.IO.File.WriteAllText("ProjectRepairBackups/TwinBladeHits-20261012/verification.txt",string.Join(System.Environment.NewLine,rows));return rows;
}
finally{UnityEngine.Object.DestroyImmediate(holder);}
