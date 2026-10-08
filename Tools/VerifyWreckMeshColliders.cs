using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mavis;

public static class VerifyWreckMeshColliders
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static async Task<object> Verify()
    {
        Require(Application.isPlaying,"Requires Play Mode.");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        GameObject floor=null,falling=null;
        try
        {
            GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!load.isDone)await Task.Delay(30);await Task.Delay(300);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
            var battle=sequence.GetComponent<Story1WreckBattle>();battle.minimumFightSeconds=battle.maximumFightSeconds=120;
            sequence.BeginImpact();
            for(int i=0;i<300 && battle.ShipFragmentCount==0;i++)await Task.Delay(20);
            Require(battle.ShipFragmentCount==202,"Ship did not fracture into its real geometry.");
            var shapes=UnityEngine.Object.FindObjectsByType<Story1WreckMeshCollision>(FindObjectsSortMode.None);
            int colliders=0,compound=0,raycastParts=0;
            foreach(var shape in shapes)
            {
                Require(shape.Active,"Missing solid collider on "+shape.name);
                var body=shape.GetComponent<Rigidbody>();
                Require(body!=null && body.useGravity && !body.isKinematic,"Fragment still bypasses physical collision: "+shape.name);
                Require(body.collisionDetectionMode==CollisionDetectionMode.ContinuousSpeculative,"Missing mesh-compatible continuous collision.");
                if(shape.Parts.Length>1)compound++;
                foreach(var part in shape.Parts)
                {
                    colliders++;
                    Require(!part.isTrigger && part.convex && part.sharedMesh!=null && part.attachedRigidbody==body,"Invalid mesh contact: "+shape.name);
                    var bounds=part.sharedMesh.bounds;Vector3 center=part.transform.TransformPoint(bounds.center);
                    float span=Vector3.Scale(bounds.extents,part.transform.lossyScale).magnitude+2;
                    bool hit=false;
                    foreach(var axis in new[]{Vector3.up,Vector3.right,Vector3.forward})
                        if(part.Raycast(new Ray(center+axis*span,-axis),out var contact,span*2)){hit=true;break;}
                    Require(hit,"Mesh cooked without a hittable shape: "+part.sharedMesh.name);raycastParts++;
                }
            }
            Require(shapes.Length==202,"A visual fragment has no collision body: "+shapes.Length);
            // Test an actual cut deck and real child mesh away from the scripted fight.
            var board=battle.Boards.OrderByDescending(b=>b.Dimensions.x*b.Dimensions.z).First();
            var source=board.FractureSource;
            floor=new GameObject("Real deck mesh collision fixture",typeof(MeshFilter),typeof(MeshRenderer));
            floor.GetComponent<MeshFilter>().sharedMesh=source.mesh;floor.GetComponent<MeshRenderer>().sharedMaterial=source.material;
            var support=floor.AddComponent<Story1WreckPlank>();support.SetFractureSource(source);
            Vector3 at=sequence.sahur.transform.position+Vector3.right*300+Vector3.up*25;
            support.Initialize(at,Quaternion.identity,board.Dimensions,null,Vector3.zero,Vector3.zero);
            var floorBody=floor.GetComponent<Rigidbody>();floor.GetComponent<Story1WreckFloatBody>().enabled=false;floorBody.isKinematic=true;
            var shard=source.splinters[0];
            falling=new GameObject("Real splinter high speed collision fixture",typeof(MeshFilter),typeof(MeshRenderer));
            falling.GetComponent<MeshFilter>().sharedMesh=shard.mesh;falling.GetComponent<MeshRenderer>().sharedMaterial=source.material;
            falling.transform.localScale=new Vector3(2,.4f,2);
            Vector3 top=support.BoardingPoint(at);falling.transform.position=top+Vector3.up*6;
            var detail=falling.AddComponent<Story1WreckDebris>();detail.Initialize(null,falling.transform.localScale,true,Vector3.down*40,Vector3.zero);
            var probe=falling.AddComponent<WreckMeshCollisionProbe>();probe.floor=floor;
            Physics.SyncTransforms();await Task.Delay(1800);
            Require(probe.contacts>0,"Fast real mesh fragment passed through the deck without contact.");
            Require(falling.transform.position.y>top.y-.5f,"Fast real mesh fragment tunnelled below the deck.");
            float penetration=0;
            foreach(var a in detail.Collision.Parts)foreach(var b in support.Collision.Parts)
                if(Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out var normal,out var depth))penetration=Mathf.Max(penetration,depth);
            Require(penetration<.035f,"Mesh contacts failed to settle: "+penetration);
            // Real secondary fragments must use solid meshes too, including the small ones.
            var target=battle.Boards.First(b=>!b.IsBroken);
            int oldCount=battle.Boards.Count+battle.WreckDetails.Count;
            battle.DamageBoard(target,100,target.BoardingPoint(target.transform.position),Vector3.up);
            await Task.Delay(80);
            int added=battle.Boards.Count+battle.WreckDetails.Count-oldCount;
            Require(added>=3,"Secondary fracture did not spawn real pieces.");
            foreach(var fragment in UnityEngine.Object.FindObjectsByType<Story1WreckMeshCollision>(FindObjectsSortMode.None).Where(s=>s.name.StartsWith(target.name+" splinter")))
                Require(fragment.Active && fragment.Parts.All(c=>!c.isTrigger),"Small secondary fragment lost its solid mesh.");
            var result=new{physicalFragments=shapes.Length,solidMeshParts=colliders,compoundFragments=compound,hittableMeshParts=raycastParts,
                highSpeedContacts=probe.contacts,highSpeedMetresPerSecond=40,settledPenetration=penetration,secondaryPieces=added};
            Directory.CreateDirectory(".codex/fragment-colliders-20261007");
            File.WriteAllText(".codex/fragment-colliders-20261007/verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally
        {
            if(floor!=null)UnityEngine.Object.Destroy(floor);if(falling!=null)UnityEngine.Object.Destroy(falling);
            var actor=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(actor!=null && actor.GetComponent<GameSaveExcluded>()==null)actor.gameObject.AddComponent<GameSaveExcluded>();
            GameSaveManager.CancelPendingContinue();if(original!=null)File.WriteAllBytes(save,original);
        }
    }
}
public sealed class WreckMeshCollisionProbe : MonoBehaviour
{
    public GameObject floor;
    public int contacts;
    void OnCollisionEnter(Collision contact){if(contact.collider.transform.IsChildOf(floor.transform))contacts++;}
    void OnCollisionStay(Collision contact){if(contact.collider.transform.IsChildOf(floor.transform))contacts++;}
}
