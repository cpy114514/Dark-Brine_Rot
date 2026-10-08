using System;
using UnityEngine;
public static class VerifyCapsuleGeometryV014
{
    public static async System.Threading.Tasks.Task<object> Verify()
    {
        if(!Application.isPlaying)throw new Exception("Play mode required");
        var go=new GameObject("Isolated capsule geometry fixture");
        try
        {
            go.transform.position=new Vector3(3000,60,3000);
            float worst=0;int samples=0;var rows=new System.Collections.Generic.List<object>();
            foreach(var scale in new[]{new Vector3(2,3,.7f),new Vector3(.8f,.7f,3),new Vector3(2,.4f,1.6f)})
            foreach(int direction in new[]{0,1,2})
            {
                var fixture=new GameObject("Capsule case");fixture.transform.SetParent(go.transform,false);
                var capsule=fixture.AddComponent<CapsuleCollider>();capsule.radius=.3f;capsule.height=4;capsule.direction=direction;
                fixture.transform.localScale=scale;fixture.transform.localRotation=Quaternion.Euler(17,31,12);
                await System.Threading.Tasks.Task.Delay(40);
                Physics.SyncTransforms();Story1CapsuleContact.Segment(capsule,out var a,out var b,out float radius);
                foreach(var offset in new[]{Vector3.up*20,Vector3.right*20,Vector3.forward*20,(b-a).normalized*20})
                {
                    Vector3 point=go.transform.position+offset,v=b-a;
                    Vector3 centre=a+v*Mathf.Clamp01(Vector3.Dot(point-a,v)/Mathf.Max(.000001f,v.sqrMagnitude));
                    Vector3 predicted=centre+(point-centre).normalized*radius;
                    var native=capsule.ClosestPoint(point);float error=Vector3.Distance(predicted,native);
                    if(error>.01f)rows.Add(new {scale=scale.ToString(),direction,radius,offset=offset.ToString(),predicted=(predicted-go.transform.position).ToString(),native=(native-go.transform.position).ToString(),error});
                    worst=Mathf.Max(worst,error);samples++;
                }
                UnityEngine.Object.DestroyImmediate(fixture);
            }
            if(worst>.002f)throw new Exception("Capsule surface differs from native physics: "+Newtonsoft.Json.JsonConvert.SerializeObject(rows));
            return new{samples,maximumNativeSurfaceError=worst};
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
}
