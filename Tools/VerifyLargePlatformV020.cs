using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
public static class VerifyLargePlatformV020
{
    public static async Task<object> Verify()
    {
        if(!Application.isPlaying)throw new Exception("Native Play required");
        var battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
        var hero=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var rider=hero.GetComponent<Story1WreckRider>();
        var board=battle.Boards.Where(b=>!b.IsBroken && !Story1WreckPlank.SurfboardShape(b.Dimensions))
            .OrderBy(b=>(b.transform.position-hero.transform.position).sqrMagnitude).First();
        var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        var body=board.GetComponent<Rigidbody>();rider.Release();
        // Test-only isolation of an actual released hull mesh. Production
        // never stages timber for choreography or platform selection.
        Vector3 at=board.transform.position+Vector3.right*450+Vector3.forward*180;
        at.y=ocean.SampleSurfaceHeight(at,Time.time)-.15f;
        body.position=at;body.rotation=Quaternion.identity;body.linearVelocity=body.angularVelocity=Vector3.zero;
        await Task.Delay(1800);Physics.SyncTransforms();board.CommitPose();
        // Native updates can automatically remount the old small board during
        // settling. Release that claim immediately before relocating the fixture.
        rider.Release();
        hero.RestoreSavedPose(board.BoardingPoint(at)+Vector3.up*(.05f-hero.LowestFootWorldOffset),Quaternion.identity);
        rider.Initialize(hero,battle.Boards,ocean,board,battle.WreckDetails);
        await Task.Delay(500);
        board.Drive(1,1,hero.transform);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        float throttle=(float)typeof(Story1WreckPlank).GetField("throttle",flags).GetValue(board);
        float steer=(float)typeof(Story1WreckPlank).GetField("steer",flags).GetValue(board);
        var occupant=(Transform)typeof(Story1WreckPlank).GetField("rider",flags).GetValue(board);
        if(throttle!=0 || steer!=0 || occupant!=null || rider.IsSurfing || hero.ExternalMovementLock)
            throw new Exception("Large platform isolation/control failure: "+board.name+" throttle="+throttle+" steer="+steer+" occupant="+(occupant!=null)+" mounted="+rider.DrivenBoard?.name+" climbing="+rider.IsClimbing);
        var result=new{nativeReleasedMesh=board.name,dimensions=board.Dimensions.ToString(),largePlatformCannotDrive=true,
            automaticMountRejected=true,normalMovementOwnerRetained=true,throttle,steer};
        Directory.CreateDirectory(".codex/encounter-v020");
        File.WriteAllText(".codex/encounter-v020/large-platform-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
