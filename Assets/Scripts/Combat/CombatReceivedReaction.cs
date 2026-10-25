using UnityEngine;

namespace Mavis
{
    // The authored controller layer keeps state parameters and root motion
    // owned by the original Animator, including after equipment switches.
    [DisallowMultipleComponent,DefaultExecutionOrder(850)]
    public sealed class CombatReceivedReaction : MonoBehaviour
    {
        const string Layer="Combat Received Hit";
        static readonly int Phase=Animator.StringToHash("ReceivedHitPhase");
        Animator animator;
        RuntimeAnimatorController bound;
        ThirdPersonPlayerController movement;
        int layer=-1;
        float elapsed=10,seconds=.36f,weight;
        public bool IsReacting => elapsed<seconds;
        public float Weight => animator&&layer>=0&&layer<animator.layerCount?animator.GetLayerWeight(layer):0;
        void Awake()=>movement=GetComponent<ThirdPersonPlayerController>();
        bool Prepare()
        {
            var current=movement?movement.CharacterAnimator:GetComponentInChildren<Animator>();
            if(!current||!current.isHuman||!current.runtimeAnimatorController)return false;
            if(animator!=current||bound!=current.runtimeAnimatorController)
            {
                Clear();animator=current;bound=current.runtimeAnimatorController;layer=animator.GetLayerIndex(Layer);
            }
            return layer>=0;
        }
        public void Hit(float strength)
        {
            if(!isActiveAndEnabled || movement&&movement.ExternalControlLock || !Prepare())return;
            var profile=CombatFeelProfile.Default;seconds=profile.receivedReactionSeconds;
            weight=Mathf.Clamp01(profile.receivedReactionWeight*strength);elapsed=0;
            animator.SetFloat(Phase,.035f/.75f);animator.SetLayerWeight(layer,0);
        }
        void Update()
        {
            if(!Prepare())return;
            if(movement&&movement.ExternalControlLock){elapsed=seconds;animator.SetLayerWeight(layer,0);return;}
            if(!IsReacting){animator.SetLayerWeight(layer,0);return;}
            if(Time.timeScale<=0||CombatLocalHitStop.Active(gameObject))return;
            elapsed=Mathf.Min(seconds,elapsed+Time.deltaTime);float t=elapsed/Mathf.Max(.01f,seconds);
            animator.SetFloat(Phase,Mathf.Lerp(.035f,.60f,t)/.75f);
            animator.SetLayerWeight(layer,weight*Mathf.SmoothStep(0,1,elapsed/.035f)*Mathf.Pow(1-t,1.25f));
        }
        void Clear()
        {
            if(animator&&animator.runtimeAnimatorController==bound&&layer>=0&&layer<animator.layerCount)animator.SetLayerWeight(layer,0);
            elapsed=seconds;
        }
        void OnDisable()=>Clear();
        void OnDestroy()=>Clear();
    }
}
