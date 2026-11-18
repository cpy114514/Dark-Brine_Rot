using UnityEngine;

public sealed class HuangMeiImpactPulse : MonoBehaviour
{
    Transform[] rings;
    Renderer[] renderers;
    MaterialPropertyBlock block;
    Vector3 anchor;
    float elapsed=1,radius,inner;
    public void Initialize(Transform[] shapes,Renderer[] visuals){rings=shapes;renderers=visuals;block=new MaterialPropertyBlock();Clear();}
    public void Play(Vector3 point,float radius,float inner){anchor=point;this.radius=radius;this.inner=inner;elapsed=0;foreach(var r in renderers)r.enabled=true;Tick();}
    void Update(){if(elapsed>=.48f)return;elapsed+=Time.deltaTime;Tick();}
    void Tick()
    {
        if(rings==null||renderers==null)return;
        block??=new MaterialPropertyBlock();
        transform.SetPositionAndRotation(anchor,Quaternion.Euler(90,0,0));
        float t=Mathf.Clamp01(elapsed/.48f);
        for(int i=0;i<rings.Length;i++)
        {
            if(!rings[i]||!renderers[i])continue;
            float scale=i==0?Mathf.Lerp(Mathf.Max(.5f,inner),radius,1-Mathf.Pow(1-t,3)):Mathf.Lerp(Mathf.Max(.35f,inner),radius*.78f,t);
            rings[i].localScale=Vector3.one*scale;renderers[i].enabled=t<1;
            var color=new Color(1.5f,1,.35f)*(1-t);block.SetColor("_BaseColor",color);block.SetColor("_EmissionColor",color*.7f);renderers[i].SetPropertyBlock(block);
        }
    }
    public void Clear(){elapsed=1;if(renderers!=null)foreach(var r in renderers)if(r)r.enabled=false;}
}
