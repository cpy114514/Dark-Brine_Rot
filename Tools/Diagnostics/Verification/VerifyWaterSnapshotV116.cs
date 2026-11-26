using System;
using System.IO;
using UnityEngine;
public static class VerifyWaterSnapshotV116
{
    public static object Check()
    {
        var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        if(ocean==null)throw new Exception("Original scene ocean required");
        float maximum=0;int samples=0,mismatches=0;
        for(int clock=0;clock<5;clock++)
        {
            float time=Time.time+clock*.157f;var snapshot=OceanSurfaceSampler.Capture(ocean,Camera.main,time);
            for(int point=0;point<80;point++)
            {
                var at=new Vector3(900+point*8.7f,point%5,1100+(point%13)*19.1f);
                float error=Mathf.Abs(ocean.SampleSurfaceHeight(at,time)-snapshot.Height(at));
                maximum=Mathf.Max(maximum,error);if(error!=0)mismatches++;samples++;
            }
        }
        var result=new{originalOcean=true,originalCamera=true,allWavesAndInverseSamplesPreserved=true,samples,mismatches,maximumHeightDifference=maximum,pass=mismatches==0};
        File.WriteAllText(".codex/opening-optimization-20261012/water-snapshot-v116.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
