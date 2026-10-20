using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Mavis;
public static class InstallCombatFeel
{
    public static object Install()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode before authoring.");
        const string folder="Assets/Resources/Combat";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources","Combat");
        const string path=folder+"/SahurCombatFeel.asset";
        var profile=AssetDatabase.LoadAssetAtPath<CombatFeelProfile>(path);
        if(profile)throw new Exception("Already installed: tune the existing asset instead of overwriting.");
        profile=ScriptableObject.CreateInstance<CombatFeelProfile>();
        var contact=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/StoryBattle/Audio/StickHit.wav");
        var guard=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/StoryBattle/Audio/Guard.wav");
        var samples=new float[contact.samples*contact.channels];contact.GetData(samples,0);
        // Offline stems derived from the retained contact recording. No waveform generation on hit.
        WriteStem(folder+"/ClubBody.wav",samples,contact.channels,contact.frequency,.22f,140,.8f);
        WriteStem(folder+"/ClubSurface.wav",samples,contact.channels,contact.frequency,.14f,950,.48f);
        AssetDatabase.ImportAsset(folder+"/ClubBody.wav",ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(folder+"/ClubSurface.wav",ImportAssetOptions.ForceSynchronousImport);
        foreach(string audioPath in new[]{folder+"/ClubBody.wav",folder+"/ClubSurface.wav"})
        {
            var importer=(AudioImporter)AssetImporter.GetAtPath(audioPath);var s=importer.defaultSampleSettings;
            s.loadType=AudioClipLoadType.DecompressOnLoad;s.compressionFormat=AudioCompressionFormat.PCM;
            importer.defaultSampleSettings=s;importer.forceToMono=true;importer.SaveAndReimport();
        }
        profile.contact=new[]{contact};profile.wood=new[]{contact};profile.metal=new[]{guard};
        profile.lowImpact=new[]{AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"/ClubBody.wav")};
        profile.flesh=new[]{AssetDatabase.LoadAssetAtPath<AudioClip>(folder+"/ClubSurface.wav")};
        // No invented enemy vocal: optional per-enemy recording slot remains empty.
        AssetDatabase.CreateAsset(profile,path);
        var changed=new System.Collections.Generic.List<object>();
        foreach(string controllerPath in new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller"})
        {
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(!controller.parameters.Any(p=>p.name=="LightPlaybackSpeed"))controller.AddParameter(new AnimatorControllerParameter{name="LightPlaybackSpeed",type=AnimatorControllerParameterType.Float,defaultFloat=1});
            foreach(var layer in controller.layers)foreach(var child in layer.stateMachine.states)
            {
                var state=child.state;
                if(state.name!="Combo 1" && state.name!="Combo 2" && state.name!="Combo 3")continue;
                state.speedParameter="LightPlaybackSpeed";state.speedParameterActive=true;
                EditorUtility.SetDirty(state);changed.Add(new{controllerPath,layer=layer.name,state.name,clip=AssetDatabase.GetAssetPath(state.motion),state.speed});
            }
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        }
        AssetDatabase.SaveAssetIfDirty(profile);
        return new{profile=path,changed,localStop=true,originalClipsAndMasksRetained=true};
    }
    static void WriteStem(string path,float[] input,int channels,int frequency,float seconds,float cutoff,float gain)
    {
        int n=Mathf.Min(input.Length/channels,Mathf.CeilToInt(seconds*frequency));var output=new float[n];
        float filtered=0, coefficient=1-Mathf.Exp(-2*Mathf.PI*cutoff/frequency),peak=.0001f;
        for(int i=0;i<n;i++)
        {
            float value=0;for(int c=0;c<channels;c++)value+=input[i*channels+c]/channels;
            filtered+=coefficient*(value-filtered);
            float fade=Mathf.Min(1,i/(frequency*.002f))*Mathf.Clamp01((n-i)/(frequency*.04f));
            output[i]=filtered*fade;peak=Mathf.Max(peak,Mathf.Abs(output[i]));
        }
        using(var writer=new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+n*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(frequency);writer.Write(frequency*2);writer.Write((short)2);writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(n*2);
            foreach(float value in output)writer.Write((short)(Mathf.Clamp(value/peak*gain,-1,1)*32767));
        }
    }
}
