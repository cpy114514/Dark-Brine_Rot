if(!Application.isPlaying)throw new System.Exception("Play Mode required");
Time.timeScale=0;
var notes=new System.Collections.Generic.List<string>();
void Check(bool ok,string text){if(!ok)throw new System.Exception(text);notes.Add("PASS "+text);}
foreach(var script in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
    if(script.GetType().Name=="FirstIslandArrival"){script.StopAllCoroutines();script.enabled=false;}
var player=UnityEngine.Object.FindFirstObjectByType<Mavis.PlayerHealth>();
var move=player.GetComponent<ThirdPersonPlayerController>();var attack=player.GetComponent<Mavis.SahurAttack>();
var weapons=player.GetComponent<Mavis.SahurWeaponLoadout>();var animator=attack.animator;
move.enabled=true;move.ExternalControlLock=false;move.ExternalMovementLock=false;attack.enabled=false;Cursor.lockState=CursorLockMode.None;
Check(!weapons.HasTwinBlades,"Fresh game still requires Capri loot");
// Only this isolated Play Mode review grants the weapon to sample its poses.
player.GetComponent<Mavis.EquipmentInventory>().Collect(Resources.Load<Mavis.EquipmentItem>("Equipment/Capri/CapriTwinBlades"));
Check(weapons.TryEquip(Mavis.SahurWeaponLoadout.Weapon.CapriTwinBlades),"Equip blades for pose comparison");
var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Lowered arms verification floor";
floor.transform.position=new Vector3(0,499.5f,0);floor.transform.localScale=new Vector3(100,1,100);
var body=player.GetComponent<CharacterController>();float offset=body.bounds.min.y-player.transform.position.y;
player.transform.position=new Vector3(0,500-offset,0);player.transform.rotation=Quaternion.identity;Physics.SyncTransforms();
move.ExternalMovementLock=true;
var locomotion=player.GetComponent<Mavis.SahurTwinBladeLocomotion>();
var pose=player.GetComponent<Mavis.SahurTwinBladeReadyPose>();
var arms=new[]{HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm};
foreach(float speed in new[]{0f,move.moveSpeed})
{
    animator.SetFloat("Speed",speed);locomotion.Refresh();animator.Play("Locomotion",0,.35f);animator.Update(0);
    var rotations=arms.Select(b=>animator.GetBoneTransform(b).localRotation).ToArray();
    var positions=arms.Select(b=>animator.GetBoneTransform(b).localPosition).ToArray();
    pose.Apply(.2f);
    for(int i=0;i<arms.Length;i++)Check(Quaternion.Angle(rotations[i],animator.GetBoneTransform(arms[i]).localRotation)<.01f &&
        Vector3.Distance(positions[i],animator.GetBoneTransform(arms[i]).localPosition)<.00001f,
        arms[i]+" keeps authored lowered pose at speed "+speed);
    foreach(float side in new[]{-1f,1f})
    {
        var hand=animator.GetBoneTransform(side<0?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
        float elevation=Mathf.Asin(Vector3.Dot(hand.TransformDirection(Mavis.SahurWeaponLoadout.GripShaft(side)).normalized,player.transform.up))*Mathf.Rad2Deg;
        Check(Mathf.Abs(elevation-35)<.05f,(side<0?"Left":"Right")+" wrist sets blade elevation to "+elevation.ToString("F2")+" degrees");
    }
}
var bones=Enumerable.Range(0,(int)HumanBodyBones.LastBone).Select(i=>(HumanBodyBones)i).Where(b=>animator.GetBoneTransform(b)).ToArray();
foreach(float speed in new[]{move.moveSpeed+.3f,move.moveSpeed*move.sprintMultiplier})
{
    Check(weapons.TryEquip(Mavis.SahurWeaponLoadout.Weapon.Stick),"Switch to stick for running baseline");
    animator.SetFloat("Speed",speed);locomotion.Refresh();animator.Play("Locomotion",0,.35f);animator.Update(0);
    var rotations=bones.Select(b=>animator.GetBoneTransform(b).localRotation).ToArray();
    var positions=bones.Select(b=>animator.GetBoneTransform(b).localPosition).ToArray();
    var originalController=animator.runtimeAnimatorController;
    var clips=animator.GetCurrentAnimatorClipInfo(0).Select(x=>x.clip).ToArray();
    Check(weapons.TryEquip(Mavis.SahurWeaponLoadout.Weapon.CapriTwinBlades),"Switch to blades at running speed");
    animator.SetFloat("Speed",speed);locomotion.Refresh();animator.Play("Locomotion",0,.35f);animator.Update(0);pose.Apply(.2f);
    Check(animator.runtimeAnimatorController==originalController,"Running uses original stick controller at speed "+speed);
    Check(animator.GetCurrentAnimatorClipInfo(0).Select(x=>x.clip).SequenceEqual(clips),"Running uses identical original clip blend");
    float maxAngle=0,maxPosition=0;
    for(int i=0;i<bones.Length;i++)
    {
        maxAngle=Mathf.Max(maxAngle,Quaternion.Angle(rotations[i],animator.GetBoneTransform(bones[i]).localRotation));
        maxPosition=Mathf.Max(maxPosition,Vector3.Distance(positions[i],animator.GetBoneTransform(bones[i]).localPosition));
    }
    Check(maxAngle<.05f && maxPosition<.0001f,"All "+bones.Length+" humanoid bones match the stick run; max angle="+maxAngle+", max position="+maxPosition);
}
animator.SetFloat("Speed",0);locomotion.Refresh();animator.Play("Locomotion",0,.35f);animator.Update(0);pose.Apply(.2f);
var camera=Camera.main;camera.transform.position=new Vector3(3,503,7.5f);camera.transform.LookAt(new Vector3(0,502.7f,0));
foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
    if(canvas.name.Contains("Arrival")||canvas.sortingOrder>200)canvas.gameObject.SetActive(false);
System.IO.File.WriteAllLines("ProjectRepairBackups/LoweredBladeArms-20261007/verification.txt",notes);return notes;
