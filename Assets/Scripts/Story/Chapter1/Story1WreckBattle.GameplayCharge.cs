using System;
using Mavis;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public sealed partial class Story1WreckBattle
{
    AnimationLayerMixerPlayable redGameplayChargeLayers;
    AnimationClipPlayable redGameplayChargePose;
    AnimationClip redGameplayChargeClip;
    AvatarMask redGameplayChargeMask;
    bool redSamplingGameplayCharge;
    float redGameplayHoldPhase,redGameplayFullCharge,redGameplayContactPhase,redGameplayContactSeconds;
    float redGameplayChargePhase;
    readonly float[] redGameplayReleaseTimes=new float[257];

    void RedCreateGameplayChargeLayer()
    {
        if(redGameplayChargeClip==null)
        {
            var attack=player.GetComponent<SahurAttack>();
            if(attack==null||attack.chargeClip==null||attack.UsesTwinBlades||attack.chargedStrikeClip!=null)
                throw new InvalidOperationException("Opening requires the existing single-clip club charge contract.");
            redGameplayChargeClip=attack.chargeClip;
            redGameplayChargeMask=Resources.Load<AvatarMask>("Encounter/v242/SahurGameplayChargeRightArm");
            if(redGameplayChargeMask==null)throw new InvalidOperationException("Gameplay charge mask missing.");
            redGameplayHoldPhase=attack.maxChargePosePhase;
            redGameplayFullCharge=attack.EffectiveFullChargeTime;
            redGameplayContactPhase=Mathf.Lerp(attack.heavySwingWindowStart,attack.heavySwingWindowEnd,.5f);
            var feel=attack.feel?attack.feel:CombatFeelProfile.Default;
            // Same phase-dependent speed as SahurAttack.UpdateChargeLayerWeight.
            // Integrate once in phase space; sampling never advances an extra clock
            // during temporary contact previews or paused input gates.
            for(int i=1;i<redGameplayReleaseTimes.Length;i++)
            {
                float phase=Mathf.Lerp(redGameplayHoldPhase,1,(i-.5f)/256f);
                float speed=phase<=attack.heavySwingWindowEnd
                    ?feel.heavySpeed.Evaluate(Mathf.InverseLerp(redGameplayHoldPhase,attack.heavySwingWindowEnd,phase))
                    :Mathf.Lerp(1.1f,1.85f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(attack.heavySwingWindowEnd,1,phase)));
                redGameplayReleaseTimes[i]=redGameplayReleaseTimes[i-1]+redGameplayChargeClip.length*(1-redGameplayHoldPhase)/256/Mathf.Max(.05f,speed);
            }
            float contactIndex=Mathf.InverseLerp(redGameplayHoldPhase,1,redGameplayContactPhase)*256;
            int lo=Mathf.Min(255,Mathf.FloorToInt(contactIndex));
            redGameplayContactSeconds=Mathf.Lerp(redGameplayReleaseTimes[lo],redGameplayReleaseTimes[lo+1],contactIndex-lo);
        }
        redGameplayChargePose=AnimationClipPlayable.Create(redGraph,redGameplayChargeClip);redGameplayChargePose.SetSpeed(0);
        redGameplayChargeLayers=AnimationLayerMixerPlayable.Create(redGraph,2);
        redGameplayChargeLayers.ConnectInput(0,redMixer,0,1);
        redGameplayChargeLayers.ConnectInput(1,redGameplayChargePose,0,0);
        redGameplayChargeLayers.SetLayerMaskFromAvatarMask(1,redGameplayChargeMask);
    }
    float RedGameplayReleasePhase(float seconds)
    {
        if(seconds<=0)return redGameplayHoldPhase;
        int index=Array.BinarySearch(redGameplayReleaseTimes,seconds);
        if(index>=0)return Mathf.Lerp(redGameplayHoldPhase,1,index/256f);
        int upper=~index;if(upper>=redGameplayReleaseTimes.Length)return 1;
        float part=Mathf.InverseLerp(redGameplayReleaseTimes[upper-1],redGameplayReleaseTimes[upper],seconds);
        return Mathf.Lerp(redGameplayHoldPhase,1,(upper-1+part)/256);
    }
    float RedGameplayReleaseWeight(float seconds)=>1-Mathf.SmoothStep(0,1,Mathf.Clamp01((seconds-redGameplayReleaseTimes[256])/.18f));
    void RedGameplayChargeOnBoard(float phase,float seconds,float weight=1)
    {
        redGameplayChargePhase=Mathf.Clamp01(phase);
        redGameplayChargePose.SetTime(redGameplayChargePhase*redGameplayChargeClip.length);
        redGameplayChargeLayers.SetInputWeight(1,Mathf.Clamp01(weight));
        redSamplingGameplayCharge=true;
        try{RedOnBoard("CH1_SahurGuard_v050",seconds);}
        finally{redSamplingGameplayCharge=false;}
    }
    void RedGameplayFilmCounter(float seconds,float contactAt)
    {
        float releaseAt=contactAt-redGameplayContactSeconds;
        if(seconds<releaseAt)
        {
            float held=Mathf.Max(0,seconds-(releaseAt-redGameplayFullCharge));
            RedGameplayChargeOnBoard(Mathf.SmoothStep(0,1,Mathf.Clamp01(held/redGameplayFullCharge))*redGameplayHoldPhase,seconds,held/.1f);
        }
        else
        {
            float released=seconds-releaseAt;
            RedGameplayChargeOnBoard(RedGameplayReleasePhase(released),seconds,RedGameplayReleaseWeight(released));
        }
    }
    void RedRestoreGameplayChargeAfterPreview(string baseClip,float baseSeconds,double chargeSeconds,float weight)
    {
        redGameplayChargePose.SetTime(chargeSeconds);
        redGameplayChargeLayers.SetInputWeight(1,weight);
        redSamplingGameplayCharge=weight>0;
        try{RedPose(baseClip,baseSeconds,false);}
        finally{redSamplingGameplayCharge=false;}
    }
}
