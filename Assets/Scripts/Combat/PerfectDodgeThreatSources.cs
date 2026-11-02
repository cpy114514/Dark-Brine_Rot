using UnityEngine;
namespace Mavis
{
    public partial class EnemyAttack:IPerfectDodgeThreatSource
    {
        public bool ForecastDodge(PlayerHealth player,float horizon,out PerfectDodgeThreat threat)
        {
            threat=default;if(!animator||!attackHitbox)return false;
            var ai=GetComponent<EnemyAI>();var hp=GetComponent<Health>();if(hp&&hp.IsDead)return false;
            var state=animator.GetCurrentAnimatorStateInfo(0);
            if(animator.IsInTransition(0))state=animator.GetNextAnimatorStateInfo(0);
            if(!state.IsName(attackStateName)||state.normalizedTime>windowEnd||(windowActive&&victims.Contains(player)))return false;
            float seconds=Mathf.Max(0,windowStart-state.normalizedTime)*state.length/Mathf.Max(.01f,Mathf.Abs(state.speed*animator.speed));
            float reach=ai?ai.attackRange:Mathf.Max(1,Vector3.Distance(transform.position,attackHitbox.transform.position)+attackHitbox.bounds.extents.magnitude);
            float started=Time.time-state.normalizedTime*state.length;
            return PerfectDodgeThreat.Sector(this,transform,player,seconds,horizon,transform.position,transform.forward,reach,150,3,PerfectDodgeThreat.Stamp(this,started,0),out threat);
        }
    }
    public sealed partial class NailongAI:IPerfectDodgeThreatSource
    {
        public bool ForecastDodge(PlayerHealth player,float horizon,out PerfectDodgeThreat threat)
        {
            threat=default;if(!FightActive||state!=State.Attack||health.IsDead||!attack)return false;
            float elapsed=Time.time-attackStartedAt,cue;int strike=0;float reach=EffectiveAttackReach,arc=attack.attackArc;
            Vector3 origin=NailongSize.Feet(transform),direction=transform.forward;
            switch(activeAttack)
            {
                case AttackPattern.Slap:if(attackResolved)return false;cue=attackWindup;break;
                case AttackPattern.ClawFlurry:if(strikesDone>=flurryCount)return false;strike=strikesDone;cue=flurryWindup+strike*flurryInterval;break;
                case AttackPattern.CryingBurst:if(attackResolved)return false;cue=cryingWindup;reach=cryingRadius;arc=360;break;
                case AttackPattern.UnreasonableTantrum:
                    if(tantrumPulsesDone>=tantrumPulseCount)return false;
                    strike=tantrumPulsesDone;cue=tantrumWindup+tantrumPulseInterval*strike;reach=tantrumRadius*(.55f+.225f*strike);arc=360;break;
                case AttackPattern.Rampage:if(strikesDone!=3)return false;cue=3.35f;strike=3;break;
                case AttackPattern.ShamelessCharge:
                    if(attackResolved||elapsed>.55f+shamelessChargeDuration)return false;
                    reach=1.9f*SizeFactor;arc=360;
                    float gap=Vector3.Dot(Vector3.ProjectOnPlane(player.transform.position-origin,Vector3.up),chargeDirection)-reach;
                    cue=Mathf.Max(.55f,elapsed)+Mathf.Max(0,gap)/Mathf.Max(1,shamelessChargeSpeed);
                    direction=chargeDirection;origin+=direction*Mathf.Max(0,cue-Mathf.Max(.55f,elapsed))*shamelessChargeSpeed;break;
                default:return false;
            }
            return PerfectDodgeThreat.Sector(this,transform,player,Mathf.Max(0,cue-elapsed),horizon,origin,direction,reach,arc,attack.verticalTolerance,PerfectDodgeThreat.Stamp(this,attackStartedAt,strike),out threat,true,false);
        }
    }
    public sealed partial class CappuccinoAI:IPerfectDodgeThreatSource
    {
        public bool ForecastDodge(PlayerHealth player,float horizon,out PerfectDodgeThreat threat)
        {
            threat=default;if(!FightActive||health.IsDead||CurrentState!=State.Windup&&CurrentState!=State.Attack)return false;
            bool wind=CurrentState==State.Windup;
            if(!wind&&hitResolved)return false;
            float duration=wind?(CurrentAttack==AttackKind.Whirlwind?1.05f:CurrentAttack==AttackKind.DashThrust?.78f:.72f)/AttackFrequencyMultiplier:swingSeconds;
            float elapsed=wind?-(windupDuration-(Time.time-stateStartedAt)):Time.time-attackStartedAt-swingIndex*(swingSeconds+.18f/AttackFrequencyMultiplier);
            float cue=(hitWindowStart<=.28f?Mathf.Lerp(0,.16f,Mathf.InverseLerp(.22f,.28f,hitWindowStart)):.16f+.36f*Mathf.InverseLerp(.28f,.62f,hitWindowStart))*duration;
            float seconds=Mathf.Max(0,cue-elapsed);
            Vector3 origin=transform.position,direction=wind?transform.forward:committedDirection;
            if(CurrentAttack==AttackKind.DashThrust)origin+=direction*dashSpeed*Mathf.Max(0,seconds-Mathf.Max(0,-elapsed));
            return PerfectDodgeThreat.Sector(this,transform,player,seconds,horizon,origin,direction,attackReach,CurrentAttack==AttackKind.Whirlwind?360:CurrentAttack==AttackKind.DashThrust?65:attackArc,3,
                PerfectDodgeThreat.Stamp(this,wind?stateStartedAt+windupDuration:attackStartedAt,wind?0:swingIndex),out threat);
        }
    }
    public sealed partial class BombardinoBoss:IPerfectDodgeThreatSource
    {
        public bool ForecastDodge(PlayerHealth player,float horizon,out PerfectDodgeThreat threat)
        {
            threat=default;if(!FightActive||CurrentState!=State.Attack||CurrentAttack!=AttackKind.SpeedRun||health.IsDead||rushVictims.Contains(player))return false;
            Vector3 origin=Ground(transform.position),delta=Vector3.ProjectOnPlane(player.transform.position-origin,Vector3.up);
            float seconds=Mathf.Max(0,Vector3.Dot(delta,heading)-2.6f)/Mathf.Max(1,AttackSpeed);
            origin+=heading*(seconds*AttackSpeed);
            return PerfectDodgeThreat.Sector(this,transform,player,seconds,horizon,origin,heading,2.6f,360,4,PerfectDodgeThreat.Stamp(this,Time.time-age,0),out threat);
        }
    }
    public sealed partial class ChickenBroAI:IPerfectDodgeThreatSource
    {
        public bool ForecastDodge(PlayerHealth target,float horizon,out PerfectDodgeThreat threat)
        {
            threat=default;if(!HoldsAttack||health.IsDead||struck.Contains(target)||Mode==Mood.Wing&&wingDamage<=0)return false;
            float duration=Mode==Mood.Lunge?lungeDuration:Mode==Mood.Wing?wingDuration:strikeDuration;
            float start=Mode==Mood.Lunge?lungeWindowStart:Mode==Mood.Wing?wingWindowStart:strikeWindowStart;
            float end=Mode==Mood.Lunge?lungeWindowEnd:Mode==Mood.Wing?wingWindowEnd:strikeWindowEnd;
            if(actionTime>duration*end)return false;
            float seconds=Mathf.Max(0,duration*start-actionTime);if(seconds>horizon)return false;
            float forward=motor.radius+(Mode==Mood.Wing?.25f:.35f)+(Mode==Mood.Strike?peckReach:0);
            Vector3 origin=transform.position+Vector3.up*(motor.height*(Mode==Mood.Wing?.42f:.55f))+committed*forward;
            var body=target.GetComponent<CharacterController>();float radius=Mode==Mood.Wing?wingRadius:attackRadius;
            if(!body||body.bounds.SqrDistance(origin)>radius*radius||!PerfectDodgeThreats.ClearPath(target.transform,transform,origin,body.ClosestPoint(origin)))return false;
            threat=new PerfectDodgeThreat{source=this,attacker=transform,seconds=seconds,stamp=PerfectDodgeThreat.Stamp(this,Time.time-actionTime,0)};return true;
        }
    }
}
public sealed partial class PigRoninBoss:Mavis.IPerfectDodgeThreatSource
{
    public bool ForecastDodge(Mavis.PlayerHealth player,float horizon,out Mavis.PerfectDodgeThreat threat)
    {
        threat=default;if(!FightActive||health.IsDead||CurrentState!=State.Windup&&CurrentState!=State.Attack)return false;
        bool wind=CurrentState==State.Windup;int strike=CurrentAttack==AttackKind.TwinSlash&&(hitMask&1)!=0?1:0;
        if((hitMask&(1<<strike))!=0)return false;
        float duration=AttackDuration(CurrentAttack),phase=CurrentAttack==AttackKind.TwinSlash?(strike==0?.27f:.65f):CurrentAttack==AttackKind.Overhead?.53f:CurrentAttack==AttackKind.Dash?.08f:.48f;
        float seconds=wind?timer+duration*phase:Mathf.Max(0,timer-duration*(1-phase));
        float reach=CurrentAttack==AttackKind.TwinSlash?slashReach:CurrentAttack==AttackKind.Overhead?overheadReach:CurrentAttack==AttackKind.Dash?dashReach:spinReach;
        Vector3 direction=wind?transform.forward:committed,origin=transform.position;
        if(CurrentAttack==AttackKind.Dash)origin+=direction*dashSpeed*Mathf.Max(0,seconds-(wind?timer:0));
        return Mavis.PerfectDodgeThreat.Sector(this,transform,player,seconds,horizon,origin,direction,reach,CurrentAttack==AttackKind.Spin?360:CurrentAttack==AttackKind.Overhead?80:120,3,
            ((long)GetInstanceID()<<32)^(uint)(AttacksStarted*17+strike),out threat);
    }
}
