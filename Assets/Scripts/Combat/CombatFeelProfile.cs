using System;
using UnityEngine;

namespace Mavis
{
    [CreateAssetMenu(menuName = "Dark Brine/Combat Feel Profile")]
    public sealed class CombatFeelProfile : ScriptableObject
    {
        [Serializable]
        public struct Impact
        {
            [Range(0, .13f)] public float stop;
            [Range(0, .15f)] public float cameraStrength;
            [Range(.03f, .25f)] public float cameraDuration;
            [Range(0, 2)] public float vfx;
            [Range(0, 2)] public float reaction;
            [Range(0, 2)] public float knockback;
            public Impact(float s, float c, float d, float v, float r, float k)
            { stop=s; cameraStrength=c; cameraDuration=d; vfx=v; reaction=r; knockback=k; }
        }
        public Impact normal = new Impact(.048f,.035f,.105f,.75f,1,.13f);
        public Impact second = new Impact(.038f,.03f,.095f,.7f,.9f,.1f);
        public Impact finisher = new Impact(.105f,.075f,.16f,1.1f,1.5f,.4f);
        public Impact heavy = new Impact(.09f,.09f,.17f,1.25f,1.8f,.65f);
        [Header("Received damage and rifle feedback")]
        public Impact received = new Impact(.055f,.075f,.15f,.95f,1,0);
        public Impact receivedHeavy = new Impact(.085f,.10f,.19f,1.2f,1.25f,0);
        public Impact rifle = new Impact(.028f,.025f,.075f,.7f,.65f,0);
        [Header("Combat forgiveness (hostile attacks only)")]
        [Range(.25f,1f)] public float incomingDamageMultiplier = .7f;
        [Range(.1f,.5f)] public float maximumHitHealthFraction = .18f;
        [Range(.15f,1f)] public float damageRecoverySeconds = .6f;
        [Range(0,.2f)] public float dodgeProtectionStart = .04f;
        [Range(.4f,.9f)] public float dodgeProtectionEnd = .72f;
        [Header("Authored received-hit overlay")]
        public AnimationClip receivedHitClip;
        [Range(.2f,.6f)] public float receivedReactionSeconds = .36f;
        [Range(.2f,1f)] public float receivedReactionWeight = .85f;
        [Header("Response and motion (multipliers on authored clips)")]
        [Range(.08f,.4f)] public float inputBuffer = .22f;
        [Range(.03f,.15f)] public float comboTransition = .07f;
        [Range(.65f,.98f)] public float comboRecoveryChain = .9f;
        [Range(.6f,1)] public float lightDodgeCancel = .9f;
        [Range(.45f,1)] public float heavyDodgeCancel = .62f;
        [Range(.04f,.18f)] public float trailLifetime = .095f;
        public AnimationCurve lightSpeed = new AnimationCurve(new Keyframe(0,1.08f),new Keyframe(.35f,1.05f),new Keyframe(.53f,1.6f),new Keyframe(.7f,1.8f),new Keyframe(.88f,.85f),new Keyframe(1,1));
        public AnimationCurve heavySpeed = new AnimationCurve(new Keyframe(0,.8f),new Keyframe(.6f,1.9f),new Keyframe(1,2.45f));
        [Header("Accessibility")]
        [Range(0,1)] public float cameraMotion = 1;
        [Range(0,1)] public float impactBrightness = .8f;
        [Header("Layered sound (assigned clips, no synthesis on impact)")]
        public AudioClip[] contact;
        public AudioClip[] wood;
        public AudioClip[] metal;
        public AudioClip[] flesh;
        public AudioClip[] lowImpact;
        public AudioClip[] enemyReaction;
        [Range(0,1)] public float soundVolume = .7f;
        public Impact For(CombatHitKind kind) => kind==CombatHitKind.Rifle ? rifle : kind==CombatHitKind.ChargedHeavy || kind==CombatHitKind.JumpSlash ? heavy : kind==CombatHitKind.ComboFinisher ? finisher : kind==CombatHitKind.ComboTwo ? second : normal;
        static CombatFeelProfile cached;
        public static CombatFeelProfile Default
        {
            get
            {
                if (!cached) cached=Resources.Load<CombatFeelProfile>("Combat/SahurCombatFeel");
                if (!cached) { cached=CreateInstance<CombatFeelProfile>(); cached.hideFlags=HideFlags.HideAndDontSave; }
                return cached;
            }
        }
    }
    public enum CombatHitResult { Hit, HeavyHit, Blocked, Unflinching }
    public enum CombatMaterial { Flesh, Wood, Metal }
    public readonly struct CombatHitEvent
    {
        public readonly GameObject attacker;
        public readonly Component target;
        public readonly Vector3 point, direction;
        public readonly CombatHitKind kind;
        public readonly CombatHitResult result;
        public readonly float damage;
        public CombatHitEvent(GameObject a, Component t, Vector3 p, Vector3 d, CombatHitKind k, CombatHitResult r, float value)
        { attacker=a;target=t;point=p;direction=d;kind=k;result=r;damage=value; }
    }
    // Optional victim contract. Existing health/poise and cinematic movement remain authoritative.
    public interface ICombatHitResolver
    {
        CombatHitResult ResolveCombatHit(GameObject attacker, float damage, Vector3 point, CombatHitKind kind);
    }
}
