using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Mavis;

public static class InstallIsland2Audio
{
    const string Folder="Assets/Resources/SecondIslandAudio";
    static AudioClip Clip(string name)
    {var c=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/"+name+".wav");if(!c)throw new Exception("Missing clip: "+name);return c;}
    public static object Run()
    {
        var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=SecondIslandAudio.ScenePath||scene.isDirty)throw new Exception("Saved Island 2 Edit scene required");
        Directory.CreateDirectory(".codex/island2-audio");if(!File.Exists(".codex/island2-audio/before.unity"))File.Copy(scene.path,".codex/island2-audio/before.unity");
        AssetDatabase.Refresh();var paths=Directory.GetFiles(Folder,"*.wav");
        AssetDatabase.StartAssetEditing();
        try{foreach(var path in paths){var importer=(AudioImporter)AssetImporter.GetAtPath(path);bool loop=Path.GetFileNameWithoutExtension(path)=="CloudWind"||Path.GetFileNameWithoutExtension(path)=="ChocolateFlow"||Path.GetFileNameWithoutExtension(path)=="ChocolateBubbles";
            importer.forceToMono=Path.GetFileNameWithoutExtension(path)!="CloudWind";importer.loadInBackground=loop;
            var sample=importer.defaultSampleSettings;sample.preloadAudioData=!loop;sample.loadType=loop?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;sample.compressionFormat=loop?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;sample.quality=.65f;sample.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;sample.sampleRateOverride=32000;importer.defaultSampleSettings=sample;importer.SaveAndReimport();}}
        finally{AssetDatabase.StopAssetEditing();}
        var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Island 2 - Soundscape");if(!root){root=new GameObject("Island 2 - Soundscape");Undo.RegisterCreatedObjectUndo(root,"Install Island 2 soundscape");}
        var sound=root.GetComponent<SecondIslandAudio>();if(!sound)sound=Undo.AddComponent<SecondIslandAudio>(root);
        sound.cloudWind=Clip("CloudWind");sound.chocolateFlow=Clip("ChocolateFlow");sound.chocolateBubbles=Clip("ChocolateBubbles");
        var decoration=scene.GetRootGameObjects().First(g=>g.name=="Island 2 - Environment Decoration");var connected=decoration.transform.Find("Chocolate River - The Couverture Run/Lake to Cloudfall - connected chocolate water system");
        if(!sound.waterway && decoration.transform.Find("Chocolate Waterway - West Valley"))
            sound.waterway=AssetDatabase.LoadAssetAtPath<SecondIslandChocolateWaterway>("Assets/Game/Art/Island2Waterway/West valley waterway.asset");
        if(!sound.waterway)
        {
            var river=connected?connected.Find("Continuous chocolate river - lake outlet to cliff"):decoration.transform.Find("Chocolate River - The Couverture Run/Slow flowing couverture - walkable shallow spill");
            var v=river.GetComponent<MeshFilter>().sharedMesh.vertices;
            int rows=v.Length/21-1;var indices=Enumerable.Range(0,rows+1).Where(i=>i%4==0||i==rows).ToArray();
            sound.riverPoints=indices.Select(i=>river.TransformPoint(v[i*21+10])).ToArray();
            sound.riverHalfWidths=indices.Select(i=>{var d=river.TransformVector(v[i*21+20]-v[i*21]);return new Vector2(d.x,d.z).magnitude*.5f;}).ToArray();
            if(connected){var lake=connected.Find("Grand Chocolate Lake - 170 metre headwater");sound.chocolateLakeCenter=lake.TransformPoint(lake.GetComponent<MeshFilter>().sharedMesh.vertices[0]);sound.chocolateLakeRadii=new Vector2(85,62);sound.chocolateFallLip=sound.riverPoints.Last();}
        }
        sound.confectionPoints=decoration.GetComponentsInChildren<Transform>().Where(t=>t.Find("Confection")).Select(t=>t.position).Where((p,i)=>i%5==0).ToArray();EditorUtility.SetDirty(sound);
        var profile=AssetDatabase.LoadAssetAtPath<CombatFeelProfile>(Folder+"/CombatSounds.asset");if(!profile){profile=UnityEngine.Object.Instantiate(CombatFeelProfile.Default);profile.name="Island 2 audio palette";AssetDatabase.CreateAsset(profile,Folder+"/CombatSounds.asset");}
        profile.contact=Enumerable.Range(0,3).Select(i=>Clip("Contact"+i)).ToArray();profile.wood=Enumerable.Range(0,3).Select(i=>Clip("Crunch"+i)).ToArray();profile.metal=Enumerable.Range(0,3).Select(i=>Clip("Metal"+i)).ToArray();profile.flesh=Enumerable.Range(0,3).Select(i=>Clip("SoftHit"+i)).ToArray();profile.lowImpact=new[]{Clip("LowImpact")};profile.enemyReaction=Array.Empty<AudioClip>();profile.soundVolume=.67f;EditorUtility.SetDirty(profile);
        var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SahurAttack>(true)).First();Undo.RecordObject(player,"Island 2 swing recordings");player.comboSwishes=new[]{Clip("Swing"),Clip("Swing_1"),Clip("Swing_2")};player.heavySwish=Clip("RakeSwing");EditorUtility.SetDirty(player);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        var result=new{clips=paths.Length,riverSamples=sound.waterway?sound.waterway.sections.Length:sound.riverPoints.Length,confectionAnchors=sound.confectionPoints.Length,loopStreaming=true,masterVolumeUnchanged=AudioListener.volume,playerPrefabPreserved=true};
        File.WriteAllText(".codex/island2-audio/installation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
