using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Mavis
{
    /// <summary>A terrain-following white guiding thread towards the active main objective.</summary>
    [DisallowMultipleComponent,DefaultExecutionOrder(18500)]
    public sealed class MainQuestGuidance:MonoBehaviour
    {
        const int Side=97,Cells=Side*Side,Half=Side/2,Points=1024,Strands=1,Ring=8;
        float cell=2.5f;
        readonly int[] heap=new int[Cells],heapPosition=new int[Cells];
        int heapCount;
        readonly Vector3[] ground=new Vector3[Cells],chain=new Vector3[Cells],route=new Vector3[Points],pendingRoute=new Vector3[Points];
        readonly float[] cost=new float[Cells],estimate=new float[Cells],lengths=new float[Points];
        readonly int[] parents=new int[Cells];readonly byte[] seen=new byte[Cells],sampled=new byte[Cells];
        readonly RaycastHit[] hits=new RaycastHit[32];readonly Collider[] blockers=new Collider[24];
        readonly Vector3[] vertices=new Vector3[Points*Strands*Ring],normals=new Vector3[Points*Strands*Ring];
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
            mesh=new Mesh{name="Main quest white guiding thread"};mesh.MarkDynamic();
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
            Vector3 candidate=gridOrigin+new Vector3((index%Side-Half)*cell,0,(index/Side-Half)*cell);
            bool valid=Surface(candidate,out ground[index],out var floor)&&Walkable(ground[index],floor);
            sampled[index]=(byte)(valid?1:2);return valid;
        }
        IEnumerator Solve(Vector3 start,Vector3 goal)
        {
            // Always yield once so the caller stores the Coroutine before it can finish.
            yield return null;
            waterLevel=Shader.GetGlobalFloat("_MavisSeaLevel")+.2f;
            int chainCount=0;
            if(!Surface(start,out var startFloor,out _)||!Surface(goal,out var goalFloor,out _)){job=null;yield break;}
            if(NavMesh.SamplePosition(start,out var navStart,3,NavMesh.AllAreas)&&NavMesh.SamplePosition(goal,out var navGoal,3,NavMesh.AllAreas)&&
                NavMesh.CalculatePath(navStart.position,navGoal.position,NavMesh.AllAreas,navPath)&&navPath.status==NavMeshPathStatus.PathComplete)
            {
                var corners=navPath.corners;
                if(corners.Length>=Cells-2){job=null;yield break;}
                chain[chainCount++]=startFloor;
                foreach(var corner in corners)chain[chainCount++]=corner;
                chain[chainCount++]=goalFloor;
            }
            else
            {
                // Cover BOTH endpoints plus a detour margin. A heap keeps the
                // whole-island fallback from scanning every open cell per step.
                Vector3 extent=goalFloor-startFloor;
                float span=Mathf.Max(Mathf.Abs(extent.x),Mathf.Abs(extent.z));
                cell=Mathf.Max(2.5f,(span+Mathf.Max(50,span*.4f))/(Side-1));
                gridOrigin=(startFloor+goalFloor)*.5f;
                System.Array.Clear(sampled,0,Cells);System.Array.Clear(seen,0,Cells);heapCount=0;
                for(int i=0;i<Cells;i++){cost[i]=float.PositiveInfinity;estimate[i]=float.PositiveInfinity;parents[i]=-1;heapPosition[i]=-1;}
                int first=GridIndex(startFloor),last=GridIndex(goalFloor);
                if(!Node(first)||!Node(last)){job=null;yield break;}
                cost[first]=0;estimate[first]=FlatDistance(ground[first],goalFloor);PushOrImprove(first);
                bool reached=false;double deadline=Time.realtimeSinceStartupAsDouble+.001;
                while(heapCount>0)
                {
                    if(Time.realtimeSinceStartupAsDouble>=deadline){yield return null;deadline=Time.realtimeSinceStartupAsDouble+.001;}
                    int current=Pop();seen[current]=2;
                    if(current==last){reached=true;break;}
                    int x=current%Side,z=current/Side;
                    for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                    {
                        int nx=x+dx,nz=z+dz;if((dx==0&&dz==0)||nx<0||nx>=Side||nz<0||nz>=Side)continue;
                        int next=nz*Side+nx;if(seen[next]==2||!Node(next))continue;
                        if(dx!=0&&dz!=0&&(!Node(z*Side+nx)||!Node(nz*Side+x)))continue;
                        float horizontal=cell*(dx!=0&&dz!=0?1.414214f:1);
                        float rise=Mathf.Abs(ground[next].y-ground[current].y);if(rise>horizontal*.70f)continue;
                        float candidate=cost[current]+horizontal+rise*.5f;
                        if(candidate>=cost[next])continue;
                        if(!ClearSegment(ground[current],ground[next]))continue;
                        cost[next]=candidate;estimate[next]=candidate+FlatDistance(ground[next],goalFloor);parents[next]=current;PushOrImprove(next);
                    }
                }
                // Never present the closest partial cell as the destination.
                if(!reached){job=null;yield break;}
                for(int node=last;node>=0&&chainCount<Cells-2;node=parents[node])chain[chainCount++]=ground[node];
                System.Array.Reverse(chain,0,chainCount);
                if(!ClearSegment(startFloor,chain[0])||!ClearSegment(chain[chainCount-1],goalFloor)){job=null;yield break;}
                System.Array.Copy(chain,0,chain,1,chainCount);chain[0]=startFloor;chain[++chainCount]=goalFloor;chainCount++;
            }
            if(chainCount>1)
            {
                int count=0;Vector3 previous=chain[0];bool blocked=false;
                float pathLength=0;for(int c=1;c<chainCount;c++)pathLength+=Vector3.Distance(chain[c-1],chain[c]);
                if(chainCount>=Points-2){job=null;yield break;}
                float spacing=Mathf.Max(1f,pathLength/(Points-chainCount-1));
                pendingRoute[count++]=previous;
                for(int c=1;c<chainCount&&count<Points&&!blocked;c++)
                {
                    Vector3 delta=chain[c]-previous;float segment=delta.magnitude;int samples=Mathf.Max(1,Mathf.CeilToInt(segment/spacing));
                    for(int i=1;i<=samples&&count<Points;i++)
                    {
                        Vector3 p=Vector3.Lerp(previous,chain[c],i/(float)samples);
                        if(Surface(p,out var groundPoint,out var floor)&&Mathf.Abs(groundPoint.y-p.y)<1&&Walkable(groundPoint,floor))p=groundPoint;
                        else{blocked=true;break;}
                        pendingRoute[count++]=p;
                        if(count%8==0)yield return null;
                    }
                    previous=chain[c];
                }
                if(blocked||count<2||FlatDistance(pendingRoute[count-1],goalFloor)>.05f){job=null;yield break;}
                for(int i=0;i<count;i++)
                {
                    Vector3 p=pendingRoute[i];
                    if(i>0&&i<count-1)
                    {
                        Vector3 smooth=(pendingRoute[i-1]+p*2+pendingRoute[i+1])*.25f;
                        if(Surface(smooth,out var floorPoint,out var floor)&&Walkable(floorPoint,floor)&&Mathf.Abs(floorPoint.y-p.y)<1&&
                            ClearSegment(pendingRoute[i-1],floorPoint)&&ClearSegment(floorPoint,pendingRoute[i+1]))p=floorPoint;
                    }
                    chain[i]=p;if(i%8==0)yield return null;
                }
                System.Array.Copy(chain,route,count);routeCount=count;lengths[0]=0;
                for(int i=1;i<count;i++)lengths[i]=lengths[i-1]+Vector3.Distance(route[i-1],route[i]);
                SolvedRoutes++;
            }
            job=null;
        }
        int GridIndex(Vector3 point)
        {
            int x=Mathf.Clamp(Mathf.RoundToInt((point.x-gridOrigin.x)/cell)+Half,0,Side-1);
            int z=Mathf.Clamp(Mathf.RoundToInt((point.z-gridOrigin.z)/cell)+Half,0,Side-1);return z*Side+x;
        }
        bool ClearSegment(Vector3 a,Vector3 b)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(FlatDistance(a,b)/1.25f));
            for(int i=1;i<=steps;i++)
            {
                Vector3 point=Vector3.Lerp(a,b,i/(float)steps);
                if(!Surface(point,out var floorPoint,out var floor)||Mathf.Abs(point.y-floorPoint.y)>1||!Walkable(floorPoint,floor))return false;
            }
            return true;
        }
        void PushOrImprove(int value)
        {
            int at=heapPosition[value];if(at<0){at=heapCount++;heap[at]=value;heapPosition[value]=at;}seen[value]=1;
            while(at>0){int parent=(at-1)/2;if(estimate[heap[parent]]<=estimate[value])break;heap[at]=heap[parent];heapPosition[heap[at]]=at;at=parent;}
            heap[at]=value;heapPosition[value]=at;
        }
        int Pop()
        {
            int result=heap[0],value=heap[--heapCount];heapPosition[result]=-1;
            if(heapCount==0)return result;int at=0;
            while(at*2+1<heapCount){int child=at*2+1;if(child+1<heapCount&&estimate[heap[child+1]]<estimate[heap[child]])child++;if(estimate[value]<=estimate[heap[child]])break;heap[at]=heap[child];heapPosition[heap[at]]=at;at=child;}
            heap[at]=value;heapPosition[value]=at;return result;
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
                float envelope=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,1.5f,d));
                if(i>=routeCount||d<0)envelope=0;
                float sway=d*.48f-now*.35f;
                Vector3 centre=route[p]+Vector3.up*(height+Mathf.Sin(d*.23f-now*.6f)*.12f)+
                    (side*Mathf.Sin(sway)+Vector3.up*Mathf.Cos(sway))*.015f;
                for(int r=0;r<Ring;r++)
                {
                    int k=(strand*Points+i)*Ring+r;float a=r*Mathf.PI*2/Ring;
                    Vector3 normal=side*Mathf.Cos(a)+Vector3.up*Mathf.Sin(a);float radius=.055f;
                    vertices[k]=worldToLocal.MultiplyPoint3x4(centre+normal*radius);normals[k]=worldToLocal.MultiplyVector(normal).normalized;
                    uv[k]=new Vector2(lengths[p],r/(float)Ring);
                    colors[k]=new Color(1,1,1,envelope);
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
