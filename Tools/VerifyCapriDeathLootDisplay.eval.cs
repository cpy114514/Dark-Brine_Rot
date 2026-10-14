if(!UnityEngine.Application.isPlaying)throw new System.Exception("Requires Play Mode.");
var report=new System.Collections.Generic.List<string>();
void Check(bool pass,string message){if(!pass)throw new System.Exception(message);report.Add("PASS: "+message);}
var holder=new UnityEngine.GameObject("Temporary Capri death loot QA");holder.SetActive(false);
UnityEngine.GameObject boss=null,player=null;Mavis.EquipmentPickup pickup=null;
var oldPickups=new System.Collections.Generic.HashSet<Mavis.EquipmentPickup>(UnityEngine.Object.FindObjectsByType<Mavis.EquipmentPickup>(UnityEngine.FindObjectsSortMode.None));
try
{
    var floor=UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);floor.transform.SetParent(holder.transform,false);floor.transform.position=new UnityEngine.Vector3(7000,99.5f,7000);floor.transform.localScale=new UnityEngine.Vector3(40,1,40);
    boss=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab"),holder.transform);
    boss.transform.position=new UnityEngine.Vector3(7000,100,7000);
    player=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),holder.transform);
    player.transform.position=new UnityEngine.Vector3(7030,100,7030);
    foreach(var b in holder.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true))b.enabled=false;
    holder.SetActive(true);
    player.AddComponent<Mavis.GameSaveExcluded>();
    var primary=UnityEngine.Object.FindObjectsByType<ThirdPersonPlayerController>(UnityEngine.FindObjectsSortMode.None).FirstOrDefault(p=>p.gameObject!=player);
    if(primary&&!primary.GetComponent<Mavis.GameSaveExcluded>())primary.gameObject.AddComponent<Mavis.GameSaveExcluded>();
    UnityEngine.Physics.SyncTransforms();
    var hp=boss.GetComponent<Mavis.Health>();var loot=boss.GetComponent<Mavis.CappuccinoWeaponLoot>();
    Check(loot.Drop()==null,"Living Capri does not drop swords");
    hp.ApplyDamage(hp.currentHealth+1,boss.transform.position);
    var drops=UnityEngine.Object.FindObjectsByType<Mavis.EquipmentPickup>(UnityEngine.FindObjectsSortMode.None).Where(p=>!oldPickups.Contains(p)).ToArray();
    Check(loot.HasDropped && drops.Length==1 && drops[0].Item.id=="capri_twin_blades","Actual death event creates one twin-blade pickup");
    pickup=drops[0];Check(loot.Drop()==null,"Repeated death/drop does not duplicate the reward");
    var blades=pickup.GetComponentsInChildren<UnityEngine.Renderer>().Where(r=>r.name=="Left blade"||r.name=="Right blade").ToArray();
    Check(blades.Length==2 && blades.All(r=>r.enabled && r.bounds.min.y>=100.49f),"Both complete sword meshes are above the floor");
    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
    var update=typeof(Mavis.EquipmentPickup).GetMethod("Update",flags);
    for(int i=0;i<12;i++)
    {
        pickup.transform.Find("Loot visual").localRotation=UnityEngine.Quaternion.Euler(0,i*30,0);
        update.Invoke(pickup,null);
        Check(blades.All(r=>r.bounds.min.y>100.35f),"Display stays above ground while rotating, pose "+i);
    }
    var movement=player.GetComponent<ThirdPersonPlayerController>();movement.enabled=true;movement.ExternalControlLock=false;movement.ExternalMovementLock=false;
    var weapons=player.GetComponent<Mavis.SahurWeaponLoadout>();weapons.enabled=true;weapons.Initialize();
    var inventory=player.GetComponent<Mavis.EquipmentInventory>();var body=player.GetComponent<UnityEngine.CharacterController>();
    var item=pickup.Item;var at=pickup.transform.position;at.y=100-(body.center.y-body.height*.5f)*player.transform.lossyScale.y;player.transform.position=at;
    UnityEngine.Physics.SyncTransforms();
    Check(pickup.TryCollect(inventory),"Player can collect the visible reward");
    Check(inventory.Count(item)==1 && weapons.HasTwinBlades && !pickup.TryCollect(inventory),"Pickup unlocks blades exactly once");
    Check(weapons.TryEquip(Mavis.SahurWeaponLoadout.Weapon.CapriTwinBlades) && weapons.UsesTwinBlades,"Collected swords can be equipped");
    var equipped=player.GetComponentsInChildren<UnityEngine.Renderer>(true).Where(r=>r.name=="Left blade"||r.name=="Right blade").ToArray();
    Check(equipped.Length==2,"Both swords appear in the player's hands");
    System.IO.File.WriteAllText("ProjectRepairBackups/CapriLoot-20261012/verification.txt",string.Join(System.Environment.NewLine,report));
    return report;
}
finally
{
    foreach(var drop in UnityEngine.Object.FindObjectsByType<Mavis.EquipmentPickup>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None))if(!oldPickups.Contains(drop))UnityEngine.Object.DestroyImmediate(drop.gameObject);
    UnityEngine.Object.DestroyImmediate(holder);
}
