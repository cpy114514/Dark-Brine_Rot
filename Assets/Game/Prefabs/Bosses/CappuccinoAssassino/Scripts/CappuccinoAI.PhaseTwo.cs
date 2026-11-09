using UnityEngine;

namespace Mavis
{
    public sealed partial class CappuccinoAI
    {
        public bool IsPhaseTwoAttack => CurrentAttack >= AttackKind.ShadowChain;
        public bool PhaseTwoAirborne => IsPhaseTwoAttack && CurrentState == State.Attack &&
            CurrentAttack == AttackKind.MochaDive && specialElapsed >= .65f && specialElapsed < 1.95f;
        public bool InPhaseTransition => CurrentState == State.PhaseTransition;
        public float ShockwaveRadius { get; private set; }
        public bool ShockwaveActive { get; private set; }
        public Vector3 ShockwaveCenter => landingPoint;
        string PhaseTwoName => CurrentAttack == AttackKind.ShadowChain ? "AFTERIMAGE REQUIEM" :
            CurrentAttack == AttackKind.CremaCyclone ? "CREMA CYCLONE" :
            CurrentAttack == AttackKind.MochaDive ? "MOCHA DESCENT" : "RISTRETTO TEMPEST";
        readonly float[] phaseReadyAt = new float[4];
        readonly float[] phaseWeights = new float[4];
        static readonly float[] PhaseCooldown = { 7.5f, 9f, 11f, 16f };
        static readonly float[] PhaseDuration = { 3.35f, 3.25f, 3.45f, 5.15f };
        static readonly string[] PhaseClips = { "CapriP2Shadow", "CapriP2Cyclone", "CapriP2Dive", "CapriP2Tempest" };
        static readonly float[] ShadowHits = { .94f, 1.64f, 2.34f };
        static readonly float[] CycloneHits = { 1.02f, 1.68f };
        static readonly float[] TempestHits = { 1.12f, 1.9f, 2.65f, 3.91f };
        static readonly float[] DiveHits = { 1.95f };
        int lastSpecial = -1, previousSpecial = -1, specialHitMask, specialCueMask;
        float specialElapsed, specialPrevious, transitionElapsed;
        bool transitionStarted, transitionSuspended, transitionReleased, airTravel;
        Vector3 landingPoint, leapStart, dashOrigin, shadowDestination, specialPreviousPosition;
        CappuccinoPhaseTwoVfx phaseEffects;

        void InitializePhaseTwo()
        {
            phaseEffects = GetComponent<CappuccinoPhaseTwoVfx>();
            if (!phaseEffects) phaseEffects = gameObject.AddComponent<CappuccinoPhaseTwoVfx>();
        }

        void BeginPhaseTransition(float now)
        {
            if (transitionStarted || health.IsDead) return;
            CancelPhaseTwoAction();
            transitionStarted = true; transitionSuspended = false; transitionReleased = false;
            transitionElapsed = 0; stateStartedAt = now;
            CurrentState = State.PhaseTransition; DamageWindowActive = false; hitResolved = true;
            pendingRetreat = false; poise = 0; committedDirection = transform.forward;
            presentation.SetPhaseTwoPose("CapriP2Transition", 0);
            BossAttackWarning.For(gameObject).Clear();
            phaseEffects?.TransitionStart();
        }

