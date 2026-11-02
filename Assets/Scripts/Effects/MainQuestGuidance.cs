using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Mavis
{
    /// <summary>A short, terrain-following gold/green thread towards the active main objective.</summary>
    [DisallowMultipleComponent,DefaultExecutionOrder(18500)]
    public sealed class MainQuestGuidance:MonoBehaviour
    {
        const int Side=25,Cells=Side*Side,Half=Side/2,Points=64,Strands=3,Ring=6;
        const float Cell=2.5f,Reach=27.5f,MaximumLength=45;
        readonly Vector3[] ground=new Vector3[Cells],chain=new Vector3[Cells],route=new Vector3[Points],pendingRoute=new Vector3[Points];
        readonly float[] cost=new float[Cells],estimate=new float[Cells],lengths=new float[Points];
        readonly int[] parents=new int[Cells];readonly byte[] seen=new byte[Cells],sampled=new byte[Cells];
        readonly RaycastHit[] hits=new RaycastHit[32];readonly Collider[] blockers=new Collider[24];
        readonly Vector3[] vertices=new Vector3[Points*Strands*Ring],normals=new Vector3[Points*Strands*Ring],corners=new Vector3[32];
        readonly Vector2[] uv=new Vector2[Points*Strands*Ring];readonly Color[] colors=new Color[Points*Strands*Ring];
        readonly int[] indices=new int[(Points-1)*Strands*Ring*6];
        MaterialPropertyBlock properties;
        NavMeshPath navPath;
        FirstIslandObjectives objectives;ThirdPersonPlayerController motor;SahurAttack attack;CharacterController capsule;
        Mesh mesh;MeshRenderer renderer;Transform root;Coroutine job;
        Vector3 gridOrigin,lastStart,lastGoal;string targetId;float nextPath,fade,waterLevel;int routeCount;
        public bool Visible=>renderer&&renderer.enabled&&fade>.01f;
        public int RoutePointCount=>routeCount;
        public Vector3 TargetPosition{get;private set;}
        public string TargetId=>targetId;
        public int SolvedRoutes{get;private set;}
        void OnEnable()
        {
            objectives=GetComponent<FirstIslandObjectives>();motor=GetComponent<ThirdPersonPlayerController>();attack=GetComponent<SahurAttack>();capsule=GetComponent<CharacterController>();
            Ensure();nextPath=0;
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload-=BeforeReload;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload+=BeforeReload;
#endif
        }
        void Ensure()
        {
            if(properties==null)properties=new MaterialPropertyBlock();
            if(navPath==null)navPath=new NavMeshPath();
            if(mesh)return;var material=Resources.Load<Material>("Guidance/QuestThread");if(!material)return;
            mesh=new Mesh{name="Main quest gold-green guiding thread"};mesh.MarkDynamic();
            var go=new GameObject("Main quest guiding thread");go.transform.SetParent(transform,false);root=go.transform;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.enabled=false;
            int cursor=0;
            for(int strand=0;strand<Strands;strand++)for(int p=0;p<Points-1;p++)for(int r=0;r<Ring;r++)
            {
                int a=(strand*Points+p)*Ring+r,b=(strand*Points+p)*Ring+(r+1)%Ring,c=a+Ring,d=b+Ring;
                indices[cursor++]=a;indices[cursor++]=b;indices[cursor++]=c;indices[cursor++]=b;indices[cursor++]=d;indices[cursor++]=c;
            }
        }
        bool Blocked=>!motor||!motor.enabled||motor.ExternalControlLock||Time.timeScale<=0||PauseSettingsMenu.IsOpen||
            SahurLoadoutUI.BlocksInput||IslandMapUI.IsOpen||PlayerDeathRespawn.IsOpen||BossHudPanel.HasActiveEncounter||
            (attack&&attack.IsCombatMotionActive)||AdditiveSceneBootstrap.IsLoading||GameSaveManager.IsContinuePending;
        void LateUpdate()
        {
            Ensure();if(!renderer)return;
            Vector3 goal=default;string id=null;
            bool available=!Blocked&&objectives&&objectives.TryGetGuidanceTarget(out goal,out id)&&
                Vector3.ProjectOnPlane(goal-transform.position,Vector3.up).sqrMagnitude>25;
            if(available)
            {
                TargetPosition=goal;
                bool changed=id!=targetId;
                if(changed){targetId=id;routeCount=0;nextPath=0;if(job!=null){StopCoroutine(job);job=null;}}
                if(job==null&&Time.unscaledTime>=nextPath&&(routeCount<2||(transform.position-lastStart).sqrMagnitude>9||(goal-lastGoal).sqrMagnitude>9))
                {lastStart=transform.position;lastGoal=goal;nextPath=Time.unscaledTime+1.5f;job=StartCoroutine(Solve(lastStart,goal));}
            }
            fade=Mathf.MoveTowards(fade,available&&routeCount>1?1:0,Time.unscaledDeltaTime/(available?.65f:.22f));
            renderer.enabled=fade>.001f&&routeCount>1;if(renderer.enabled)Draw();
        }
        bool Surface(Vector3 point,out Vector3 result,out Collider floor)
        {
            result=default;floor=null;float best=float.PositiveInfinity;
            int count=Physics.RaycastNonAlloc(point+Vector3.up*120,Vector3.down,hits,300,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];if(!hit.collider||hit.point.y<=waterLevel||hit.normal.y<.65f||hit.distance>=best||hit.transform.IsChildOf(transform)||
                    hit.collider.GetComponentInParent<Health>()||hit.collider.GetComponentInParent<NailongHealth>()||hit.collider.GetComponentInParent<CharacterController>())continue;
                best=hit.distance;result=hit.point;floor=hit.collider;
            }
            return floor;
        }
        bool Walkable(Vector3 point,Collider floor)
        {
            float scale=Mathf.Abs(transform.lossyScale.y),radius=capsule?Mathf.Clamp(capsule.radius*scale*.7f,.35f,.85f):.5f;
            float height=capsule?Mathf.Clamp(capsule.height*scale,2,6):2;
            int count=Physics.OverlapCapsuleNonAlloc(point+Vector3.up*(radius+.35f),point+Vector3.up*(height-radius),radius,blockers,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            if(count==blockers.Length)return false;
            for(int i=0;i<count;i++)
            {
                var c=blockers[i];if(!c||c==floor||c.transform.IsChildOf(transform)||c.GetComponentInParent<Health>()||
                    c.GetComponentInParent<NailongHealth>()||c.GetComponentInParent<CharacterController>())continue;
                return false;
            }
            return true;
        }
        bool Node(int index)
        {
            if(sampled[index]!=0)return sampled[index]==1;
            Vector3 candidate=gridOrigin+new Vector3((index%Side-Half)*Cell,0,(index/Side-Half)*Cell);
            bool valid=Surface(candidate,out ground[index],out var floor)&&Walkable(ground[index],floor);
            sampled[index]=(byte)(valid?1:2);return valid;
        }
        IEnumerator Solve(Vector3 start,Vector3 goal)
        {
            // Always yield once so the caller stores the Coroutine before it can finish.
            yield return null;
            waterLevel=Shader.GetGlobalFloat("_MavisSeaLevel")+.2f;
            int chainCount=0;
            if(NavMesh.SamplePosition(start,out var navStart,3,NavMesh.AllAreas)&&NavMesh.SamplePosition(goal,out var navGoal,8,NavMesh.AllAreas)&&
                NavMesh.CalculatePath(navStart.position,navGoal.position,NavMesh.AllAreas,navPath)&&navPath.status==NavMeshPathStatus.PathComplete)
            {
                int count=navPath.GetCornersNonAlloc(corners);for(int i=0;i<count;i++)chain[chainCount++]=corners[i];
            }
            else
            {
                gridOrigin=new Vector3(Mathf.Round(start.x/Cell)*Cell,start.y,Mathf.Round(start.z/Cell)*Cell);
                System.Array.Clear(sampled,0,Cells);System.Array.Clear(seen,0,Cells);
                for(int i=0;i<Cells;i++){cost[i]=float.PositiveInfinity;estimate[i]=float.PositiveInfinity;parents[i]=-1;}
                int first=Half*Side+Half;if(!Node(first)){job=null;yield break;}
                Vector3 towards=Vector3.ProjectOnPlane(goal-start,Vector3.up);Vector3 localGoal=start+towards.normalized*Mathf.Min(Reach,towards.magnitude);
                cost[first]=0;estimate[first]=FlatDistance(ground[first],localGoal);seen[first]=1;
                int closest=first;float closestDistance=estimate[first];double deadline=Time.realtimeSinceStartupAsDouble+.001;
                for(int iteration=0;iteration<Cells;iteration++)
                {
                    if(Time.realtimeSinceStartupAsDouble>=deadline){yield return null;deadline=Time.realtimeSinceStartupAsDouble+.001;}
                    int current=-1;float best=float.PositiveInfinity;
                    for(int i=0;i<Cells;i++)if(seen[i]==1&&estimate[i]<best){current=i;best=estimate[i];}
                    if(current<0)break;seen[current]=2;
                    float remaining=FlatDistance(ground[current],localGoal);
                    if(remaining<closestDistance){closest=current;closestDistance=remaining;}
                    if(remaining<Cell*.8f)break;
                    int x=current%Side,z=current/Side;
                    for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                    {
                        int nx=x+dx,nz=z+dz;if((dx==0&&dz==0)||nx<0||nx>=Side||nz<0||nz>=Side)continue;
                        int next=nz*Side+nx;if(seen[next]==2||!Node(next))continue;
                        // No diagonal shortcuts through the corner of a solid obstacle.
                        if(dx!=0&&dz!=0&&(!Node(z*Side+nx)||!Node(nz*Side+x)||
                            Mathf.Abs(ground[z*Side+nx].y-ground[current].y)>Cell*.7f||
                            Mathf.Abs(ground[nz*Side+x].y-ground[current].y)>Cell*.7f))continue;
                        float horizontal=Cell*(dx!=0&&dz!=0?1.414214f:1);
                        float rise=Mathf.Abs(ground[next].y-ground[current].y);if(rise>horizontal*.70f)continue;
                        float candidate=cost[current]+horizontal+rise*.5f;
                        if(candidate>=cost[next])continue;
                        Vector3 middle=(ground[current]+ground[next])*.5f;
                        if(!Surface(middle,out var middleGround,out var middleFloor)||Mathf.Abs(middleGround.y-middle.y)>horizontal*.35f+.2f||!Walkable(middleGround,middleFloor))continue;
                        cost[next]=candidate;estimate[next]=candidate+FlatDistance(ground[next],localGoal);parents[next]=current;seen[next]=1;
                    }
                }
                if(closest==first){job=null;yield break;}
                for(int node=closest;node>=0&&chainCount<Cells;node=parents[node])chain[chainCount++]=ground[node];
                System.Array.Reverse(chain,0,chainCount);
            }
            if(chainCount>1)
            {
                int count=0;float distance=0;Vector3 previous=chain[0];bool blocked=false;
                pendingRoute[count++]=previous;
                for(int c=1;c<chainCount&&count<Points&&distance<MaximumLength&&!blocked;c++)
                {
                    Vector3 delta=chain[c]-previous;float segment=delta.magnitude;int samples=Mathf.Max(1,Mathf.CeilToInt(segment/.85f));
                    for(int i=1;i<=samples&&count<Points;i++)
                    {
                        Vector3 p=Vector3.Lerp(previous,chain[c],i/(float)samples);
                        if(Surface(p,out var groundPoint,out var floor)&&Mathf.Abs(groundPoint.y-p.y)<1&&Walkable(groundPoint,floor))p=groundPoint;
                        else{blocked=true;break;}
                        distance+=Vector3.Distance(pendingRoute[count-1],p);if(distance>MaximumLength)break;pendingRoute[count++]=p;
                        if(count%8==0)yield return null;
                    }
                    previous=chain[c];
                }
                for(int i=0;i<count;i++)
                {
                    Vector3 p=pendingRoute[i];
                    if(i>0&&i<count-1)
                    {
                        Vector3 smooth=(pendingRoute[i-1]+p*2+pendingRoute[i+1])*.25f;
                        if(Surface(smooth,out var floorPoint,out var floor)&&Walkable(floorPoint,floor)&&Mathf.Abs(floorPoint.y-p.y)<1)p=floorPoint;
                    }
                    chain[i]=p;if(i%8==0)yield return null;
                }
                System.Array.Copy(chain,route,count);routeCount=count;lengths[0]=0;
                for(int i=1;i<count;i++)lengths[i]=lengths[i-1]+Vector3.Distance(route[i-1],route[i]);
                SolvedRoutes++;
            }
            job=null;
        }
        static float FlatDistance(Vector3 a,Vector3 b){a-=b;return Mathf.Sqrt(a.x*a.x+a.z*a.z);}
        void Draw()
        {
            Matrix4x4 worldToLocal=root.worldToLocalMatrix;
            float now=Time.time;int nearest=0;float best=float.PositiveInfinity;
            for(int i=0;i<routeCount;i++){float d=Vector3.ProjectOnPlane(route[i]-transform.position,Vector3.up).sqrMagnitude;if(d<best){best=d;nearest=i;}}
            if(best>100){renderer.enabled=false;return;}
            float start=lengths[nearest],height=capsule?Mathf.Clamp(capsule.height*Mathf.Abs(transform.lossyScale.y)*.65f,2.2f,3.8f):2.2f;
            for(int strand=0;strand<Strands;strand++)for(int i=0;i<Points;i++)
            {
                int p=Mathf.Min(i,routeCount-1);Vector3 tangent=route[Mathf.Min(routeCount-1,p+1)]-route[Mathf.Max(0,p-1)];tangent.y=0;
                if(tangent.sqrMagnitude<.001f)tangent=transform.forward;tangent.Normalize();Vector3 side=Vector3.Cross(Vector3.up,tangent);
                float d=lengths[p]-start;
                float envelope=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.8f,3,d))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(23,31,d)));
                if(i>=routeCount||d<0)envelope=0;
                float braid=d*.48f-now*.35f+strand*2.0944f;
                Vector3 centre=route[p]+Vector3.up*(height+Mathf.Sin(d*.23f-now*.6f)*.12f)+
                    (side*Mathf.Sin(braid)+Vector3.up*Mathf.Cos(braid))*(strand==0?.025f:.10f);
                for(int r=0;r<Ring;r++)
                {
                    int k=(strand*Points+i)*Ring+r;float a=r*Mathf.PI*2/Ring;
                    Vector3 normal=side*Mathf.Cos(a)+Vector3.up*Mathf.Sin(a);float radius=strand==0?.035f:.022f;
                    vertices[k]=worldToLocal.MultiplyPoint3x4(centre+normal*radius);normals[k]=worldToLocal.MultiplyVector(normal).normalized;
                    uv[k]=new Vector2(lengths[p],r/(float)Ring);
                    Color tint=strand==0?new Color(1,.73f,.25f):strand==1?new Color(.20f,.70f,.36f):new Color(.48f,.69f,.29f);
                    tint.a=envelope;colors[k]=tint;
                }
            }
            mesh.Clear();mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.colors=colors;mesh.triangles=indices;mesh.RecalculateBounds();
            properties.SetFloat("_Clock",now);properties.SetFloat("_Fade",fade);renderer.SetPropertyBlock(properties);
        }
        void OnDisable()
        {
            if(job!=null)StopCoroutine(job);job=null;routeCount=0;fade=0;if(renderer)renderer.enabled=false;
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload-=BeforeReload;
#endif
        }
#if UNITY_EDITOR
        void BeforeReload()=>Dispose(true);
#endif
        void Dispose(bool immediate)
        {
            if(mesh){if(immediate)DestroyImmediate(mesh);else Destroy(mesh);}mesh=null;
            if(root){if(immediate)DestroyImmediate(root.gameObject);else Destroy(root.gameObject);}root=null;renderer=null;
        }
        void OnDestroy()=>Dispose(false);
    }
}
