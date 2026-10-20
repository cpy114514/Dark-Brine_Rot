using UnityEngine;
namespace Mavis
{
    /// <summary>Terrain-projected attack footprint. Geometry comes from the attack owner.</summary>
    public sealed class BossAttackWarning : MonoBehaviour
    {
        LineRenderer line; Material material; float until, nextGeometry;
        readonly Vector3[] points = new Vector3[67];
        readonly Vector3[] circlePoints = new Vector3[65], lanePoints = new Vector3[5];
        RaycastHit[] groundHits = new RaycastHit[16];
        public static BossAttackWarning For(GameObject owner) => owner.GetComponent<BossAttackWarning>() ?? owner.AddComponent<BossAttackWarning>();
        // Keep this compatibility gate global: projectiles and demo encounters
        // must follow the same no-telegraph policy as the island bosses.
        public static bool HiddenOnIsland(GameObject owner) => true;
        void Awake()
        {
            if(HiddenOnIsland(gameObject)){enabled=false;return;}
            var child = new GameObject("Attack danger footprint"); child.transform.SetParent(transform, false);
            line = child.AddComponent<LineRenderer>();
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", new Color(1,.24f,.05f)); line.sharedMaterial = material;
            line.useWorldSpace = true; line.widthMultiplier = .13f; line.enabled = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        Vector3 Ground(Vector3 p)
        {
            float best = float.NegativeInfinity;
            int count;
            // Retry a full buffer so dense/overlapping scenery cannot hide the true floor.
            while((count=Physics.RaycastNonAlloc(p + Vector3.up * 100, Vector3.down,groundHits,200,~0,QueryTriggerInteraction.Ignore))==groundHits.Length)
                System.Array.Resize(ref groundHits,groundHits.Length*2);
            for(int i=0;i<count;i++)
            {
                var hit=groundHits[i];
                var c=hit.collider;
                if(c.transform.IsChildOf(transform) || c.GetComponentInParent<IDamageable>() != null || c.GetComponentInParent<PlayerHealth>() != null || hit.normal.y < .5f) continue;
                best=Mathf.Max(best,hit.point.y);
            }
            if(best>float.NegativeInfinity)p.y=best; p.y+=.12f; return p;
        }
        public void Circle(Vector3 center,float radius,float seconds) => Sector(center,Vector3.forward,radius,360,seconds);
        public void Sector(Vector3 center,Vector3 direction,float radius,float angle,float seconds)
        {
            if(HiddenOnIsland(gameObject) || line==null){Clear();return;}
            if(Time.time<nextGeometry){Show(seconds);return;}nextGeometry=Time.time+.08f;
            float yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg; int count=65;
            for(int i=0;i<count;i++){float a=(yaw-angle*.5f+angle*i/64)*Mathf.Deg2Rad;points[i]=Ground(center+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*radius);}
            if(angle<359){points[count++]=Ground(center);points[count++]=points[0];}
            line.positionCount=count;
            if(count==65){System.Array.Copy(points,circlePoints,65);line.SetPositions(circlePoints);}
            else line.SetPositions(points);
            Show(seconds);
        }
        public void Lane(Vector3 start,Vector3 end,float halfWidth,float seconds)
        {
            if(HiddenOnIsland(gameObject) || line==null){Clear();return;}
            if(Time.time<nextGeometry){Show(seconds);return;}nextGeometry=Time.time+.08f;
            var side=Vector3.Cross(Vector3.up,(end-start).normalized)*halfWidth;
            lanePoints[0]=Ground(start-side);lanePoints[1]=Ground(end-side);lanePoints[2]=Ground(end+side);lanePoints[3]=Ground(start+side);lanePoints[4]=lanePoints[0];
            line.positionCount=5;line.SetPositions(lanePoints);Show(seconds);
        }
        void Show(float seconds){if(line==null)return;until=Time.time+Mathf.Max(.05f,seconds);line.enabled=true;}
        public void Clear(){if(line)line.enabled=false;}
        void Update(){if(Time.time>=until)Clear();}
        void OnDisable()=>Clear();
        void OnDestroy(){if(material)Destroy(material);}
    }
}
