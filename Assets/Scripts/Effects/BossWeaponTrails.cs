using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Mavis
{
    // World-space history follows the animated blade, sampled after the fitted rig.
    [DefaultExecutionOrder(200)]
    public sealed class BossWeaponTrails : MonoBehaviour
    {
        sealed class Ribbon
        {
            public Transform hand;public Vector3 tip;
            public Mesh mesh;public MeshRenderer renderer;
            public readonly List<(Vector3 a,Vector3 b,float time)> points=new List<(Vector3,Vector3,float)>();
        }
        readonly List<Ribbon> ribbons=new List<Ribbon>();
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Color> colors=new List<Color>();
        readonly List<Vector2> uvs=new List<Vector2>();
        readonly List<int> triangles=new List<int>();
        Material material;
        CappuccinoAI capri;NailongAI nailong;Health health;NailongHealth nailongHealth;
        void Start()
        {
            capri=GetComponent<CappuccinoAI>();nailong=GetComponent<NailongAI>();health=GetComponent<Health>();nailongHealth=GetComponent<NailongHealth>();
            material=NaturalParticleEffects.Material("Fading blade sweep",new Color(.9f,.93f,.95f,.42f),false);
            if(capri)
            {
                var rig=GetComponentInChildren<CappuccinoMotionRig>();
                if(rig && rig.bladeGroundPoints!=null)
                {
                    var tips=new Dictionary<Transform,Vector3>();
                    foreach(var p in rig.bladeGroundPoints)if(p.bone && (!tips.TryGetValue(p.bone,out var tip)||p.localPoint.sqrMagnitude>tip.sqrMagnitude))tips[p.bone]=p.localPoint;
                    foreach(var pair in tips)Add(pair.Key,pair.Value);
                }
            }
            else if(nailong){var motion=GetComponent<NailongAttackMotion>();Add(motion.LeftHand,Vector3.zero);Add(motion.RightHand,Vector3.zero);}
        }
        void Add(Transform hand,Vector3 tip)
        {
            if(!hand)return;var go=new GameObject("Animated weapon sweep");go.transform.SetParent(transform,false);
            var mesh=new Mesh{name="Short blade history"};mesh.MarkDynamic();go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            ribbons.Add(new Ribbon{hand=hand,tip=tip,mesh=mesh,renderer=renderer});
        }
        void LateUpdate()
        {
            if(PauseSettingsMenu.IsOpen)return;
            bool dead=(health&&health.IsDead)||(nailongHealth&&nailongHealth.IsDead);
            for(int r=0;r<ribbons.Count;r++)
            {
                var trail=ribbons[r];bool active=!dead&&enabled&&(capri?capri.DamageWindowActive:nailong&&nailong.ClawActive(r));
                var camera=Camera.main;if(camera && (camera.transform.position-transform.position).sqrMagnitude>10000)active=false;
                float lifetime=capri?.14f:.11f;
                for(int i=trail.points.Count-1;i>=0;i--)if(Time.time-trail.points[i].time>lifetime)trail.points.RemoveAt(i);
                if(active)
                {
                    Vector3 end=trail.hand.TransformPoint(trail.tip),start=Vector3.Lerp(trail.hand.position,end,.35f);
                    if(!capri){end=trail.hand.position;start=end-transform.up*.15f;end+=transform.up*.10f;}
                    if(trail.points.Count==0 || (end-trail.points[trail.points.Count-1].b).sqrMagnitude<9)
                        trail.points.Add((start,end,Time.time));else trail.points.Clear();
                    if(trail.points.Count>12)trail.points.RemoveAt(0);
                }
                trail.renderer.enabled=trail.points.Count>1;
                if(!trail.renderer.enabled)continue;
                vertices.Clear();colors.Clear();uvs.Clear();triangles.Clear();
                for(int i=0;i<trail.points.Count;i++)
                {
                    var p=trail.points[i];vertices.Add(transform.InverseTransformPoint(p.a));vertices.Add(transform.InverseTransformPoint(p.b));
                    float alpha=Mathf.Pow(Mathf.Clamp01(1-(Time.time-p.time)/lifetime),2);
                    colors.Add(new Color(1,1,1,alpha*.25f));colors.Add(new Color(1,1,1,alpha));uvs.Add(new Vector2(0,0));uvs.Add(new Vector2(1,0));
                    if(i>0){int k=i*2;triangles.Add(k-2);triangles.Add(k);triangles.Add(k-1);triangles.Add(k-1);triangles.Add(k);triangles.Add(k+1);}
                }
                trail.mesh.Clear();trail.mesh.SetVertices(vertices);trail.mesh.SetColors(colors);trail.mesh.SetUVs(0,uvs);trail.mesh.SetTriangles(triangles,0);trail.mesh.RecalculateNormals();trail.mesh.RecalculateBounds();
            }
        }
        void OnDisable(){foreach(var r in ribbons){r.points.Clear();if(r.renderer)r.renderer.enabled=false;}}
        void OnDestroy(){foreach(var r in ribbons)if(r.mesh)Destroy(r.mesh);if(material)Destroy(material);}
    }
}
