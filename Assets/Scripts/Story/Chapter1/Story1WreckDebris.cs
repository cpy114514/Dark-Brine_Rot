using UnityEngine;

/// <summary>Small real ship fragments use gravity, solid contacts and damped water forces.</summary>
[RequireComponent(typeof(Story1WreckMeshCollision),typeof(Story1WreckFloatBody))]
public sealed class Story1WreckDebris : MonoBehaviour
{
    OceanWorld ocean;
    Story1WreckFloatBody floating;
    bool floats;
    public Story1WreckMeshCollision Collision { get; private set; }
    public MeshCollider Obstacle => Collision!=null ? Collision.Primary : null;
    public Story1ShipWreckAsset.Fragment FractureSource { get; private set; }
    public bool IsBroken { get; private set; }
    public float Integrity { get; private set; }=100;
    public Vector3 PointVelocity(Vector3 point)=>floating.Body.GetPointVelocity(point);
    public void ConfigureObstacle(Story1ShipWreckAsset.Fragment source)
    {
        if(source.splinters==null || source.splinters.Length<2)return;
        FractureSource=source;Collision.Initialize(source.mesh,source.splinters);
    }
    public bool Damage(float amount)
    {
        if(IsBroken || FractureSource?.splinters==null || FractureSource.splinters.Length<2)return false;
        Integrity=Mathf.Max(0,Integrity-amount);return Integrity<=0;
    }
    public void MarkBroken()
    {
        IsBroken=true;Collision.SetEnabled(false);floating.enabled=false;
        floating.Body.isKinematic=true;gameObject.SetActive(false);
    }
    public void Initialize(OceanWorld water,Vector3 size,bool buoyant,Vector3 launch,Vector3 angularDegrees)
    {
        ocean=water;floats=buoyant;
        Collision=GetComponent<Story1WreckMeshCollision>();Collision.Initialize(GetComponent<MeshFilter>().sharedMesh);
        floating=GetComponent<Story1WreckFloatBody>();floating.Initialize(water,size,buoyant,launch,angularDegrees);
    }
    void Update()
    {
        if(floating==null || floats || ocean==null || PauseSettingsMenu.IsOpen)return;
        if(transform.position.y>=ocean.SampleSurfaceHeight(transform.position,Time.time)-10)return;
        GetComponent<Renderer>().enabled=false;Collision.SetEnabled(false);
        floating.enabled=false;floating.Body.isKinematic=true;enabled=false;
    }
}
