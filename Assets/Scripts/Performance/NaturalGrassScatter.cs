using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mavis
{
    // Shares small irregular tufts instead of stamping large rectangular mats or
    // storing an island-sized copy of every leaf. No GameObject per tuft.
    [ExecuteAlways, DefaultExecutionOrder(31010)]
    public sealed class NaturalGrassScatter : MonoBehaviour
    {
        public NaturalGrassScatterData data;
        sealed class Batch { public Mesh mesh,medium,far; public Matrix4x4[] matrices; public Bounds bounds; }
        readonly List<Batch> batches=new List<Batch>();
        readonly Plane[] planes=new Plane[6];
        Coroutine preparation;
        MaterialPropertyBlock lodBlock;
        static readonly int LodFade=Shader.PropertyToID("_EcologyLodFade");
        Batch MakeBatch(int variant,Matrix4x4[] matrices,Bounds bounds)
        {
            return new Batch{mesh=data.meshes[variant],matrices=matrices,bounds=bounds,
                medium=data.mediumMeshes!=null&&variant<data.mediumMeshes.Length?data.mediumMeshes[variant]:null,
                far=data.farMeshes!=null&&variant<data.farMeshes.Length?data.farMeshes[variant]:null};
        }
        public int InstanceCount {get;private set;}
        public int VisibleBatches {get;private set;}
        public bool IsReady {get;private set;}

        void OnEnable()
        {
            if(Application.isPlaying)preparation=StartCoroutine(Prepare());
            else {var work=Prepare();while(work.MoveNext()){} }
        }
        IEnumerator Prepare()
        {
            batches.Clear();InstanceCount=0;IsReady=false;
            if(!data||!data.material||data.meshes==null||data.meshes.Length==0||data.cells==null)yield break;
            double deadline=Time.realtimeSinceStartupAsDouble+.003;
            foreach(var cell in data.cells)
            {
                if(cell.positions==null||cell.normals==null||cell.positions.Length!=cell.normals.Length)continue;
                for(int variant=0;variant<data.meshes.Length;variant++)
                {
                    var matrices=new List<Matrix4x4>(256);
                    for(int i=variant;i<cell.positions.Length;i+=data.meshes.Length)
                    {
                        var p=cell.positions[i];
                        float scale=.86f+Mathf.Repeat(p.w*.071f,1)*.28f;
                        var rotation=Quaternion.FromToRotation(Vector3.up,cell.normals[i])*Quaternion.Euler(0,p.w,0);
                        matrices.Add(Matrix4x4.TRS(new Vector3(p.x,p.y,p.z),rotation,Vector3.one*scale));
                        InstanceCount++;
                        if(matrices.Count==256){batches.Add(MakeBatch(variant,matrices.ToArray(),cell.bounds));matrices.Clear();}
                    }
                    if(matrices.Count>0)batches.Add(MakeBatch(variant,matrices.ToArray(),cell.bounds));
                }
                if(Application.isPlaying&&Time.realtimeSinceStartupAsDouble>=deadline){yield return null;deadline=Time.realtimeSinceStartupAsDouble+.003;}
            }
            IsReady=true;preparation=null;
        }
        void LateUpdate()
        {
            if(!data||!data.material||!SystemInfo.supportsInstancing)return;
            var camera=Camera.main;
#if UNITY_EDITOR
            if(!Application.isPlaying&&UnityEditor.SceneView.lastActiveSceneView)camera=UnityEditor.SceneView.lastActiveSceneView.camera;
#endif
            if(!camera)return;
            Submit(camera);
        }
        public void Submit(Camera camera)
        {
            if(!data||!data.material||!camera)return;
            GeometryUtility.CalculateFrustumPlanes(camera,planes);VisibleBatches=0;
            foreach(var batch in batches)
            {
                if(batch.bounds.SqrDistance(camera.transform.position)>240f*240f||!GeometryUtility.TestPlanesAABB(planes,batch.bounds))continue;
                float distance=Mathf.Sqrt(batch.bounds.SqrDistance(camera.transform.position));
                if(!batch.medium||!batch.far){Draw(batch,batch.mesh,camera,0,0);continue;}
                if(distance<data.mediumTransition.x)Draw(batch,batch.mesh,camera,0,0);
                else if(distance<data.mediumTransition.y)
                {
                    float fade=Mathf.InverseLerp(data.mediumTransition.x,data.mediumTransition.y,distance);
                    Draw(batch,batch.mesh,camera,fade,1);Draw(batch,batch.medium,camera,fade,-1);
                }
                else if(distance<data.farTransition.x)Draw(batch,batch.medium,camera,0,0);
                else if(distance<data.farTransition.y)
                {
                    float fade=Mathf.InverseLerp(data.farTransition.x,data.farTransition.y,distance);
                    Draw(batch,batch.medium,camera,fade,1);Draw(batch,batch.far,camera,fade,-1);
                }
                else Draw(batch,batch.far,camera,0,0);
            }
        }
        void Draw(Batch batch,Mesh mesh,Camera camera,float fade,float side)
        {
            if(lodBlock==null)lodBlock=new MaterialPropertyBlock();
            lodBlock.Clear();lodBlock.SetVector(LodFade,new Vector4(fade,side,0,0));
            Graphics.DrawMeshInstanced(mesh,0,data.material,batch.matrices,batch.matrices.Length,lodBlock,ShadowCastingMode.Off,true,gameObject.layer,camera,LightProbeUsage.Off);
            VisibleBatches++;
        }
        void OnDisable()
        {
            if(preparation!=null)StopCoroutine(preparation);preparation=null;
            batches.Clear();InstanceCount=0;VisibleBatches=0;IsReady=false;
        }
    }
}
