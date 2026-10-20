using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Mavis
{
    /// <summary>Retargeted CC0 humanoid clips blended over Nailong's existing idle pose.</summary>
    public sealed class NailongPhaseAnimation : MonoBehaviour
    {
        Animator animator;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable action;
        float cueSeconds, authoredCue, clipSeconds;
        float cueHoldSeconds;
        float began, duration;
        public bool IsPlaying=>graph.IsValid();
        public bool Play(string clipName,float seconds,float releaseSeconds=-1,float releaseNormalized=-1,float holdSeconds=0)
        {
            Stop();
            animator=GetComponentInChildren<Animator>();
            var clip=Resources.Load<AnimationClip>(clipName) ?? Resources.Load<AnimationClip>("BossPhases/"+clipName);
            if(!animator || !animator.isHuman || !animator.avatar || !animator.avatar.isValid || !clip || !clip.humanMotion)return false;
            graph=PlayableGraph.Create("Nailong phase animation");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer=AnimationMixerPlayable.Create(graph,2);
            var controller=AnimatorControllerPlayable.Create(graph,animator.runtimeAnimatorController);
            action=AnimationClipPlayable.Create(graph,clip);
            cueSeconds=releaseSeconds;authoredCue=releaseNormalized*clip.length;clipSeconds=clip.length;
            cueHoldSeconds=Mathf.Max(0,holdSeconds);
            action.SetApplyFootIK(true);action.SetSpeed(releaseSeconds>0?0:clip.isLooping?1:clip.length/Mathf.Max(.1f,seconds));
            graph.Connect(controller,0,mixer,0);graph.Connect(action,0,mixer,1);
            mixer.SetInputWeight(0,1);mixer.SetInputWeight(1,0);
            AnimationPlayableOutput.Create(graph,"Nailong imported action",animator).SetSourcePlayable(mixer);
            began=Time.time;duration=Mathf.Max(.1f,seconds);graph.Play();return true;
        }
        void Update()
        {
            if(!graph.IsValid())return;
            float age=Time.time-began;
            if(age>=duration){Stop();return;}
            if(cueSeconds>0)action.SetTime(MapCueTime(age,duration,cueSeconds,clipSeconds,authoredCue,cueHoldSeconds));
            float blend=Mathf.Min(Mathf.Clamp01(age/.12f),Mathf.Clamp01((duration-age)/.15f));
            mixer.SetInputWeight(0,1-blend);mixer.SetInputWeight(1,blend);
        }
        public static float MapCueTime(float age,float seconds,float cue,float length,float sourceCue,float holdSeconds=0)
        {
            cue=Mathf.Clamp(cue,.01f,seconds-.01f);sourceCue=Mathf.Clamp(sourceCue,0,length);
            float hold=Mathf.Clamp(holdSeconds,0,Mathf.Max(0,seconds-cue-.01f));
            return age<=cue?Mathf.Clamp01(age/cue)*sourceCue:
                Mathf.Lerp(sourceCue,length,Mathf.Clamp01((age-cue-hold)/(seconds-cue-hold)));
        }
        public void Stop()
        {
            if(!graph.IsValid())return;
            graph.Destroy();
            // The mixer has already faded to the live controller. Rebind here
            // reset the entire rig and produced a visible snap on every recovery.
        }
        void OnDisable()=>Stop();
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
    }
}
