using System.Collections.Generic;
using UnityEngine;

/// <summary>Keeps loose wreckage outside actual landing decks, without pinning their buoyancy.</summary>
[DefaultExecutionOrder(16000),DisallowMultipleComponent]
public sealed class Story1WreckDeckClearance : MonoBehaviour
{
    [Min(.1f)] public float clearance=1.25f;
    sealed class Fragment
    {
        public Story1WreckMeshCollision collision;
        public Rigidbody body;
        public Transform transform;
        public Mesh mesh;
        public Bounds local;
    }
    readonly List<Fragment> fragments=new List<Fragment>();
    readonly HashSet<Story1WreckMeshCollision> known=new HashSet<Story1WreckMeshCollision>();
    readonly List<Story1WreckPlank> decks=new List<Story1WreckPlank>();
    readonly List<Bounds> occupied=new List<Bounds>();
    Renderer hero,shark;
    IList<Story1WreckPlank> liveBoards;
    IList<Story1WreckDebris> liveDetails;
    int boardCount,detailCount;
    bool enforcing;
    public int SeparationCount { get; private set; }
    public int ReservedDecks => decks.Count;
    static readonly Unity.Profiling.ProfilerMarker Marker=new Unity.Profiling.ProfilerMarker("Wreck.DeckClearance");

    public void SetActors(Renderer player,Renderer animal){hero=player;shark=animal;}
    public void Track(IList<Story1WreckPlank> boards,IList<Story1WreckDebris> details)
    {liveBoards=boards;liveDetails=details;RefreshFragments();}
    void RefreshFragments()
    {
        if(liveBoards!=null)while(boardCount<liveBoards.Count)Register(liveBoards[boardCount++].Collision);
        if(liveDetails!=null)while(detailCount<liveDetails.Count)Register(liveDetails[detailCount++].Collision);
    }
    public void Register(Story1WreckMeshCollision collision)
    {
        if(collision==null||!known.Add(collision))return;
        var filter=collision.GetComponent<MeshFilter>();
        if(filter==null||filter.sharedMesh==null)return;
        fragments.Add(new Fragment{collision=collision,body=collision.GetComponent<Rigidbody>(),transform=collision.transform,
            mesh=filter.sharedMesh,local=filter.sharedMesh.bounds});
    }
    public void Reserve(Story1WreckPlank deck)
    {
        if(deck==null||deck.IsBroken)return;
        if(decks.Count==1&&decks[0]==deck)return;
        decks.Clear();decks.Add(deck);
        Register(deck.Collision);
        Enforce();
    }
    static Bounds BoundsOf(Fragment f)
    {
        // Physics pose, not the one-frame-delayed interpolated render pose.
        // Native angular integration can return a quaternion a few ulps
        // outside TRS's unit-length tolerance. Normalize the query copy;
        // do not rewrite the actual body's orientation or momentum.
        var matrix=f.body!=null?Matrix4x4.TRS(f.body.position,f.body.rotation.normalized,f.transform.lossyScale):f.transform.localToWorldMatrix;
        var e=f.local.extents;
        var x=matrix.MultiplyVector(Vector3.right*e.x);var y=matrix.MultiplyVector(Vector3.up*e.y);var z=matrix.MultiplyVector(Vector3.forward*e.z);
        return new Bounds(matrix.MultiplyPoint3x4(f.local.center),new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z))*2);
    }
    static bool OverlapsXZ(Bounds a,Bounds b,float margin)
        =>a.min.x<b.max.x+margin&&a.max.x>b.min.x-margin&&a.min.z<b.max.z+margin&&a.max.z>b.min.z-margin;
    static Vector3 ExitXZ(Bounds loose,Bounds deck,float margin)
    {
        float right=deck.max.x+margin-loose.min.x,left=deck.min.x-margin-loose.max.x;
        float front=deck.max.z+margin-loose.min.z,back=deck.min.z-margin-loose.max.z;
        float x=Mathf.Abs(left)<Mathf.Abs(right)?left:right,z=Mathf.Abs(back)<Mathf.Abs(front)?back:front;
        return Mathf.Abs(x)<Mathf.Abs(z)?Vector3.right*x:Vector3.forward*z;
    }
    bool Reserved(Story1WreckMeshCollision collision)
    {
        for(int i=0;i<decks.Count;i++)if(decks[i]!=null&&!decks[i].IsBroken&&decks[i].Collision==collision)return true;
        return false;
    }
    void FixedUpdate()=>Enforce();
    void LateUpdate(){if(Time.deltaTime>0&&!PauseSettingsMenu.IsOpen)Enforce();}
    public void Enforce()
    {
        RefreshFragments();
        if(enforcing||decks.Count==0)return;
        using var measured=Marker.Auto();
        enforcing=true;
        try
        {
            occupied.Clear();
            for(int i=decks.Count-1;i>=0;i--)
            {
                var d=decks[i];
                if(d==null||d.IsBroken||!d.gameObject.activeInHierarchy){decks.RemoveAt(i);continue;}
                for(int j=0;j<fragments.Count;j++)if(fragments[j].collision==d.Collision){occupied.Add(BoundsOf(fragments[j]));break;}
            }
            int deckCount=occupied.Count;if(deckCount==0)return;
            // An outward correction must not put a loose beam into either
            // actor after the opening's exact posed-contact pass has run.
            if(hero!=null&&hero.enabled&&hero.gameObject.activeInHierarchy)occupied.Add(hero.bounds);
            if(shark!=null&&shark.enabled&&shark.gameObject.activeInHierarchy)occupied.Add(shark.bounds);
            for(int i=0;i<fragments.Count;i++)
            {
                var f=fragments[i];if(f.collision==null||!f.collision.Active||Reserved(f.collision))continue;
                Bounds bounds=BoundsOf(f);bool near=false;
                for(int j=0;j<deckCount;j++)if(OverlapsXZ(bounds,occupied[j],clearance)){near=true;break;}
                if(!near)continue;
                Vector3 offset=Vector3.zero;
                for(int pass=0;pass<16;pass++)
                {
                    bool clear=true;
                    for(int j=0;j<occupied.Count;j++)
                    {
                        if(!OverlapsXZ(bounds,occupied[j],clearance))continue;
                        var push=ExitXZ(bounds,occupied[j],clearance+.02f);bounds.center+=push;offset+=push;clear=false;
                    }
                    if(clear)break;
                    if(pass==15)
                    {
                        var union=occupied[0];for(int j=1;j<occupied.Count;j++)union.Encapsulate(occupied[j]);
                        var push=ExitXZ(bounds,union,clearance+.02f);offset+=push;bounds.center+=push;
                    }
                }
                if(offset.sqrMagnitude<.000001f)continue;
                Vector3 direction=offset.normalized;
                if(f.body!=null&&!f.body.isKinematic)
                {
                    f.body.position+=offset;f.transform.position=f.body.position;
                    // Preserve falling speed/spin; remove only momentum back
                    // into the newly cleared deck footprint.
                    float inward=Vector3.Dot(f.body.linearVelocity,direction);
                    if(inward<.75f)f.body.linearVelocity+=direction*(.75f-inward);
                    f.body.WakeUp();
                }
                else f.transform.position+=offset;
                SeparationCount++;
            }
        }
        finally{enforcing=false;}
    }
}