        bool TickPhaseTransition(float dt, float now)
        {
            if (transitionSuspended)
            {
                transitionSuspended = false; CurrentState = State.PhaseTransition;
                stateStartedAt = now - transitionElapsed; phaseEffects?.TransitionStart();
            }
            if (CurrentState != State.PhaseTransition) return false;
            float duration = ultimate ? ultimate.TransitionSeconds : 4.2f;
            transitionElapsed = Mathf.Max(0, now - stateStartedAt);
            ActionProgress = Mathf.Clamp01(transitionElapsed / duration);
            DamageWindowActive = false; SetMovement(0, dt);
            presentation.SetPhaseTwoPose("CapriP2Transition", ActionProgress);
            if (transitionElapsed < .65f)
                Move(-committedDirection, 1.8f * Mathf.Sin(transitionElapsed / .65f * Mathf.PI), dt, -1, false);
            phaseEffects?.TransitionProgress(ActionProgress);
            if (!transitionReleased && ActionProgress >= .7f)
            {
                transitionReleased = true;
                phaseEffects?.Release();
                bossPresentation?.Cue(CappuccinoBossPresentation.CueKind.Surge);
            }
            if (ActionProgress < 1) return true;
            FightPhase = 2; transitionElapsed = 0;
            if(animator)animator.SetBool("Ristretto",true);
            phaseEffects?.TransitionEnd();
            CurrentAttack = AttackKind.TwinSlash;
            FinishAttack(now, 1.05f);
            return true;
        }

        void SuspendPhaseTransition()
        {
            if (InPhaseTransition && !health.IsDead)
                transitionSuspended = true;
        }

        bool TryChoosePhaseTwo(float distance, float now)
        {
            if (distance > 17 || !transitionStarted) return false;
            float sum = 0;
            for (int i = 0; i < 4; i++)
            {
                float weight = now < phaseReadyAt[i] || i == lastSpecial ? 0 : 1;
                if (i == previousSpecial) weight *= .35f;
                if (i == 0) weight *= distance > 6 ? 2.8f : 1;
                if (i == 1) weight *= distance < 7 ? 2.7f : 0;
                if (i == 2) weight *= distance > 6 && distance < 15 ? 2 : .4f;
                if (i == 3) weight *= distance < 8 ? 1.4f : 0;
                phaseWeights[i] = weight; sum += weight;
            }
            if (sum <= 0) return false;
            float pick = Random.value * sum;
            for (int i = 0; i < 4; i++)
            {
                if (phaseWeights[i] <= 0) continue;
                pick -= phaseWeights[i];
                if (pick > 0) continue;
                BeginPhaseTwoAttack((AttackKind)(i + (int)AttackKind.ShadowChain), now); return true;
            }
            return false;
        }

        void BeginPhaseTwoAttack(AttackKind kind, float now)
        {
            CancelPhaseTwoAction();
            int index = (int)kind - (int)AttackKind.ShadowChain;
            CurrentAttack = kind; CurrentState = State.Windup;
            stateStartedAt = attackStartedAt = now; specialElapsed = specialPrevious = 0;
            specialHitMask = specialCueMask = 0; swingIndex = 0; DamageWindowActive = false;
            attackStartPosition = leapStart = dashOrigin = transform.position;
            specialPreviousPosition = transform.position;
            committedDirection = transform.forward; committedRotation = transform.rotation;
            landingPoint = transform.position;
            previousSpecial = lastSpecial; lastSpecial = index;
            phaseReadyAt[index] = now + PhaseCooldown[index];
            presentation.SetPhaseTwoPose(PhaseClips[index], 0);
            phaseEffects?.AttackCue(kind, false);
            bossPresentation?.Cue(CappuccinoBossPresentation.CueKind.Windup);
        }

