using System;
using System.IO;
using UnityEngine;
using Mavis;
public static class TestCombatAudio
{
    public static object Run()
    {
        var profile=CombatFeelProfile.Default;
        var clips=new[]{profile.contact[0],profile.lowImpact[0],profile.flesh[0]};
        var samples=new float[3][];
        for(int i=0;i<3;i++){samples[i]=new float[clips[i].samples*clips[i].channels];clips[i].GetData(samples[i],0);}
        float peak=0;
        // Worst endpoint pitch combinations for the configured heavy flesh impact.
        for(int variant=0;variant<8;variant++)for(int frame=0;frame<24000;frame++)
        {
            float mixed=0;
            for(int i=0;i<3;i++)
            {
                float pitch=(i==1?.91f:1)+((variant&(1<<i))==0?-.035f:.035f);
                float source=frame/48000f*clips[i].frequency*pitch;int index=(int)source;
                if(index+1>=clips[i].samples)continue;
                float fraction=source-index,value=0;
                for(int c=0;c<clips[i].channels;c++)value+=Mathf.Lerp(samples[i][index*clips[i].channels+c],samples[i][(index+1)*clips[i].channels+c],fraction)/clips[i].channels;
                mixed+=value*profile.soundVolume*(i==0?.85f:i==1?.8f:.35f);
            }
            peak=Mathf.Max(peak,Mathf.Abs(mixed));
        }
        var output=new{passed=peak<.98f,heavyFleshMixPeak=peak,pitchCombinations=8,unattenuated=true,enemyVocalAssigned=profile.enemyReaction!=null && profile.enemyReaction.Length>0,
            limitation="Offline peak estimate for configured stems; excludes whoosh overlap, audio-device processing, metal/wood mixes and subjective listening."};
        File.WriteAllText("D:/indiegameDev/unity/Dark Brine_Rot/.codex/combat-feel-20261018/audio-check.json",Newtonsoft.Json.JsonConvert.SerializeObject(output,Newtonsoft.Json.Formatting.Indented));
        return output;
    }
}
