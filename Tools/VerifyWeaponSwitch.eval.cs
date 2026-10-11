if(!Application.isPlaying)throw new System.Exception("Play Mode required");
var p=UnityEngine.Object.FindFirstObjectByType<Mavis.PlayerHealth>();
var arrival=p.GetComponent<FirstIslandArrival>();if(arrival!=null){arrival.StopAllCoroutines();arrival.enabled=false;}
var move=p.GetComponent<ThirdPersonPlayerController>();move.enabled=true;move.ExternalControlLock=false;move.ExternalMovementLock=false;
var attack=p.GetComponent<Mavis.SahurAttack>();attack.enabled=true;p.GrantProtection(10000);
var weapons=p.GetComponent<Mavis.SahurWeaponLoadout>();var ui=p.GetComponent<Mavis.SahurLoadoutUI>();var inventory=p.GetComponent<Mavis.EquipmentInventory>();
var checks=new System.Collections.Generic.List<string>();
void Check(bool okay,string text){if(!okay)throw new System.Exception(text);checks.Add("PASS "+text);}
Check(weapons!=null&&ui!=null&&inventory!=null,"Movement initialization installs inventory, weapons and UI");
ui.enabled=true;weapons.enabled=true;weapons.Initialize();
Check(!weapons.HasTwinBlades&&weapons.CurrentWeapon==Mavis.SahurWeaponLoadout.Weapon.Stick,"New game starts with stick; Capri blades remain locked");
Cursor.lockState=CursorLockMode.Locked;Time.timeScale=0;
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
void Refresh(){typeof(Mavis.SahurLoadoutUI).GetMethod("RefreshVisibility",flags).Invoke(ui,null);}
Refresh();var hint=(UnityEngine.UI.Text)typeof(Mavis.SahurLoadoutUI).GetField("weaponHint",flags).GetValue(ui);
Check(hint.gameObject.activeSelf&&hint.text.Contains("[1]")&&hint.text.Contains("[2]")&&hint.text.Contains("[I]"),"HUD exposes weapon and gear controls");
var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
void Press(UnityEngine.InputSystem.Key key,object target){
 UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(key));UnityEngine.InputSystem.InputSystem.Update();
 target.GetType().GetMethod("Update",flags).Invoke(target,null);
 UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());UnityEngine.InputSystem.InputSystem.Update();
}
try{
 Press(UnityEngine.InputSystem.Key.I,ui);Check(ui.PanelVisible&&Mavis.SahurLoadoutUI.IsOpen,"I opens equipment");
 var choices=(GameObject)typeof(Mavis.SahurLoadoutUI).GetField("weaponChoices",flags).GetValue(ui);Check(choices.activeSelf,"Equipment opens directly on weapon choices");
 var bladesText=(UnityEngine.UI.Text)typeof(Mavis.SahurLoadoutUI).GetField("bladesButtonText",flags).GetValue(ui);
 Check(bladesText.text.Contains("LOCKED"),"Weapon menu shows Capri unlock requirement");
 Press(UnityEngine.InputSystem.Key.Digit2,ui);Check(!weapons.UsesTwinBlades,"2 cannot equip an uncollected blade");
 ui.SetOpen(false);
 // This evaluation runs in one frame; simulate the input guard expiring.
 typeof(Mavis.SahurLoadoutUI).GetField("consumedFrame",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,-1);
 var boss=UnityEngine.Object.FindFirstObjectByType<Mavis.CappuccinoEnemy>();var loot=boss.GetComponent<Mavis.CappuccinoWeaponLoot>();
 Check(loot.Drop()==null&&!loot.HasDropped,"Living Capri cannot drop the weapon");
 var health=boss.GetComponent<Mavis.Health>();health.ApplyDamage(health.currentHealth+100,boss.transform.position);
 var pickup=UnityEngine.Object.FindObjectsByType<Mavis.EquipmentPickup>(FindObjectsSortMode.None).FirstOrDefault(x=>x.Item!=null&&x.Item.name=="CapriTwinBlades");
 Check(pickup!=null&&loot.HasDropped,"Defeated Capri creates physical twin-blade loot");
 var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=pickup.transform.position-Vector3.up*(cc.center.y-cc.height*.5f)*p.transform.lossyScale.y;cc.enabled=true;Physics.SyncTransforms();
 Check(pickup.TryCollect(inventory)&&weapons.HasTwinBlades,"Picking up Capri loot unlocks blades");
 Press(UnityEngine.InputSystem.Key.Digit2,weapons);Check(weapons.UsesTwinBlades&&attack.UsesTwinBlades,"Gameplay 2 equips twin blades and attack mode");
 Press(UnityEngine.InputSystem.Key.Digit1,weapons);Check(!weapons.UsesTwinBlades&&!attack.UsesTwinBlades,"Gameplay 1 restores stick and attack mode");
 Press(UnityEngine.InputSystem.Key.I,ui);Press(UnityEngine.InputSystem.Key.Digit2,ui);Check(weapons.UsesTwinBlades,"Weapon-page 2 equips blades while menu pauses time");
 Press(UnityEngine.InputSystem.Key.Digit1,ui);Check(!weapons.UsesTwinBlades,"Weapon-page 1 restores stick");
 var stickText=(UnityEngine.UI.Text)typeof(Mavis.SahurLoadoutUI).GetField("stickButtonText",flags).GetValue(ui);
 var buttons=choices.GetComponentsInChildren<UnityEngine.UI.Button>();buttons.First(b=>b.GetComponentInChildren<UnityEngine.UI.Text>()==bladesText).onClick.Invoke();Check(weapons.UsesTwinBlades,"Blade equipment button also works");
 buttons.First(b=>b.GetComponentInChildren<UnityEngine.UI.Text>()==stickText).onClick.Invoke();Check(!weapons.UsesTwinBlades,"Stick equipment button also works");
 ui.SelectSkillForAssignment(0);Press(UnityEngine.InputSystem.Key.Digit3,ui);Check(ui.SelectedSkill==2,"Other pages preserve skill-slot keys");
 Check(inventory.Items.Count==1,"One real pickup adds one inventory item");
 System.IO.File.WriteAllLines("ProjectRepairBackups/WeaponSwitch-20261008/verification.txt",checks);return checks;
}finally{if(Mavis.SahurLoadoutUI.IsOpen)ui.SetOpen(false);UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);Time.timeScale=1;}
