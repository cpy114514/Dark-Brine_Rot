using UnityEngine;

/// <summary>Reads encounter presentation state; never changes AI, damage or attack clocks.</summary>
public sealed class SecondIslandBossAudio : MonoBehaviour
{
    HuangMeiBoss huang;ZhuBaJieBoss bajie;int previousState=-1;float previousPhase;
    void Awake(){huang=GetComponent<HuangMeiBoss>();bajie=GetComponent<ZhuBaJieBoss>();}
    void LateUpdate()
    {
        var mix=SecondIslandAudio.Current;if(!mix||Time.timeScale<=0)return;
        int state=huang?(int)huang.CurrentState:bajie?(int)bajie.CurrentState:-1;
        if(state!=previousState){
            if(huang){if(huang.CurrentState==HuangMeiBoss.State.Windup)mix.PlayWorld("HuangWarning",transform.position+Vector3.up*2,.49f);else if(previousState>=0&&huang.CurrentState==HuangMeiBoss.State.Dead)mix.PlayWorld("BossDefeat",transform.position,.42f);}
            if(bajie){if(bajie.CurrentState==ZhuBaJieBoss.State.Windup)mix.PlayWorld("BajieWarning",transform.position+Vector3.up,.53f);
                else if(bajie.CurrentState==ZhuBaJieBoss.State.Attack)mix.PlayWorld(bajie.CurrentMove==ZhuBaJieBoss.MoveKind.Charge?"BajieCharge":"RakeSwing",transform.position+Vector3.up,.52f);
                else if(bajie.CurrentState==ZhuBaJieBoss.State.Transform)mix.PlayWorld("SealImpact",transform.position,.48f);
                else if(previousState>=0&&bajie.CurrentState==ZhuBaJieBoss.State.Dead)mix.PlayWorld("BossDefeat",transform.position,.45f);}
            previousState=state;previousPhase=0;
        }
        if(bajie&&bajie.CurrentState==ZhuBaJieBoss.State.Attack){if(bajie.CurrentMove==ZhuBaJieBoss.MoveKind.RakeCombo&&previousPhase<.63f&&bajie.ActionProgress>=.63f)mix.PlayWorld("RakeSwing",transform.position+Vector3.up,.43f);previousPhase=bajie.ActionProgress;}
    }
}