        void TickPhaseTwoAttack(float dt, float now)
        {
            int index = (int)CurrentAttack - (int)AttackKind.ShadowChain;
            float duration = PhaseDuration[index];
            specialPrevious = specialElapsed; specialElapsed = Mathf.Max(0, now - attackStartedAt);
            ActionProgress = Mathf.Clamp01(specialElapsed / duration);
            float startup = CurrentAttack == AttackKind.ShadowChain ? .7f : CurrentAttack == AttackKind.MochaDive ? .65f : .85f;
            CurrentState = specialElapsed < startup ? State.Windup : State.Attack;
            DamageWindowActive = false; ShockwaveActive = false;
            SetMovement(0, dt);
            if (specialElapsed < startup * .6f && target)
            {
                Face(Vector3.ProjectOnPlane(target.position-transform.position, Vector3.up), dt*.6f);
                committedDirection = transform.forward; committedRotation = transform.rotation;
            }
            presentation.SetPhaseTwoPose(PhaseClips[index], ActionProgress);
            if (CurrentAttack == AttackKind.ShadowChain) TickShadow(dt);
            else if (CurrentAttack == AttackKind.CremaCyclone)
            {
                float turn = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.85f,2.25f,specialElapsed));
                transform.rotation = Quaternion.AngleAxis(turn * 720, Vector3.up) * committedRotation;
                TickStrikes(CycloneHits, .24f, attackReach*1.1f, 360, .78f);
            }
            else if (CurrentAttack == AttackKind.MochaDive) TickDive(dt);
            else TickTempest(dt);
            specialPreviousPosition = transform.position;
            if (specialElapsed >= duration)
            {
                attackCounter++;
                FinishAttack(now, CurrentAttack == AttackKind.CoffeeTempest ? 1.45f : 1.05f);
            }
        }

        void TickShadow(float dt)
        {
            for (int i = 0; i < ShadowHits.Length; i++)
            {
                float start = ShadowHits[i] - .24f, end = ShadowHits[i] + .17f;
                if (specialPrevious < start && specialElapsed >= start)
                {
                    dashOrigin = transform.position;
                    if (target)
                    {
                        Vector3 aim = Vector3.ProjectOnPlane(target.position-transform.position,Vector3.up);
                        Vector3 inward = aim.sqrMagnitude > .01f ? aim.normalized : transform.forward;
                        // Cut around the target on a safe outer chord. A close target still
                        // produces real lateral footwork instead of three stationary cuts.
                        float flank = i==0?0:(i==1?70:-70)*avoidanceSide;
                        Vector3 offset=Quaternion.AngleAxis(flank,Vector3.up)*(-inward*4.8f);
                        shadowDestination=target.position+offset;shadowDestination.y=transform.position.y;
                        shadowDestination=Vector3.MoveTowards(transform.position,shadowDestination,8.5f);
                        committedDirection=Vector3.ProjectOnPlane(target.position-shadowDestination,Vector3.up).normalized;
                        committedRotation = Quaternion.LookRotation(committedDirection);
                    }
                    else shadowDestination=transform.position;
                }
                if (specialElapsed < start || specialElapsed > end) continue;
                Face(committedDirection,dt*2);
                float phase = Mathf.InverseLerp(start,end,specialElapsed);
                Vector3 travel=Vector3.ProjectOnPlane(shadowDestination-transform.position,Vector3.up);
                Move(travel.normalized,Mathf.Min(28*Mathf.Sin(phase*Mathf.PI),travel.magnitude/dt),dt,1,false);
                phaseEffects?.Afterimage();
            }
            TickStrikes(ShadowHits,.18f,attackReach,100,.7f);
        }

        void TickTempest(float dt)
        {
            TickStrikes(TempestHits,.2f,attackReach*1.05f,140,.7f);
            if (specialElapsed < 3.05f && target && !DamageWindowActive)
            {
                Face(Vector3.ProjectOnPlane(target.position-transform.position,Vector3.up),dt*.28f);
                committedDirection = transform.forward;
            }
            if (specialElapsed >= 3.1f && specialPrevious < 3.1f)
            {
                committedDirection = transform.forward;
                phaseEffects?.AttackCue(CurrentAttack,false);
            }
            if (specialElapsed > 3.65f && specialElapsed < 4.08f)
            {
                float gap = target ? Vector3.ProjectOnPlane(target.position-transform.position,Vector3.up).magnitude : 0;
                Move(committedDirection,Mathf.Min(9*Mathf.Sin(Mathf.InverseLerp(3.65f,4.08f,specialElapsed)*Mathf.PI),Mathf.Max(0,gap-3.5f)/dt),dt,1,false);
                phaseEffects?.Afterimage();
            }
        }

        void TickDive(float dt)
        {
            if (!airTravel && specialElapsed >= .65f && specialElapsed < 1.95f)
            {
                airTravel = true; leapStart = transform.position;
                Vector3 desired = target ? Vector3.MoveTowards(leapStart,target.position-committedDirection*3.2f,11) : leapStart;
                landingPoint = leapStart;
                bool safe = true;
                for (int i=1;i<=8;i++)
                {
                    Vector3 sample=Vector3.Lerp(leapStart,desired,i/8f);
                    if (!SampleGround(sample,out var floor) || Mathf.Abs(floor.y+ .06f-SoleOffset-leapStart.y)>1.5f ||
                        (ocean && floor.y < ocean.oceanHeight+.12f)) { safe=false;break; }
                }
                if(safe && SampleGround(desired,out var ground))landingPoint=new Vector3(desired.x,ground.y+.06f-SoleOffset,desired.z);
            }
            if (airTravel && specialElapsed < 1.95f)
            {
                float height = specialElapsed < 1.05f ? 5.6f*Mathf.Sin(Mathf.InverseLerp(.65f,1.05f,specialElapsed)*Mathf.PI*.5f) :
                    specialElapsed < 1.5f ? 5.6f : 5.6f*(1-Mathf.Pow(Mathf.InverseLerp(1.5f,1.95f,specialElapsed),2));
                Vector3 goal=Vector3.Lerp(leapStart,landingPoint,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1.95f,specialElapsed)))+Vector3.up*height;
                if(motor.enabled)motor.Move(goal-transform.position);
                if(specialElapsed>=1.5f)phaseEffects?.Afterimage();
            }
            if (airTravel && specialElapsed >= 1.95f)
            {
                LandPhaseTwo();landingPoint=transform.position;
                phaseEffects?.Impact(landingPoint);
            }
            TickStrikes(DiveHits,.2f,3.8f,360,.9f);
            if (specialElapsed >= 1.95f && specialElapsed <= 3.1f)
            {
                ShockwaveActive=true;ShockwaveRadius=3.8f+(specialElapsed-1.95f)*8.5f;
                phaseEffects?.Wave(landingPoint,ShockwaveRadius);
                if ((specialHitMask & 2) == 0 && TryPhaseTwoHit(landingPoint,ShockwaveRadius+.5f,360,.75f,ShockwaveRadius-.65f,1.1f))
                    specialHitMask |= 2;
            }
        }

        void TickStrikes(float[] hits,float window,float reach,float arc,float multiplier)
        {
            for(int i=0;i<hits.Length;i++)
            {
                bool active=specialElapsed>=hits[i] && specialElapsed<=hits[i]+window;
                if(!active)continue;
                swingIndex=i;DamageWindowActive=true;
                if((specialCueMask&(1<<i))==0)
                {
                    specialCueMask|=1<<i;bossPresentation?.Cue(CappuccinoBossPresentation.CueKind.Swing);
                    phaseEffects?.AttackCue(CurrentAttack,true);
                }
                if((specialHitMask&(1<<i))==0 && TryPhaseTwoHit(transform.position,reach,arc,multiplier))specialHitMask|=1<<i;
            }
        }

        bool TryPhaseTwoHit(Vector3 origin,float reach,float arc,float multiplier,float inner=0,float height=4)
        {
            if(!targetHealth || targetHealth.currentHealth<=0 || targetHealth.IsProtected)return false;
            var body=targetHealth.GetComponent<CharacterController>();
            Vector3 point=body&&body.enabled?body.ClosestPoint(origin+Vector3.up*2):target.position+Vector3.up;
            Vector3 delta=Vector3.ProjectOnPlane(point-origin,Vector3.up);
            float feet=body&&body.enabled?body.bounds.min.y:target.position.y;
            if(delta.magnitude>reach||delta.magnitude<Mathf.Max(0,inner)||Mathf.Abs(feet-origin.y)>height)return false;
            if(arc<359&&Vector3.Angle(committedDirection,delta)>arc*.5f)return false;
            if(IsBlocked(origin+Vector3.up*2,point))return false;
            // ApplyMeleeDamage owns guard, perfect dodge, shared damage recovery and hit feedback.
            bool applied=targetHealth.ApplyMeleeDamage(damage*attackDamageScale*multiplier,point,transform.position);
            if(applied)
            {
                targetHealth.GetComponent<CombatKnockback>()?.Push(delta.normalized*.8f);
                bossPresentation?.Cue(CappuccinoBossPresentation.CueKind.Hit);
            }
            return true;
        }

        void LandPhaseTwo()
        {
            if(!airTravel)return;
            airTravel=false;
            if(motor&&motor.enabled&&FindGround(transform.position,2,30,out var floor))
                motor.Move(Vector3.up*(floor.point.y+.06f-SoleOffset-transform.position.y));
            verticalSpeed=-2;
        }
        internal void PreparePhaseTwoDeath() => LandPhaseTwo();

        void CancelPhaseTwoAction()
        {
            LandPhaseTwo();DamageWindowActive=false;ShockwaveActive=false;ShockwaveRadius=0;
            phaseEffects?.ClearAction();
        }
        void ResetPhaseTwo()
        {
            transitionStarted=transitionSuspended=transitionReleased=false;transitionElapsed=0;
            if(animator)animator.SetBool("Ristretto",false);
            lastSpecial=previousSpecial=-1;CurrentAttack=AttackKind.TwinSlash;
            for(int i=0;i<4;i++)phaseReadyAt[i]=0;
            phaseEffects?.EndEncounter();
        }

        bool ForecastPhaseTwoDodge(PlayerHealth player,float horizon,out PerfectDodgeThreat threat)
        {
            threat=default;
            if(!FightActive||health.IsDead||!isActiveAndEnabled||CurrentState!=State.Windup&&CurrentState!=State.Attack)return false;
            var hits=CurrentAttack==AttackKind.ShadowChain?ShadowHits:CurrentAttack==AttackKind.CremaCyclone?CycloneHits:CurrentAttack==AttackKind.MochaDive?DiveHits:TempestHits;
            for(int i=0;i<hits.Length;i++)
            {
                float seconds=hits[i]-(Time.time-attackStartedAt);
                if(seconds<-.02f||(specialHitMask&(1<<i))!=0)continue;
                Vector3 origin=CurrentAttack==AttackKind.MochaDive?landingPoint:transform.position;
                float arc=CurrentAttack==AttackKind.CremaCyclone||CurrentAttack==AttackKind.MochaDive?360:CurrentAttack==AttackKind.ShadowChain?100:140;
                float reach=CurrentAttack==AttackKind.MochaDive?3.8f:attackReach*(CurrentAttack==AttackKind.CremaCyclone?1.1f:1);
                if(CurrentAttack==AttackKind.ShadowChain&&specialElapsed>=hits[i]-.24f)
                    origin=Vector3.MoveTowards(origin,shadowDestination,Mathf.Max(0,seconds)*28);
                return PerfectDodgeThreat.Sector(this,transform,player,Mathf.Max(0,seconds),horizon,origin,committedDirection,reach,arc,4,
                    PerfectDodgeThreat.Stamp(this,attackStartedAt,i),out threat);
            }
            if(CurrentAttack==AttackKind.MochaDive&&(specialHitMask&2)==0)
            {
                float distance=Vector3.ProjectOnPlane(player.transform.position-landingPoint,Vector3.up).magnitude;
                float time=1.95f+Mathf.Max(0,distance-3.8f)/8.5f-(Time.time-attackStartedAt);
                return PerfectDodgeThreat.Sector(this,transform,player,time,horizon,landingPoint,Vector3.forward,13.6f,360,1.1f,
                    PerfectDodgeThreat.Stamp(this,attackStartedAt,1),out threat);
            }
            return false;
        }
    }
}
