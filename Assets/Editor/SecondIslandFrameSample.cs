using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Samples distinct rendered frames in the current Editor play session.</summary>
public static class SecondIslandFrameSample
{
    public static string Status="idle", Result="";
    static readonly List<float> samples=new List<float>();
    static float end;
    static int previousFrame;
    public static void Begin(float seconds=10f)
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play mode first.");
        EditorApplication.update-=Tick;samples.Clear();previousFrame=-1;
        end=Time.realtimeSinceStartup+Mathf.Max(1,seconds);Status="running";Result="";
        EditorApplication.update+=Tick;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying){EditorApplication.update-=Tick;Status="stopped";return;}
        if(Time.frameCount!=previousFrame && Time.unscaledDeltaTime>0)
        {previousFrame=Time.frameCount;samples.Add(Time.unscaledDeltaTime*1000f);}
        if(Time.realtimeSinceStartup<end)return;
        EditorApplication.update-=Tick;samples.Sort();
        if(samples.Count==0){Status="failed";Result="No frames sampled.";return;}
        float At(float q)=>samples[Mathf.Clamp(Mathf.FloorToInt((samples.Count-1)*q),0,samples.Count-1)];
        Result=$"{samples.Count} frames; median {At(.5f):F2} ms; p95 {At(.95f):F2} ms; p99 {At(.99f):F2} ms; max {samples[samples.Count-1]:F2} ms.";
        Status="completed";
    }
}
