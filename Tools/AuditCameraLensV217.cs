using UnityEngine;
public static class AuditCameraLensV217
{
    public static object Run()
    {
        var a=new GameObject("Temporary lens query probe");var b=new GameObject("Temporary timber query probe");
        try{
            var sphere=a.AddComponent<SphereCollider>();sphere.radius=.45f;
            var box=b.AddComponent<BoxCollider>();box.size=Vector3.one;
            sphere.enabled=false;
            bool disabled=Physics.ComputePenetration(sphere,Vector3.zero,Quaternion.identity,box,new Vector3(.4f,0,0),Quaternion.identity,out var d0,out var p0);
            sphere.enabled=true;
            bool enabled=Physics.ComputePenetration(sphere,Vector3.zero,Quaternion.identity,box,new Vector3(.4f,0,0),Quaternion.identity,out var d1,out var p1);
            return new{disabled,p0,enabled,p1};
        }finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
    }
}
