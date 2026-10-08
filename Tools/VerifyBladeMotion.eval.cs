if(!Application.isPlaying)throw new System.Exception("Play Mode required");
var player=UnityEngine.Object.FindFirstObjectByType<Mavis.PlayerHealth>();var weapons=player.GetComponent<Mavis.SahurWeaponLoadout>();
var attack=player.GetComponent<Mavis.SahurAttack>();var motion=player.GetComponent<Mavis.SahurTwinBladeMotion>();var movement=player.GetComponent<ThirdPersonPlayerController>();var body=player.GetComponent<CharacterController>();
var ui=player.GetComponent<Mavis.SahurLoadoutUI>();if(ui.PanelVisible)ui.SetOpen(false);
foreach(var script in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(script.GetType().Name=="FirstIslandArrival"){script.StopAllCoroutines();script.enabled=false;}
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var notes=new System.Collections.Generic.List<string>();
void Check(bool pass,string message){if(!pass)throw new System.Exception(message);notes.Add("PASS "+message);}
var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Blade motion check floor";floor.transform.position=new Vector3(0,499.5f,0);floor.transform.localScale=new Vector3(100,1,100);
float offset=Mavis.NailongSize.Feet(player.transform).y-player.transform.position.y;player.transform.position=new Vector3(0,500-offset,0);player.transform.rotation=Quaternion.identity;movement.ExternalControlLock=false;movement.ExternalMovementLock=false;Physics.SyncTransforms();body.Move(Vector3.down*.1f);
Time.timeScale=1;UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
    if(!Application.isPlaying){UnityEditor.EditorApplication.update-=tick;return;}
    if(Time.deltaTime<=0||Mavis.SahurLoadoutUI.BlocksInput)return;
    try
    {
        Check(weapons.UsesTwinBlades,"Motion check uses collected blades");
        Check(!player.GetComponent<Mavis.SahurBoomerang>().TryThrow(),"Wooden-stick throw cannot activate with blades selected");
        var stage=typeof(Mavis.SahurAttack).GetMethod("StartComboStage",flags);var update=typeof(Mavis.SahurTwinBladeMotion).GetMethod("LateUpdate",flags);
        typeof(Mavis.SahurAttack).GetMethod("CancelForMovement",flags).Invoke(attack,null);update.Invoke(motion,null);player.transform.rotation=Quaternion.identity;
        stage.Invoke(attack,new object[]{2});attack.animator.Play("Capri Combo 3",0,.5f);attack.animator.Update(0);
        Quaternion before=player.transform.rotation;update.Invoke(motion,null);
        Check(Quaternion.Angle(before,player.transform.rotation)>170,"Third blade combo rotates the player through the cyclone: "+Quaternion.Angle(before,player.transform.rotation).ToString("F1")+" degrees");
        typeof(Mavis.SahurAttack).GetMethod("CancelForMovement",flags).Invoke(attack,null);update.Invoke(motion,null);
        Check(Quaternion.Angle(before,player.transform.rotation)<.1f,"Cancelled cyclone restores its facing");
        player.transform.rotation=Quaternion.identity;Vector3 start=player.transform.position;
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Blade rush wall";wall.transform.position=new Vector3(start.x,502,start.z+2.5f);wall.transform.localScale=new Vector3(10,5,.5f);Physics.SyncTransforms();body.Move(Vector3.down*.1f);
        typeof(Mavis.SahurAttack).GetMethod("PlayAttack",flags).Invoke(attack,new object[]{true,attack.damage*1.5f,0f});attack.animator.Play("Capri Heavy",0,.58f);attack.animator.Update(0);update.Invoke(motion,null);
        float distance=Vector3.Dot(player.transform.position-start,Vector3.forward);
        Check(distance>.1f&&distance<2.25f,"Blade heavy rush advances and is stopped by a solid wall: "+distance.ToString("F2")+" m");
        Check(weapons.TryEquip(Mavis.SahurWeaponLoadout.Weapon.Stick),"Weapon switch can interrupt an active blade strike");
        Check(!attack.UsesTwinBlades&&!attack.IsCombatMotionActive&&attack.comboReach==4.8f&&attack.chargeClip!=Resources.Load<AnimationClip>("Equipment/Capri/CapriSlash"),"Switch back restores original wooden-stick tuning and charge animation");
        UnityEngine.Object.Destroy(wall);UnityEngine.Object.Destroy(floor);
        notes.Add("COMPLETE");
    }
    catch(System.Exception error){notes.Add("FAIL "+error);}
    finally
    {
        Time.timeScale=0;UnityEditor.EditorApplication.update-=tick;
        System.IO.File.WriteAllLines("ProjectRepairBackups/CapriWeapons-20261007/motion-verification.txt",notes);
    }
};
UnityEditor.EditorApplication.update+=tick;UnityEditor.EditorApplication.QueuePlayerLoopUpdate();return "Queued one-frame physical motion checks";
