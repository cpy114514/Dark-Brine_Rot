using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Read the real scattered wreck during a native take. Never move a fragment
// or manufacture a landing board for a preview.
[DefaultExecutionOrder(2600)]
public sealed class V016HopEligibilityCapture : MonoBehaviour
{
    bool captured;
    void LateUpdate()
    {
        var film=UnityEngine.Object.FindFirstObjectByType<Story1WreckFinale>();
        if(captured || film==null || film.CinematicTime<4.05f || film.CinematicTime>4.5f)return;
        captured=true;
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        object Field(string name)=>typeof(Story1WreckFinale).GetField(name,flags).GetValue(film);
        var current=(Story1WreckPlank)Field("board");var hero=(ThirdPersonPlayerController)Field("hero");
        Vector3 feet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
        var rows=new System.Collections.Generic.List<object>();
        var nearby=UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None)
            .Where(p=>p!=current).OrderBy(p=>Vector3.ProjectOnPlane(p.BoardingPoint(feet)-feet,Vector3.up).magnitude).Take(8);
        float oldPose=(float)Field("chosenStancePoseCost"),oldWood=(float)Field("chosenStanceWoodPenetration");
        int oldCount=(int)Field("stanceCandidateCount");
        foreach(var board in nearby)
        {
            Vector3 nearest=board.BoardingPoint(feet);float gap=Vector3.ProjectOnPlane(nearest-feet,Vector3.up).magnitude,dy=Mathf.Abs(nearest.y-feet.y);
            bool eligible=board.IsBoardable && board.Dimensions.y<5 && board.Speed<5 && Mathf.Min(board.Dimensions.x,board.Dimensions.z)>1.6f && gap<12 && dy<1.5f;
            float wood=float.PositiveInfinity,pose=float.PositiveInfinity;int candidates=0;
            Vector3 landing=nearest;
            if(eligible)
            {
                landing=(Vector3)typeof(Story1WreckFinale).GetMethod("FindEntryEdge",flags,null,new[]{typeof(Story1WreckPlank),typeof(Vector3),typeof(Vector3?)},null).Invoke(film,new object[]{board,nearest,(Vector3?)feet});
                wood=(float)Field("chosenStanceWoodPenetration");pose=(float)Field("chosenStancePoseCost");candidates=(int)Field("stanceCandidateCount");
            }
            rows.Add(new{board=board.name,board.IsBoardable,gap,dy,board.Speed,size=board.Dimensions.ToString(),eligible,landing=landing.ToString(),candidates,wood,pose});
        }
        typeof(Story1WreckFinale).GetField("chosenStancePoseCost",flags).SetValue(film,oldPose);
        typeof(Story1WreckFinale).GetField("chosenStanceWoodPenetration",flags).SetValue(film,oldWood);
        typeof(Story1WreckFinale).GetField("stanceCandidateCount",flags).SetValue(film,oldCount);
        Directory.CreateDirectory(".codex/encounter-v016");
        File.WriteAllText(".codex/encounter-v016/hop-eligibility-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{film.CinematicTime,current=current.name,feet=feet.ToString(),rows},Newtonsoft.Json.Formatting.Indented));
        UnityEngine.Object.Destroy(gameObject);
    }
}
public static class AuditHopEligibilityV016
{
    public static object Start()
    {
        if(!Application.isPlaying)throw new Exception("Play mode required.");
        var go=new GameObject("Native real wreck route audit");UnityEngine.Object.DontDestroyOnLoad(go);go.AddComponent<V016HopEligibilityCapture>();return "Observing actual wreck, no fragment mutation.";
    }
}
