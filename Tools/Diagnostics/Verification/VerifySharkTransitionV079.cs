using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class VerifySharkTransitionV079
{
    public static object Check()
    {
        if(!Application.isPlaying)throw new Exception("Requires original Play Mode");
        var original=UnityEngine.Object.FindObjectsByType<TralaleroSwimAnimator>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault();
        if(original==null)throw new Exception("No native shark in scene");
        var fixture=UnityEngine.Object.Instantiate(original.gameObject);
        fixture.name="Isolated native shark transition v079";fixture.transform.position+=Vector3.up*10000;fixture.SetActive(true);
        try
        {
            foreach(var behaviour in fixture.GetComponentsInChildren<MonoBehaviour>())behaviour.enabled=false;
            foreach(var body in fixture.GetComponentsInChildren<Rigidbody>())body.isKinematic=true;
            foreach(var collider in fixture.GetComponentsInChildren<Collider>())collider.enabled=false;
            var shark=fixture.GetComponent<TralaleroSwimAnimator>();
            if(!shark.UsesBlenderRig)throw new Exception("Requires preserved Blender anatomy");
            var bones=shark.BoneAnimator.GetComponentsInChildren<Transform>(true);
            var rows=new System.Collections.Generic.List<object>();
            float maxJoin=0,maxPositionJoin=0,maxSettled=0;
            foreach(var pair in new[]{("Breach",3.19f,"Tail_Strike"),("Bite_Lunge",.52f,"Hit_Recoil"),("Ship_Smash",1.1f,"Swim")})
            {
                shark.ReleaseCinematicAnimation();shark.SampleCinematicAction(pair.Item1,pair.Item2);shark.CommitCinematicPose(0); shark.SampleCinematicAction(pair.Item1,Mathf.Max(0,pair.Item2-.17f)); shark.RecordResolvedCinematicPose();
                var prior=bones.Select(b=>b.localRotation).ToArray();var positions=bones.Select(b=>b.localPosition).ToArray();
                // Hypothetical planner sampling must not change the committed
                // rendered pose used as the next action's visual starting pose.
                shark.SampleCinematicAction("Bite_Lunge",.52f);shark.SampleCinematicAction("Tail_Strike",.65f);
                shark.SampleCinematicAction(pair.Item3,0);shark.CommitCinematicPose(1);
                float join=0,positionJoin=0;
                for(int i=0;i<bones.Length;i++){join=Mathf.Max(join,Quaternion.Angle(prior[i],bones[i].localRotation));positionJoin=Mathf.Max(positionJoin,Vector3.Distance(positions[i],bones[i].localPosition));}
                maxJoin=Mathf.Max(maxJoin,join);maxPositionJoin=Mathf.Max(maxPositionJoin,positionJoin);
                shark.SampleCinematicAction(pair.Item3,.2f);var target=bones.Select(b=>b.localRotation).ToArray();
                shark.CommitCinematicPose(1.2f);
                float settled=0;for(int i=0;i<bones.Length;i++)settled=Mathf.Max(settled,Quaternion.Angle(target[i],bones[i].localRotation));
                maxSettled=Mathf.Max(maxSettled,settled);rows.Add(new{from=pair.Item1,to=pair.Item3,joinDegrees=join,joinLocalMetres=positionJoin,settledDegrees=settled});
            }
            bool pass=maxJoin<.05f && maxPositionJoin<.0001f && maxSettled<.05f;
            var report=new{native=true,originalRig=true,pass,maxJoin,maxPositionJoin,maxSettled,bones=bones.Length,rows};
            File.WriteAllText("D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/shark-transition-v079.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
            if(!pass)throw new Exception("Actual native action joins failed");return report;
        }
        finally{UnityEngine.Object.Destroy(fixture);}
    }
}
