using System;
using System.Linq;
using System.IO;
using UnityEngine;

/// <summary>Repeat one recorded physical fracture; retain live buoyancy after release.</summary>
[DefaultExecutionOrder(-250)]
public sealed class Story1WreckScatterPlayback:MonoBehaviour
{
    [Serializable] public struct Pose{public Vector3 position;public Quaternion rotation;}
    [Serializable] public sealed class Piece{public string name;public Pose[] poses;public Vector3 velocity,spin;}
    [Serializable] public sealed class Layout{public int version;public float step;public string entryDeck;public Piece[] pieces;}
    public bool IsPlaying { get; private set; }
    public string EntryDeckName { get; private set; }
    public float Age { get; private set; }
    public Vector3[] ReleasePositions { get; private set; }
    public float MaximumReleasePoseError { get; private set; }
    Layout layout;Story1WreckFloatBody[] fragments;Rigidbody[] bodies;
    Vector3 origin;Quaternion heading;
    Vector3[] initialPositions;Quaternion[] initialRotations;
    bool releasePending;
    public void Initialize(TextAsset asset,Story1WreckFloatBody[] released,Transform ship)
    {
        using(var stream=new MemoryStream(asset.bytes,false))using(var reader=new BinaryReader(stream))
        {
            if(reader.ReadInt32()!=0x42525732)throw new InvalidDataException("Unknown wreck bake");
            layout=new Layout{version=1,step=reader.ReadSingle()};int count=reader.ReadInt32(),frames=reader.ReadInt32();
            if(count!=released.Length || frames<2 || frames>1000)throw new InvalidDataException("Wreck bake dimensions changed");
            layout.entryDeck=reader.ReadString();layout.pieces=new Piece[count];
            for(int i=0;i<count;i++)layout.pieces[i]=new Piece{name=reader.ReadString(),poses=new Pose[frames]};
            for(int frame=0;frame<frames;frame++)for(int i=0;i<count;i++)
                layout.pieces[i].poses[frame]=new Pose{position=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()),rotation=new Quaternion(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle())};
            foreach(var piece in layout.pieces)
            {piece.velocity=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());piece.spin=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());}
        }
        if(layout==null || layout.version!=1 || layout.step<=0 || layout.pieces==null || layout.pieces.Length!=released.Length)
            throw new InvalidOperationException("Fixed wreck layout does not match original released fragments");
        var byName=released.ToDictionary(p=>p.name,StringComparer.Ordinal);
        fragments=layout.pieces.Select(p=>byName[p.name]).ToArray();bodies=fragments.Select(p=>p.Body).ToArray();
        origin=ship.position;var ocean=FindFirstObjectByType<OceanWorld>();origin.y=ocean!=null?ocean.oceanHeight:0;
        heading=Quaternion.LookRotation(Vector3.ProjectOnPlane(ship.forward,Vector3.up));EntryDeckName=layout.entryDeck;
        initialPositions=bodies.Select(b=>b.position).ToArray();initialRotations=bodies.Select(b=>b.rotation).ToArray();
        foreach(var fragment in fragments){fragment.enabled=false;fragment.Body.isKinematic=true;}
        IsPlaying=true;Apply(0);
    }
    void FixedUpdate()
    {
        if(!IsPlaying || Time.timeScale<=0)return;
        // MovePosition's final kinematic target is applied by the following
        // physics simulation. Switching mode in the same FixedUpdate drops
        // that target and releases different preceding samples at low FPS.
        if(releasePending){Release();return;}
        Age+=Time.fixedDeltaTime;Apply(Age);
        if(Age>=(layout.pieces[0].poses.Length-1)*layout.step)releasePending=true;
    }
    void Apply(float time)
    {
        float frame=time/layout.step;int first=Mathf.FloorToInt(frame);float blend=frame-first;
        for(int i=0;i<bodies.Length;i++)
        {
            if(!bodies[i].gameObject.activeInHierarchy)continue;
            var poses=layout.pieces[i].poses;var a=poses[Mathf.Min(first,poses.Length-1)];var b=poses[Mathf.Min(first+1,poses.Length-1)];
            float release=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/.15f));
            bodies[i].MovePosition(Vector3.Lerp(initialPositions[i],origin+heading*Vector3.Lerp(a.position,b.position,blend),release));
            bodies[i].MoveRotation(Quaternion.Slerp(initialRotations[i],heading*Quaternion.Slerp(a.rotation,b.rotation,blend),release));
        }
    }
    void Release()
    {
        IsPlaying=false;
        ReleasePositions=bodies.Select(b=>b.position).ToArray();
        for(int i=0;i<bodies.Length;i++)
        {
            if(!bodies[i].gameObject.activeInHierarchy)continue;
            var piece=layout.pieces[i];
            MaximumReleasePoseError=Mathf.Max(MaximumReleasePoseError,Vector3.Distance(bodies[i].position,origin+heading*piece.poses[piece.poses.Length-1].position));
            bodies[i].isKinematic=false;fragments[i].enabled=true;
            // Repeat the fracture, not a permanent horizontal constraint.
            // Frozen overlapping hulls cannot separate as they roll in waves;
            // their contact solver then ejects a thin deck vertically.
            bodies[i].linearVelocity=heading*piece.velocity;
            bodies[i].angularVelocity=heading*piece.spin;
        }
    }
}
