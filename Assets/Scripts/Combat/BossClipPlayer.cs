using System.Collections.Generic;
using UnityEngine;

namespace Mavis
{
    /// <summary>Samples baked, rig-specific authored clips on the AI's action clock.</summary>
    public sealed class BossClipPlayer
    {
        readonly Animation animation;
        readonly Dictionary<string,AnimationState> states=new Dictionary<string,AnimationState>();
        AnimationState current,previous;
        float blend;
        public bool Ready=>states.Count>=5;
        public BossClipPlayer(GameObject model,string prefix)
        {
            var clips=Resources.LoadAll<AnimationClip>("SecondIslandMotion");
            foreach(var clip in clips)if(clip.name.StartsWith(prefix)&&clip.legacy)
            {
                if(!animation)
                {
                    var animator=model.GetComponent<Animator>();if(animator)animator.enabled=false;
                    animation=model.GetComponent<Animation>();if(!animation)animation=model.AddComponent<Animation>();
                    animation.playAutomatically=false;animation.cullingType=AnimationCullingType.AlwaysAnimate;
                }
                string key=clip.name.Substring(prefix.Length);animation.AddClip(clip,key);
                var state=animation[key];state.speed=0;state.wrapMode=clip.wrapMode;states[key]=state;
            }
        }
        public float Length(string key)=>states.TryGetValue(key,out var state)?state.length:1;
        public void Sample(string key,float time,float delta)
        {
            if(!animation||!states.TryGetValue(key,out var next))return;
            if(next!=current)
            {
                if(previous!=null)previous.enabled=false;
                previous=current;current=next;current.enabled=true;blend=previous==null?1:0;
            }
            blend=Mathf.MoveTowards(blend,1,Mathf.Max(0,delta)/.14f);
            current.time=current.wrapMode==WrapMode.Loop?Mathf.Repeat(time,current.length):Mathf.Clamp(time,0,current.length);
            current.weight=blend;
            if(previous!=null){previous.weight=1-blend;if(blend>=1){previous.enabled=false;previous=null;}}
            animation.Sample();
        }
    }
}
