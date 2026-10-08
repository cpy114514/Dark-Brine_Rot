using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Mavis;

public static class VerifySharkBoardContact
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static double ProjectedArea(Mesh mesh,Vector3 scale)
    {
        var v=mesh.vertices;var t=mesh.triangles;double area=0;
        for(int i=0;i<t.Length;i+=3)
        {
            Vector3 a=Vector3.Scale(v[t[i+1]]-v[t[i]],scale),b=Vector3.Scale(v[t[i+2]]-v[t[i]],scale);
            area+=Math.Abs(a.x*b.z-a.z*b.x)*.5;
        }
        return area;
    }
    public static object Geometry()
    {
        var asset=Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
        double error=0;
        foreach(var deck in asset.decks)
        {
            Require(deck.splinters!=null && deck.splinters.Length>=3,"Missing real subfragments.");
            double area=ProjectedArea(deck.mesh,deck.sourceSize),split=deck.splinters.Sum(s=>ProjectedArea(s.mesh,Vector3.Scale(deck.sourceSize,s.localSize)));
            error=Math.Max(error,Math.Abs(area-split)/area);
            foreach(var child in deck.splinters)Require(child.mesh.uv.Length==child.mesh.vertexCount,"Shard UVs missing.");
        }
        Require(error<.01,"Split geometry does not preserve the original deck: "+error);
        return new{deckCount=asset.decks.Length,realSubfragments=asset.decks.Concat(asset.hulls).Sum(p=>p.splinters.Length),maximumProjectedAreaError=error,
            distinctDeckDimensions=asset.decks.Select(p=>p.sourceSize).Distinct().Count()};
    }
    public static async Task<object> Fight()
    {
        Require(Application.isPlaying,"Requires Play Mode.");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        Keyboard keyboard=null;
        try
        {
            GameSaveManager.CancelPendingContinue();var loading=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!loading.isDone)await Task.Delay(30);await Task.Delay(300);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
            var battle=sequence.GetComponent<Story1WreckBattle>();battle.minimumFightSeconds=battle.maximumFightSeconds=120;
            sequence.BeginImpact();while(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)await Task.Delay(25);
            var hero=sequence.sahur;hero.GetComponent<PlayerHealth>().GrantProtection(90);
            var rider=hero.GetComponent<Story1WreckRider>();var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
            var board=battle.Boards.Where(p=>p.IsBoardable && p.Dimensions.y<1.6f).OrderByDescending(p=>p.Dimensions.x*p.Dimensions.z).First();
            // Isolate one actual ship fragment so the attack must contact this board,
            // rather than a neighbouring board shielding it. Its physics stays enabled.
            Vector3 at=hero.transform.position+Vector3.right*250;at.y=ocean.SampleSurfaceHeight(at,Time.time)-.2f;
            var body=board.GetComponent<Rigidbody>();body.position=at;body.rotation=Quaternion.identity;body.linearVelocity=body.angularVelocity=Vector3.zero;
            Physics.SyncTransforms();await Task.Delay(200);
            Vector3 feet=board.BoardingPoint(board.transform.position);
            hero.RestoreSavedPose(feet+Vector3.up*(.06f-hero.LowestFootWorldOffset),Quaternion.identity);Physics.SyncTransforms();
            keyboard=InputSystem.AddDevice<Keyboard>("Shark fracture verification");Cursor.lockState=CursorLockMode.Locked;
            for(int i=0;i<40 && rider.Support()==null;i++)await Task.Delay(25);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));await Task.Delay(100);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(120);
            Require(rider.IsSurfing,"Could not mount the real isolated board: support="+(rider.Support()!=null ? rider.Support().name : "none")+", swimming="+hero.Swimming+", cursor="+Cursor.lockState+", attack="+hero.GetComponent<SahurAttack>().IsCombatMotionActive+", movementLock="+hero.ExternalMovementLock+", controlLock="+hero.ExternalControlLock+", UI="+SahurLoadoutUI.BlocksInput+", hint="+rider.Hint);
            var animator=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();
            Vector3 nose=board.BoardingPoint(board.transform.position+Vector3.forward*30)+Vector3.forward*12;
            nose.y=ocean.SampleSurfaceHeight(nose,Time.time)-1;
            battle.PoseSharkNoseAt(nose,Quaternion.LookRotation(Vector3.back));
            float start=Time.time,maxPenetration=0;int samples=0;bool riderReleased=false;
            var trace=new System.Collections.Generic.List<object>();
            while(Time.time-start<20 && !board.IsBroken)
            {
                await Task.Delay(30);samples++;
                // Async continuations can run before LateUpdate depenetration; measure
                // the committed rendered pose, rather than that intermediate desired pose.
                float penetration=battle.WreckContact.MaximumResidualPenetration;maxPenetration=Mathf.Max(maxPenetration,penetration);
                if(samples%12==0)trace.Add(new{time=Time.time-start,attacks=battle.SharkAttackCount,damage=battle.BoardDamageCount,integrity=board.Integrity,
                    penetration,surf=rider.IsSurfing,nose=animator.NoseWorldPoint.ToString(),feet=(hero.transform.position+Vector3.up*hero.LowestFootWorldOffset).ToString()});
            }
            riderReleased=!rider.IsSurfing && !hero.ExternalMovementLock;
            await Task.Delay(500);
            var fragments=battle.Boards.Where(p=>p.name.StartsWith(board.name+" splinter") && p.gameObject.activeInHierarchy).ToArray();
            var result=new{samples,attacks=battle.SharkAttackCount,damage=battle.BoardDamageCount,broken=battle.BrokenBoardCount,targetBroken=board.IsBroken,
                integrity=board.Integrity,riderReleased,remainingPlayableShards=fragments.Length,gravityPreserved=fragments.All(p=>p.GetComponent<Rigidbody>().useGravity&&!p.GetComponent<Rigidbody>().isKinematic),
                maxPenetration,blocked=battle.WreckContact.BlockedContactCount,trace};
            Directory.CreateDirectory(".codex/shark-board-contact-20261007");File.WriteAllText(".codex/shark-board-contact-20261007/fight.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            Require(board.IsBroken && battle.BoardDamageCount>=2,"Real shark attacks did not break their target: "+Newtonsoft.Json.JsonConvert.SerializeObject(result));
            Require(riderReleased && result.gravityPreserved,"Broken support did not release the rider or preserve shard gravity.");
            Require(maxPenetration<.08f,"Shark still overlaps unbroken timber: "+maxPenetration);
            return result;
        }
        finally
        {
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            GameSaveManager.CancelPendingContinue();if(original!=null)File.WriteAllBytes(save,original);
        }
    }
}
