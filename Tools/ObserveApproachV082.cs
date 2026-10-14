using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

// Read-only observation of actual freely scattered supports and solved poses.
[DefaultExecutionOrder(32600)]
public sealed class ApproachObserverV082 : MonoBehaviour
{
    public string output;
    readonly List<object> samples=new List<object>();
    readonly List<object> neighbours=new List<object>();
    readonly bool[] captured=new bool[4];
    static readonly float[] clocks={1.6f,4.4f,8.6f,11.4f};
    static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    Story1WreckFinale film;
    float next=-2;
    object Field(string name)=>typeof(Story1WreckFinale).GetField(name,flags).GetValue(film);
    static float[] Point(Vector3 p)=>new[]{p.x,p.y,p.z};
    void LateUpdate()
    {
        var battle=FindFirstObjectByType<Story1WreckBattle>();
        if(battle!=null && battle.Finale!=null)film=battle.Finale;
        if(film==null || film.CinematicTime<-2)return;
        var hero=(ThirdPersonPlayerController)Field("hero");
        var shark=(TralaleroSwimAnimator)Field("shark");
        var board=(Story1WreckPlank)Field("board");
        Vector3 feet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
        if(film.CinematicTime>=next)
        {
            next=film.CinematicTime+.06f;
            var plans=(Array)Field("hopPlans");
            string Target(int i)
            {
                object plan=plans.GetValue(i);
                var target=(Story1WreckPlank)plan.GetType().GetField("target").GetValue(plan);
                return target!=null?target.name:null;
            }
            samples.Add(new{clock=film.CinematicTime,film.CurrentBeat,board=board!=null?board.name:null,
                firstTarget=Target(0),secondTarget=Target(1),planningBusy=Field("hopPlanningBusy"),
                attackKey=Field("attackFacingKey"),biteStartAge=Field("biteRouteStartAge"),
                root=Point(shark.transform.position),intendedRoot=Point((Vector3)Field("intendedBiteRoot")),
                nose=Point(shark.NoseWorldPoint),tail=Point(shark.TailWorldPoint),heading=Point(shark.transform.eulerAngles),
                intendedTail=Point((Vector3)Field("intendedTailRoot")),tailStartAge=Field("tailRouteStartAge"),
                approachStart=Point((Vector3)Field("attackStartRoot")),aim=Point((Vector3)Field("attackAim")),
                routeOffset=Point((Vector3)Field("attackRouteOffset")),routeCost=Field("attackRouteCost"),
                entryGoal=board!=null ? Point(board.transform.TransformPoint((Vector3)Field("entrySharkExitRootLocal"))) : null,
                correction=Point(battle.WreckContact.LastCorrection),feet=Point(feet),
                action=shark.CinematicAction,actionTime=shark.CinematicActionSeconds,
                film.BoardJumpCount,film.BiteRouteReplans,film.TailRouteReplans});
        }
        for(int i=0;i<clocks.Length;i++)
        {
            if(captured[i] || film.CinematicTime<clocks[i])continue;
            captured[i]=true;
            var rows=battle.Boards.Where(p=>p!=board && p.gameObject.activeInHierarchy)
                .Select(p=>new{piece=p,point=p.BoardingPoint(feet)})
                .OrderBy(r=>Vector3.ProjectOnPlane(r.point-feet,Vector3.up).magnitude).Take(10)
                .Select(r=>new{board=r.piece.name,r.piece.IsBoardable,r.piece.Speed,
                    size=Point(r.piece.Dimensions),gap=Vector3.ProjectOnPlane(r.point-feet,Vector3.up).magnitude,
                    heightGap=Mathf.Abs(r.point.y-feet.y)}).ToArray();
            neighbours.Add(new{clock=film.CinematicTime,rows});
        }
        if(film.IsComplete || battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)Destroy(this);
    }
    void OnDestroy()
    {
        if(string.IsNullOrEmpty(output))return;
        File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{readOnly=true,samples,neighbours},Newtonsoft.Json.Formatting.Indented));
    }
}
public static class ObserveApproachV082
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only approach and real neighbour observer v082");
        UnityEngine.Object.DontDestroyOnLoad(holder);holder.AddComponent<ApproachObserverV082>().output=output;
        return new{started=true,readOnly=true};
    }
}
