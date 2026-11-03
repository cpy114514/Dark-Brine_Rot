using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Mavis;
public static class PreviewThread059
{
    static int previous;
    public static object Start()
    {
        if(!Application.isPlaying)throw new Exception("Live game required");
        previous=EditorPrefs.GetInt("ScriptCompilationDuringPlay",0);
        File.WriteAllText(".codex/thread059/compilation-preference.txt",previous.ToString());
        EditorPrefs.SetInt("ScriptCompilationDuringPlay",2);
        EditorApplication.playModeStateChanged+=Restore;
        var guide=UnityEngine.Object.FindFirstObjectByType<MainQuestGuidance>();
        if(!guide)throw new Exception("No live guide");
        var run=guide.gameObject.AddComponent<ThreadPreview059>();run.guide=guide;
        return new{preview=true,deferredCompilation=true};
    }
    static void Restore(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.ExitingPlayMode)return;
        EditorPrefs.SetInt("ScriptCompilationDuringPlay",previous);EditorApplication.playModeStateChanged-=Restore;
    }
}
[DefaultExecutionOrder(19000)] public sealed class ThreadPreview059:MonoBehaviour
{
    public MainQuestGuidance guide;
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    readonly FieldInfo meshField=typeof(MainQuestGuidance).GetField("mesh",Flags);
    readonly Vector3[] centres=new Vector3[3];
    void LateUpdate()
    {
        if(!guide||!guide.Visible)return;
        var mesh=meshField.GetValue(guide) as Mesh;if(!mesh||mesh.vertexCount!=1152)return;
        Style(mesh);
    }
    public static void Style(Mesh mesh)
    {
        var points=mesh.vertices;var colors=mesh.colors;
        var centres=new Vector3[3];
        for(int p=0;p<64;p++)
        {
            Vector3 middle=Vector3.zero;
            for(int s=0;s<3;s++){Vector3 c=Vector3.zero;for(int r=0;r<6;r++)c+=points[(s*64+p)*6+r];centres[s]=c/6;middle+=centres[s]/3;}
            for(int s=0;s<3;s++)for(int r=0;r<6;r++)
            {
                int i=(s*64+p)*6+r;points[i]=middle+(centres[s]-middle)*.4f+(points[i]-centres[s])*(s==0?.3111111f:.2f);
                colors[i]=new Color(s==0?1:.86f,s==0?1:.90f,1,colors[i].a*(s==0?1:.48f));
            }
        }
        mesh.vertices=points;mesh.colors=colors;mesh.RecalculateBounds();
    }
}
