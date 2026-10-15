using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BakeWreckScatterV210
{
    public static object Start()
    {
        if(!Application.isPlaying)throw new Exception("Bake in original Play Mode");
        var go=new GameObject("Physical wreck release bake v210");UnityEngine.Object.DontDestroyOnLoad(go);go.AddComponent<WreckScatterBakeV210>();
        return new{started=true,physicalSimulation=true,step=.02f};
    }
}
[DefaultExecutionOrder(32700)]
public sealed class WreckScatterBakeV210:MonoBehaviour
{
    readonly List<Story1WreckScatterPlayback.Pose[]> frames=new List<Story1WreckScatterPlayback.Pose[]>();
    Story1SharkCollisionSequence sequence;Story1WreckBattle battle;Story1WreckFloatBody[] pieces;
    Vector3 origin;Quaternion rotation;float warm;bool ready,begun,recording;const string Folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/chase-entry-20261014";
    IEnumerator Start()
    {
        Time.captureFramerate=50;Time.fixedDeltaTime=.02f;GameSaveManager.CancelPendingContinue();
        yield return SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");yield return null;
        sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();battle=sequence.GetComponent<Story1WreckBattle>();battle.DiagnosticScatterSeed=20261013;battle.DiagnosticBakeScatter=true;ready=true;
    }
    void LateUpdate()
    {
        if(!ready)return;
        warm+=Time.deltaTime;
        if(!begun && warm>=2){sequence.BeginImpact();begun=true;}
        if(!recording && battle.InitialFragmentCount>0)
        {
            origin=sequence.ship.transform.position;origin.y=UnityEngine.Object.FindFirstObjectByType<OceanWorld>().oceanHeight;
            rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(sequence.ship.transform.forward,Vector3.up));
            pieces=battle.Boards.Select(p=>p.GetComponent<Story1WreckFloatBody>()).Concat(battle.WreckDetails.Select(p=>p.GetComponent<Story1WreckFloatBody>())).OrderBy(p=>p.name,StringComparer.Ordinal).ToArray();
            if(pieces.Select(p=>p.name).Distinct().Count()!=pieces.Length)throw new Exception("Ambiguous fragment identifiers");
            recording=true;battle.enabled=false;
        }
        if(!recording)return;
        frames.Add(pieces.Select(p=>new Story1WreckScatterPlayback.Pose{position=Quaternion.Inverse(rotation)*(p.Body.position-origin),rotation=Quaternion.Inverse(rotation)*p.Body.rotation}).ToArray());
        if(frames.Count<301)return;
        var entry=battle.Boards.Where(p=>p.IsBoardable && p.Dimensions.y<1.2f && Mathf.Min(p.Dimensions.x,p.Dimensions.z)>3)
            .OrderBy(p=>p.EdgeDistance(sequence.sahur.transform.position)-p.Dimensions.x*p.Dimensions.z*.04f).First();
        using(var stream=File.Create(Folder+"/OpeningScatter_v210.bytes"))using(var writer=new BinaryWriter(stream))
        {
            writer.Write(0x42525732);writer.Write(.02f);writer.Write(pieces.Length);writer.Write(frames.Count);writer.Write(entry.name);
            foreach(var p in pieces)writer.Write(p.name);
            foreach(var frame in frames)foreach(var pose in frame)
            {writer.Write(pose.position.x);writer.Write(pose.position.y);writer.Write(pose.position.z);writer.Write(pose.rotation.x);writer.Write(pose.rotation.y);writer.Write(pose.rotation.z);writer.Write(pose.rotation.w);}
            foreach(var p in pieces)
            {var velocity=Quaternion.Inverse(rotation)*p.Body.linearVelocity;var spin=Quaternion.Inverse(rotation)*p.Body.angularVelocity;writer.Write(velocity.x);writer.Write(velocity.y);writer.Write(velocity.z);writer.Write(spin.x);writer.Write(spin.y);writer.Write(spin.z);}
        }
        File.WriteAllText(Folder+"/scatter-bake.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{physicalSimulation=true,seed=20261013,pieces=pieces.Length,frames=frames.Count,step=.02f,duration=6,entryDeck=entry.name,origin=origin.ToString("F6"),rotation=rotation.ToString("F6"),
            endpoints=pieces.Select(p=>new{p.name,position=p.Body.position.ToString("F6"),rotation=p.Body.rotation.ToString("F6"),size=p.transform.lossyScale.ToString("F6")}).ToArray()},Newtonsoft.Json.Formatting.Indented));
        ready=false;Time.captureFramerate=0;Destroy(gameObject);
    }
    void OnDestroy(){Time.captureFramerate=0;}
}
