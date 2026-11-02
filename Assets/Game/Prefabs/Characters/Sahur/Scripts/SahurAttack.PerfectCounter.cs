using UnityEngine;
namespace Mavis
{
    public partial class SahurAttack
    {
        static readonly int PerfectCounterHash=Animator.StringToHash("Base Layer.Perfect Counter");
        static readonly int BladePerfectCounterHash=Animator.StringToHash("Base Layer.Capri Perfect Counter");
        public bool IsPerfectCounterActive=>attacking&&(activeAttackHash==PerfectCounterHash||activeAttackHash==BladePerfectCounterHash);
        public int PerfectCountersPerformed { get; private set; }
        Transform perfectCounterTarget;
        Quaternion perfectCounterStart;
        public bool BeginPerfectCounter(Transform target)
        {
            int hash=UsesTwinBlades?BladePerfectCounterHash:PerfectCounterHash;
            if(!animator||!animator.HasState(0,hash)||!controller||!controller.CanUseGroundAttack||
               !PerfectDodgeThreats.Alive(target)||ControlInputBlocked||CinematicHandoffPending||Time.timeScale<=0||
               PauseSettingsMenu.IsOpen||SahurLoadoutUI.BlocksInput||charging||IsSpinning||IsGuarding||ChordHeld||GameControls.Held(GameControls.Command.Charge)||boomerang&&boomerang.IsBusy)return false;
            Vector3 to=Vector3.ProjectOnPlane(target.position-transform.position,Vector3.up);
            if(to.sqrMagnitude>625||!PerfectDodgeThreats.ClearPath(transform,target,transform.position+Vector3.up,target.position+Vector3.up))return false;
            if(stamina&&!stamina.TrySpend(stamina.lightAttackCost))return false;
            CancelForMovement();perfectCounterTarget=target;perfectCounterStart=transform.rotation;
            controller.SetPerfectCounterFacing(perfectCounterStart);
            BeginAttack(hash,damage,0,.055f);PerfectCountersPerformed++;return true;
        }
        void UpdatePerfectCounter()
        {
            if(!IsPerfectCounterActive&&(ChordHeld||GameControls.Held(GameControls.Command.Charge)))controller?.ClearPerfectCounter();
            else controller?.FlushPerfectCounter(this);
            if(!IsPerfectCounterActive||!perfectCounterTarget||!controller||PauseSettingsMenu.IsOpen||Time.timeScale<=0)return;
            float age=Time.time-attackStartedAt;if(age>.12f)return;
            Vector3 toward=Vector3.ProjectOnPlane(perfectCounterTarget.position-transform.position,Vector3.up);
            if(toward.sqrMagnitude>.001f)
                controller.SetPerfectCounterFacing(Quaternion.Slerp(perfectCounterStart,Quaternion.LookRotation(toward),Mathf.SmoothStep(0,1,age/.10f)));
        }
    }
}
