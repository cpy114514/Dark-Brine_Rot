using Mavis;
using UnityEngine;

public sealed partial class HuangMeiBoss
{
    static readonly float[] MandalaTells={.05f,1.31f,2.63f};
    static readonly float[] MandalaImpacts={.97f,2.25f,3.66f};
    static readonly float[] InnerRadii={0,10.2f,0};
    static readonly float[] OuterRadii={9.3f,22.5f,6.8f};
    static readonly float[] PulseDamage={26,31,37};
    const float MandalaHeight=1.65f;
    float nextMandala;
    Vector3 mandalaCenter,markedCenter;
    bool marked;

    void PrepareMandala()
    {
        mandalaCenter=GroundAt(transform.position);
        marked=false;
        nextMandala=Time.time+28f;
        effects.RingBell(transform.position+Vector3.up*4.6f);
    }

    void DrawMandala(float radius,float charge) => effects.Halo(
        transform.TransformPoint(new Vector3(0,7.15f,-1.85f)),transform.right,radius,charge);

    void TickMandala()
    {
        float elapsed=ActionElapsed;
        DrawMandala(6.25f,1);
        if(!marked && elapsed>=MandalaTells[2])
        {
            // Lock the third seal when its outline appears. Movement after that stays safe.
            var offset=Flat(playerBody.bounds.center-mandalaCenter);
            markedCenter=GroundAt(mandalaCenter+Vector3.ClampMagnitude(offset,31));
            marked=true;
        }
        int current=-1;
        for(int i=0;i<MandalaImpacts.Length;i++)
        {
            if(elapsed>=MandalaTells[i])current=i;
            if(!Spend(i,MandalaImpacts[i]/duration))continue;
            Vector3 center=i==2?markedCenter:mandalaCenter;
            effects.Impact(center,i==2?48:37,true,InnerRadii[i],OuterRadii[i]);
            if(InGroundSeal(center,InnerRadii[i],OuterRadii[i],MandalaHeight))Damage(PulseDamage[i],center);
        }
        if(current<0){effects.HideGround();return;}
        float hit=MandalaImpacts[current];
        Vector3 origin=current==2?markedCenter:mandalaCenter;
        if(elapsed<hit)
            effects.Ground(origin,InnerRadii[current],OuterRadii[current],Mathf.InverseLerp(MandalaTells[current],hit,elapsed),false);
        else if(elapsed<hit+.36f)
            effects.Ground(origin,InnerRadii[current],OuterRadii[current],(elapsed-hit)/.36f,true);
        else effects.HideGround();
    }

    public static float StaffPlant(float elapsed)
    {
        float plant=0;
        foreach(float hit in MandalaImpacts)
        {
            float lift=elapsed<hit ? Mathf.InverseLerp(hit-.34f,hit,elapsed) : 1-Mathf.InverseLerp(hit,hit+.29f,elapsed);
            plant=Mathf.Max(plant,Mathf.SmoothStep(0,1,lift));
        }
        return Mathf.Max(plant,Mathf.InverseLerp(3.93f,4.18f,elapsed));
    }

    bool InGroundSeal(Vector3 center,float inner,float outer,float height)
    {
        if(!playerBody || !playerBody.enabled)return false;
        float radius=playerBody.radius*Mathf.Abs(player.transform.lossyScale.x);
        float distance=Flat(playerBody.bounds.center-center).magnitude;
        if(distance-radius>outer || distance+radius<inner)return false;
        float feet=playerBody.bounds.min.y;
        var position=playerBody.bounds.center;position.y=feet;
        return feet-GroundAt(position).y<height && Mathf.Abs(feet-center.y)<5.5f;
    }

    public bool ForecastDodge(PlayerHealth target,float horizon,out PerfectDodgeThreat threat)
    {
        threat=default;
        if(!FightActive || target!=player || !playerBody || !playerBody.enabled || CurrentState!=State.Windup && CurrentState!=State.Strike)return false;
        bool wind=CurrentState==State.Windup;
        if(CurrentAttack==Attack.StaffSweep)
        {
            int beat=(hitMask&1)==0?0:1;
            if((hitMask&(1<<beat))!=0)return false;
            float phase=beat==0?FirstSweep:SecondSweep;
            float seconds=wind?timer+StrikeDuration(CurrentAttack)*phase:duration*phase-ActionElapsed;
            return PerfectDodgeThreat.Sector(this,transform,target,seconds,horizon,transform.position,wind?transform.forward:committed,
                beat==0?9.7f:10.2f,beat==0?120f:158f,3.7f,Stamp(beat),out threat);
        }
        if(CurrentAttack==Attack.StaffSlam && !pulseHit && InGroundSeal(sealCenter,previousRadius,SlamRadius,1.30f))
        {
            float strike=StrikeDuration(CurrentAttack);
            float wavePhase=Mathf.InverseLerp(.45f,SlamRadius,Flat(playerBody.bounds.center-sealCenter).magnitude-playerBody.radius*Mathf.Abs(player.transform.lossyScale.x));
            float hit=strike*Mathf.Lerp(ImpactPhase(CurrentAttack),1,wavePhase);
            return GroundThreat(wind?timer+hit:hit-ActionElapsed,horizon,0,sealCenter,out threat);
        }
        if(CurrentAttack==Attack.PalmSeal && palmFlying)
        {
            var bounds=playerBody.bounds;
            float distance=Vector3.Dot(bounds.center-palmPosition,committed);
            Vector3 projected=palmPosition+committed*Mathf.Max(0,distance);
            if(distance+PalmRadius<0 || distance-PalmRadius>PalmRange-palmDistance || bounds.SqrDistance(projected)>PalmRadius*PalmRadius)return false;
            return GroundThreat(Mathf.Max(0,distance-PalmRadius-bounds.extents.x)/PalmSpeed,horizon,0,palmPosition,out threat);
        }
        if(CurrentAttack==Attack.GoldenMandala && !wind)
            for(int i=0;i<MandalaImpacts.Length;i++)
            {
                if((hitMask&(1<<i))!=0 || i==2 && !marked)continue;
                Vector3 center=i==2?markedCenter:mandalaCenter;
                if(InGroundSeal(center,InnerRadii[i],OuterRadii[i],MandalaHeight) &&
                    GroundThreat(MandalaImpacts[i]-ActionElapsed,horizon,i,center,out threat))return true;
            }
        return false;
    }

    long Stamp(int beat)=>((long)GetInstanceID()<<32)^(uint)(attackSerial*8+beat);
    bool GroundThreat(float seconds,float horizon,int beat,Vector3 origin,out PerfectDodgeThreat threat)
    {
        threat=default;
        if(seconds<-.025f || seconds>horizon || !PerfectDodgeThreats.ClearPath(player.transform,transform,origin+Vector3.up*2.5f,playerBody.bounds.center))return false;
        threat=new PerfectDodgeThreat{source=this,attacker=transform,seconds=Mathf.Max(0,seconds),stamp=Stamp(beat)};
        return true;
    }
}
