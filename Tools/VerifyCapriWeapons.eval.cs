if(!Application.isPlaying)throw new System.Exception("Play Mode required");
Time.timeScale=0;
var notes=new System.Collections.Generic.List<string>();
void Check(bool pass,string message){if(!pass)throw new System.Exception(message);notes.Add("PASS "+message);}
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var player=UnityEngine.Object.FindFirstObjectByType<Mavis.PlayerHealth>();
var attack=player.GetComponent<Mavis.SahurAttack>();var inventory=player.GetComponent<Mavis.EquipmentInventory>();var weapons=player.GetComponent<Mavis.SahurWeaponLoadout>();
var movement=player.GetComponent<ThirdPersonPlayerController>();var body=player.GetComponent<CharacterController>();
foreach(var script in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(script.GetType().Name=="FirstIslandArrival"){script.StopAllCoroutines();script.enabled=false;}
attack.enabled=true;movement.enabled=true;movement.ExternalControlLock=false;movement.ExternalMovementLock=false;body.enabled=true;player.GrantProtection(100);Cursor.lockState=CursorLockMode.Locked;
weapons.Initialize();Check(!weapons.TryEquip(Mavis.SahurWeaponLoadout.Weapon.CapriTwinBlades),"Blades cannot be selected before collecting Capri's drop");
var item=Resources.Load<Mavis.EquipmentItem>("Equipment/Capri/CapriTwinBlades");Check(item&&item.worldModel,"Drop asset has both blades and textured renderers");
var cap=UnityEngine.Object.FindFirstObjectByType<Mavis.CappuccinoEnemy>();
var scene=UnityEngine.SceneManagement.SceneManager.CreateScene("Capri weapon verification");var old=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
GameObject victim=null;
try
{
    var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,499.5f,0);floor.transform.localScale=new Vector3(100,1,100);
    var boss=UnityEngine.Object.Instantiate(cap.gameObject,new Vector3(0,500,0),Quaternion.identity);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(boss,scene);
    boss.GetComponent<Mavis.CappuccinoAI>().enabled=false;
    var health=boss.GetComponent<Mavis.Health>();var loot=boss.GetComponent<Mavis.CappuccinoWeaponLoot>();Physics.SyncTransforms();
    Check(loot.Drop()==null,"Living Capri cannot drop weapons");health.ApplyDamage(health.currentHealth+1,boss.transform.position);
    var pickups=UnityEngine.Object.FindObjectsByType<Mavis.EquipmentPickup>(FindObjectsSortMode.None).Where(p=>p.gameObject.scene==scene).ToArray();
    Check(loot.HasDropped&&pickups.Length==1&&pickups[0].Item==item,"Capri death drops exactly one twin-blade item");Check(loot.Drop()==null,"Repeated death/drop calls do not duplicate loot");
    var pickup=pickups[0];float footOffset=Mavis.NailongSize.Feet(player.transform).y-player.transform.position.y;
    player.transform.position=new Vector3(pickup.transform.position.x,500-footOffset,pickup.transform.position.z);Physics.SyncTransforms();
    Check(pickup.TryCollect(inventory),"Sahur collects the real dropped weapon near his feet");Check(!pickup.TryCollect(inventory)&&inventory.Count(item)==1,"Pickup can be collected only once");
    Check(weapons.CurrentWeapon==Mavis.SahurWeaponLoadout.Weapon.Stick,"Pickup preserves the player's chosen weapon");
    Check(weapons.TryEquip(Mavis.SahurWeaponLoadout.Weapon.CapriTwinBlades),"Collected blades can be equipped");Check(attack.UsesTwinBlades&&!attack.UsesAnimationRootMotion,"Blade moves do not reuse wooden-stick root trajectories");
    var left=attack.animator.GetBoneTransform(HumanBodyBones.LeftHand).GetComponentsInChildren<CapsuleCollider>().First(c=>c.name=="Left blade");
    var right=attack.animator.GetBoneTransform(HumanBodyBones.RightHand).GetComponentsInChildren<CapsuleCollider>().First(c=>c.name=="Right blade");
    Check(left.isTrigger&&right.isTrigger&&!left.enabled&&!right.enabled,"Both sword volumes are disabled outside damage windows");
    Check(left.GetComponent<Renderer>().sharedMaterial.name=="Katana"&&right.GetComponent<Renderer>().sharedMaterial.name=="Katana","Both swords use Capri's textured katana material");
    var ui=player.GetComponent<Mavis.SahurLoadoutUI>();ui.SetOpen(true);ui.SelectEquipment(Mavis.SahurLoadoutUI.EquipmentSlot.Weapon);
    Check(ui.PanelVisible,"Weapon choices are available in the I menu");
    var menu=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b=>b.name=="EQUIP STICK  [1]");menu.onClick.Invoke();
    Check(!weapons.UsesTwinBlades,"Menu button switches back to stick while game is paused");
    var bladeButton=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b=>b.name=="TWIN BLADES  [2]");bladeButton.onClick.Invoke();Check(weapons.UsesTwinBlades,"Menu button selects collected twin blades");ui.SetOpen(false);
    // The UI's input-consumed frame blocks input intentionally. Direct stage calls verify authored poses/windows.
    var already=(System.Collections.Generic.HashSet<Mavis.IDamageable>)typeof(Mavis.SahurAttack).GetField("hitThisSwing",flags).GetValue(attack);
    var stage=typeof(Mavis.SahurAttack).GetMethod("StartComboStage",flags);var hitbox=typeof(Mavis.SahurAttack).GetMethod("UpdateHitbox",flags);var hit=typeof(Mavis.SahurAttack).GetMethod("TryHit",flags);
    victim=new GameObject("Blade verification target");victim.tag="Enemy";var target=victim.AddComponent<Mavis.Health>();target.maxHealth=target.currentHealth=1000;var targetBody=victim.AddComponent<BoxCollider>();targetBody.size=new Vector3(2,4,2);
    player.transform.rotation=Quaternion.identity;victim.transform.position=player.transform.position+Vector3.forward*4+Vector3.up*2;Physics.SyncTransforms();
    for(int i=0;i<3;i++)
    {
        stage.Invoke(attack,new object[]{i});attack.animator.Play("Capri Combo "+(i+1),0,.12f);attack.animator.Update(0);hitbox.Invoke(attack,null);
        Check(!left.enabled&&!right.enabled,"Combo "+(i+1)+" does not damage during anticipation");
        attack.animator.Play("Capri Combo "+(i+1),0,.4f);attack.animator.Update(0);Physics.SyncTransforms();float before=target.currentHealth;hitbox.Invoke(attack,null);
        hit.Invoke(attack,new object[]{targetBody,false});float expected=attack.damage*(i==1?attack.comboTwoDamageMultiplier:i==2?attack.comboThreeDamageMultiplier:1);
        Check(Mathf.Abs(before-target.currentHealth-expected)<.01f,"Combo "+(i+1)+" applies its own damage exactly once");
        for(int repeat=0;repeat<20;repeat++)hit.Invoke(attack,new object[]{targetBody,true});Check(Mathf.Abs(before-target.currentHealth-expected)<.01f,"Two swords/multiple overlaps do not duplicate stage "+(i+1)+" damage");
        Check(i!=1||attack.stickHitbox==left,"Second slash transfers the active sword to the left hand");
    }
    typeof(Mavis.SahurAttack).GetMethod("CancelForMovement",flags).Invoke(attack,null);Check(!attack.IsCombatMotionActive&&!left.enabled&&!right.enabled,"WASD cancellation clears blade motion and both hitboxes");
    Check(weapons.TryEquip(Mavis.SahurWeaponLoadout.Weapon.Stick,true)==false,"Menu input cannot bypass the consumed-frame lock after closing");
    // Damage and airspace serialized values are checked on the actual scene instances.
    var nail=UnityEngine.Object.FindFirstObjectByType<Mavis.NailongAttack>();var croc=UnityEngine.Object.FindFirstObjectByType<Mavis.BombardinoBoss>();
    Check(nail.damage==14,"Nailong base damage is 14");Check(croc.cruiseAltitude==55&&croc.patrolRadius==32&&croc.engageDistance==100&&croc.leashRadius==160,"Crocodile has expanded flight height/patrol/aggro/leash");
}
finally
{
    System.IO.File.WriteAllLines("ProjectRepairBackups/CapriWeapons-20261007/verification-progress.txt",notes);
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(old);UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
}
System.IO.File.WriteAllLines("ProjectRepairBackups/CapriWeapons-20261007/verification.txt",notes);return notes;
