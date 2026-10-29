using UnityEngine;

/// <summary>Slow floating motion for the authored candy clouds; independent of player movement.</summary>
public sealed class MarshmallowCloudMotion : MonoBehaviour
{
    [Min(0f)] public float driftDistance = 8f;
    Transform[] clouds;
    Vector3[] positions, scales;
    float started;
    void OnEnable()
    {
        if (!Application.isPlaying) return;
        started=Time.time;
        int count=transform.childCount;
        clouds=new Transform[count];positions=new Vector3[count];scales=new Vector3[count];
        for(int i=0;i<count;i++)
        { clouds[i]=transform.GetChild(i);positions[i]=clouds[i].localPosition;scales[i]=clouds[i].localScale; }
    }
    void Update()
    {
        if(clouds==null || PauseSettingsMenu.IsOpen) return;
        float t=Time.time-started;
        for(int i=0;i<clouds.Length;i++)
        {
            if(!clouds[i])continue;
            float phase=i*2.39996f;
            var motion=new Vector3(Mathf.Sin(t*.035f+phase)-Mathf.Sin(phase),
                (Mathf.Sin(t*.065f+phase)-Mathf.Sin(phase))*.45f,
                (Mathf.Cos(t*.027f+phase)-Mathf.Cos(phase))*.65f)*driftDistance;
            clouds[i].localPosition=positions[i]+motion;
            clouds[i].localScale=scales[i]*(1+Mathf.Sin(t*.11f+phase)*.003f);
        }
    }
    void OnDisable()
    {
        if(clouds==null)return;
        for(int i=0;i<clouds.Length;i++)if(clouds[i])
        { clouds[i].localPosition=positions[i];clouds[i].localScale=scales[i]; }
    }
}
