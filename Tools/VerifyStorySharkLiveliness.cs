using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
public static class VerifyStorySharkLiveliness
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Field(object obj, string name) => obj.GetType().GetField(name,Private).GetValue(obj);
    static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static async Task<object> Run()
    {
        Check(Application.isPlaying,"Requires Play mode.");
        var sequence = UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
        Check(sequence != null && !sequence.HasImpacted,"Run on a fresh Story1 voyage.");
        float oldScale = Time.timeScale;
        float minSide = float.PositiveInfinity, maxSide = float.NegativeInfinity, maxBank = 0f;
        float bodyMotion = 0f;
        try
        {
            Time.timeScale = 3f;
            int iterations = 0;
            while (!sequence.HasImpacted && sequence.VoyageTime < sequence.impactAfterSeconds + 15f && iterations++ < 100)
            {
                await Task.Delay(200);
                var animator = sequence.swimmer.GetComponent<TralaleroSwimAnimator>();
                Vector3 heading = Vector3.ProjectOnPlane(sequence.ship.transform.right,Vector3.up).normalized;
                Vector3 side = Vector3.Cross(Vector3.up,heading);
                Vector3 bow = (Vector3)Field(sequence,"bowContactPoint");
                float lateral = Vector3.Dot(animator.NoseWorldPoint - bow, side);
                if (sequence.VoyageTime < sequence.impactAfterSeconds * .7f)
                { minSide = Mathf.Min(minSide,lateral); maxSide = Mathf.Max(maxSide,lateral); }
                maxBank = Mathf.Max(maxBank,Mathf.Abs((float)Field(sequence,"swimBank")));
                var rest = (Vector3[])Field(animator,"restVertices");
                var moved = (Vector3[])Field(animator,"movedVertices");
                float tailStart = (float)Field(animator,"tailStart"), bodyStart = (float)Field(animator,"bodyStart");
                for(int i=0;i<rest.Length;i++)
                {
                    Check(!float.IsNaN(moved[i].x),"Invalid deformed shark vertex.");
                    if(rest[i].z > tailStart && rest[i].z < bodyStart)
                        bodyMotion = Mathf.Max(bodyMotion,Mathf.Abs(moved[i].x-rest[i].x));
                    if(rest[i].z >= bodyStart) Check((moved[i]-rest[i]).sqrMagnitude < 1e-10f,"Nose was deformed.");
                }
            }
            Check(maxSide-minSide > 10f,"Shark hunting sweeps are not visible.");
            Check(maxBank > 1f,"Shark is not banking while turning.");
            Check(bodyMotion > .00001f,"Rear-body swimming deformation is absent.");
            Check(sequence.HasImpacted,"Shark did not reach the bow and trigger the original cinematic.");
            Check(!sequence.ship.enabled && !sequence.storyCamera.GetComponent<ShipFollowCamera>().enabled,
                "Cinematic did not take over from sailing controls.");
            return new { lateralSweep = maxSide-minSide, maximumBank = maxBank, bodyMotion,
                noseStable = true, collisionTriggered = sequence.HasImpacted, collisionTime = sequence.VoyageTime };
        }
        finally { Time.timeScale = oldScale; }
    }
}
