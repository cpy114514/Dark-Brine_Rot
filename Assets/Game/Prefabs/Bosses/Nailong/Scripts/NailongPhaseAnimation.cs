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
        float began, duration;
        public bool IsPlaying=>graph.IsValid();
        public bool Play(string clipName,float seconds)
        {
            Stop();
            animator=GetComponentInChildren<Animator>();
            var clip=Resources.Load<AnimationClip>("BossPhases/"+clipName);
            if(!animator || !animator.isHuman || !animator.avatar || !animator.avatar.isValid || !clip || !clip.humanMotion)return false;
            graph=PlayableGraph.Create("Nailong phase animation");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer=AnimationMixerPlayable.Create(graph,2);
            var controller=AnimatorControllerPlayable.Create(graph,animator.runtimeAnimatorController);
            var action=AnimationClipPlayable.Create(graph,clip);
            action.SetApplyFootIK(true);action.SetSpeed(clip.isLooping?1:clip.length/Mathf.Max(.1f,seconds));
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
            float blend=Mathf.Min(Mathf.Clamp01(age/.12f),Mathf.Clamp01((duration-age)/.15f));
            mixer.SetInputWeight(0,1-blend);mixer.SetInputWeight(1,blend);
        }
        public void Stop()
        {
            if(!graph.IsValid())return;
            graph.Destroy();
            if(animator && animator.isActiveAndEnabled){animator.Rebind();animator.Update(0);}
        }
        void OnDisable()=>Stop();
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
    }
}
