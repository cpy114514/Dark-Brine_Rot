using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public static class TestOpeningSnapshotsV225
{
 struct RedWaterCapsule {public Vector3 a,b;public float radius;}
 struct RedWaterShape {public Story1WreckMeshCollision mesh;public Rigidbody body;public Bounds bounds;public Vector3 velocity,spin,centre;}
 static readonly List<RedWaterShape> redWaterShapes=new List<RedWaterShape>();static CapsuleCollider redWaterQuery;
 static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 public static object Run()
 {
  var battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();var type=typeof(Story1WreckBattle);
  object Field(string n)=>type.GetField(n,Private).GetValue(battle);object Call(string n,params object[] args)=>type.GetMethod(n,Private).Invoke(battle,args);
  Call("RedWaterCollectShapes");Call("RedTutorialBody");redWaterQuery=(CapsuleCollider)Field("redWaterQuery");
  var runtimeBody=(IList)Field("redWaterCapsules");var body=new List<RedWaterCapsule>();
  foreach(var c in runtimeBody){var t=c.GetType();body.Add(new RedWaterCapsule{a=(Vector3)t.GetField("a").GetValue(c),b=(Vector3)t.GetField("b").GetValue(c),radius=(float)t.GetField("radius").GetValue(c)});}
  void Snapshot()
  {
   redWaterShapes.Clear();foreach(var value in (IList)Field("redWaterShapes")){var t=value.GetType();object Get(string n)=>t.GetField(n).GetValue(value);redWaterShapes.Add(new RedWaterShape{mesh=(Story1WreckMeshCollision)Get("mesh"),body=(Rigidbody)Get("body"),bounds=(Bounds)Get("bounds"),velocity=(Vector3)Get("velocity"),spin=(Vector3)Get("spin"),centre=(Vector3)Get("centre")});}
  }
  Snapshot();var rows=new List<object>();float error=0,pushError=0;double originalMs=0,cachedMs=0;var method=type.GetMethod("RedWaterBodyCost",Private);
  float[] times={0,.7f,1.3f,1.65f,2.35f,3.45f};
  for(int i=0;i<48;i++)
  {
   if(i==24){Call("RedWaterCollectShapes");Snapshot();}
   Vector3 offset=new Vector3((i%5-2)*1.2f,0,(i%7-3)*1.1f);float seconds=times[i%6],margin=i%2==0?.1f:.65f;
   var watch=System.Diagnostics.Stopwatch.StartNew();float expected=RedWaterBodyCost(body,offset,seconds,margin,out var push);originalMs+=watch.Elapsed.TotalMilliseconds;
   object[] args={runtimeBody,offset,seconds,margin,Vector3.zero};watch.Restart();float actual=(float)method.Invoke(battle,args);cachedMs+=watch.Elapsed.TotalMilliseconds;
   error=Mathf.Max(error,Mathf.Abs(expected-actual));pushError=Mathf.Max(pushError,Vector3.Distance(push,(Vector3)args[4]));
   if(error>.0001f||pushError>.001f)throw new Exception("Water snapshot changes original loop "+i+": "+error+" / "+pushError);
   rows.Add(new{i,expected,actual});
  }
  var shape=redWaterShapes[0].mesh;var box=shape.WorldBounds;float rayError=0;int rayChecks=0;
  var snapshot=typeof(Story1WreckMeshCollision).GetMethod("ReadRaycastSnapshot",Private).Invoke(shape,null);var rayMethod=snapshot.GetType().GetMethod("Raycast");
  for(int i=0;i<180;i++)
  {
   var origin=box.center+new Vector3((i%15-7)/6f*box.extents.x,box.extents.y+10,(i/15-5.5f)/5f*box.extents.z);var ray=new Ray(origin,Vector3.down);float reach=box.size.y+25;
   bool expected=LegacyRay(shape,ray,out var old,reach);object[] args={ray,default(RaycastHit),reach};bool actual=(bool)rayMethod.Invoke(snapshot,args);var hit=(RaycastHit)args[1];
   if(expected!=actual)throw new Exception("Support batch changes native ray admission "+i);
   if(actual){rayError=Mathf.Max(rayError,Vector3.Distance(old.point,hit.point));if(rayError>.0001f)throw new Exception("Support batch changes native hit point");}rayChecks++;
  }
  var result=new{passed=true,forecastChecks=rows.Count,maximumCostError=error,maximumPushError=pushError,originalMs,cachedMs,rayChecks,maximumRayPointError=rayError,rows};
  File.WriteAllText(".codex/animation-performance-20261019/snapshot-tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
 static bool LegacyRay(Story1WreckMeshCollision shape,Ray ray,out RaycastHit nearest,float reach)
 {
  var body=shape.GetComponent<Rigidbody>();Matrix4x4 toPhysics=Matrix4x4.identity,toVisual=Matrix4x4.identity;
  if(body&&body.interpolation!=RigidbodyInterpolation.None){var physics=Matrix4x4.TRS(body.position,body.rotation,shape.transform.lossyScale);toPhysics=physics*shape.transform.worldToLocalMatrix;toVisual=shape.transform.localToWorldMatrix*physics.inverse;}
  var query=new Ray(toPhysics.MultiplyPoint3x4(ray.origin),toPhysics.MultiplyVector(ray.direction));nearest=default;bool found=false;
  for(int i=0;i<shape.Parts.Length;i++)
  {
   var part=shape.Parts[i];var bounds=shape.PartBounds(i);bounds.Expand(.04f);if(!bounds.IntersectRay(ray,out var entry)||entry>reach)continue;
   if(part.enabled&&part.Raycast(query,out var hit,reach)){hit.point=toVisual.MultiplyPoint3x4(hit.point);hit.normal=toVisual.MultiplyVector(hit.normal).normalized;nearest=hit;reach=hit.distance;found=true;}
  }
  return found;
 }
    static Vector3 RedWaterForecastTranslation(RedWaterShape shape,float seconds)
    {
        // Water damps vertical velocity quickly. Preserve measured horizontal
        // drift and retain the initial vertical momentum over its first .3 s.
        return Vector3.ProjectOnPlane(shape.velocity,Vector3.up)*seconds+
            Vector3.up*shape.velocity.y*(.3f*(1-Mathf.Exp(-seconds/.3f)));
    }
    static float RedWaterCapsuleCost(RedWaterCapsule capsule,Vector3 offset,float seconds,float margin,out Vector3 avoidance)
    {
        avoidance=Vector3.zero;float cost=0;
        Vector3 a=capsule.a+offset,b=capsule.b+offset;float radius=capsule.radius+margin;
        var capsuleBounds=new Bounds((a+b)*.5f,Vector3.zero);capsuleBounds.Encapsulate(a);capsuleBounds.Encapsulate(b);capsuleBounds.Expand(radius*2);
        redWaterQuery.radius=radius;redWaterQuery.height=(b-a).magnitude+radius*2;
        Quaternion capsuleRotation=(b-a).sqrMagnitude>.00001f?Quaternion.FromToRotation(Vector3.up,b-a):Quaternion.identity;
        redWaterQuery.enabled=true;
        try
        {
        foreach(var shape in redWaterShapes)
        {
            Vector3 translation=RedWaterForecastTranslation(shape,seconds);
            Quaternion rotation=shape.spin.sqrMagnitude>.0001f?Quaternion.AngleAxis(shape.spin.magnitude*Mathf.Rad2Deg*Mathf.Min(seconds,.7f),shape.spin.normalized):Quaternion.identity;
            // Rotation-aware broad phase; exact checks below use real cut parts.
            float reach=shape.bounds.extents.magnitude+Vector3.Distance(shape.bounds.center,shape.centre);
            Bounds future=new Bounds(shape.centre+translation,Vector3.one*(reach*2));
            if(!future.Intersects(capsuleBounds))continue;
            for(int i=0;i<shape.mesh.Parts.Length;i++)
            {
                var part=shape.mesh.Parts[i];
                if(part==null||!part.enabled||!part.gameObject.activeInHierarchy)continue;
                Vector3 point=shape.centre+translation+rotation*(shape.mesh.PartPosition(i)-shape.centre);
                Quaternion turn=rotation*shape.mesh.PartRotation(i);
                Bounds present=shape.mesh.PartBounds(i);Vector3 e=present.extents;
                Vector3 x=rotation*(Vector3.right*e.x),y=rotation*(Vector3.up*e.y),z=rotation*(Vector3.forward*e.z);
                Vector3 extents=new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));
                Bounds partBounds=new Bounds(shape.centre+translation+rotation*(present.center-shape.centre),extents*2);
                partBounds.Expand(radius*2);
                if(!partBounds.Intersects(capsuleBounds))continue;
                if(!Physics.ComputePenetration(redWaterQuery,(a+b)*.5f,capsuleRotation,part,point,turn,out var direction,out float depth))continue;
                cost+=depth*depth+.01f;
                Vector3 planar=Vector3.ProjectOnPlane(direction,Vector3.up);
                if(planar.sqrMagnitude<.04f)planar=Vector3.ProjectOnPlane((a+b)*.5f-(shape.centre+translation),Vector3.up);
                if(planar.sqrMagnitude>.0001f)avoidance+=planar.normalized*depth;
            }
        }
        return cost;
        }
        finally{redWaterQuery.enabled=false;}
    }
    static float RedWaterBodyCost(List<RedWaterCapsule> body,Vector3 offset,float seconds,float margin,out Vector3 avoidance)
    {
        avoidance=Vector3.zero;float cost=0;
        foreach(var capsule in body){cost+=RedWaterCapsuleCost(capsule,offset,seconds,margin,out var push);avoidance+=push;}
        return cost;
    }

}
