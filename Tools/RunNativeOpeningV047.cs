using System;
using UnityEngine;
public static class RunNativeOpeningV047
{
    public static object Start(bool capture=true,bool water=false,int seed=20261012)
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var holder=new GameObject("Native opening integration benchmark");
        UnityEngine.Object.DontDestroyOnLoad(holder);var benchmark=holder.AddComponent<OpeningBenchmark>();benchmark.captureScreenshots=capture;benchmark.waterEntry=water;benchmark.scatterSeed=seed;
        return new{started=true,report=Application.persistentDataPath+"/opening-benchmark.json"};
    }
}
