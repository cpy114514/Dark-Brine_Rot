using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mavis
{
    // Baked ecological placement, shared botanical meshes and the existing GPU wind field.
    // One component for the island; no colliders, renderers or scripts per specimen.
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class IslandEcologyRenderer : MonoBehaviour
    {
        public IslandEcologyData data;
        sealed class Batch
        {
            public IslandEcologyData.Species species;
            public Matrix4x4[] matrices;
            public Bounds bounds;
        }
        readonly List<Batch> batches=new List<Batch>();
        readonly Plane[] planes=new Plane[6];
        MaterialPropertyBlock block;
        Coroutine preparation;
        public int InstanceCount {get;private set;}
        public int VisibleBatches {get;private set;}
        public bool IsReady {get;private set;}
        static readonly int LodFade=Shader.PropertyToID("_EcologyLodFade");

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering+=DrawCamera;
            if(Application.isPlaying)preparation=StartCoroutine(Prepare());
            else {var work=Prepare();while(work.MoveNext()){} }
        }
        IEnumerator Prepare()
        {
            batches.Clear();InstanceCount=0;IsReady=false;
            if(!data||data.species==null)yield break;
            double deadline=Time.realtimeSinceStartupAsDouble+.002;
            foreach(var s in data.species)
            {
                if(s==null||!s.near||!s.far||!s.material||s.cells==null)continue;
                foreach(var cell in s.cells)
                {
                    if(cell.positions==null||cell.normals==null||cell.positions.Length!=cell.normals.Length)continue;
                    for(int start=0;start<cell.positions.Length;start+=256)
                    {
                        int count=Mathf.Min(256,cell.positions.Length-start);var matrices=new Matrix4x4[count];
                        for(int i=0;i<count;i++)
                        {
                            var p=cell.positions[start+i];var n=cell.normals[start+i];
                            matrices[i]=Matrix4x4.TRS(new Vector3(p.x,p.y,p.z),Quaternion.FromToRotation(Vector3.up,new Vector3(n.x,n.y,n.z))*Quaternion.Euler(0,p.w,0),Vector3.one*n.w);
                        }
                        batches.Add(new Batch{species=s,matrices=matrices,bounds=cell.bounds});InstanceCount+=count;
                    }
                    if(Application.isPlaying&&Time.realtimeSinceStartupAsDouble>=deadline){yield return null;deadline=Time.realtimeSinceStartupAsDouble+.002;}
                }
            }
            preparation=null;IsReady=true;
        }
        void DrawCamera(ScriptableRenderContext context,Camera camera)
        {
            if(!isActiveAndEnabled||!camera||!SystemInfo.supportsInstancing||
                (camera.cameraType!=CameraType.Game&&camera.cameraType!=CameraType.SceneView))return;
            GeometryUtility.CalculateFrustumPlanes(camera,planes);VisibleBatches=0;
            foreach(var b in batches)
            {
                var s=b.species;float distance=Mathf.Sqrt(b.bounds.SqrDistance(camera.transform.position));
                if(distance>s.drawDistance||!GeometryUtility.TestPlanesAABB(planes,b.bounds))continue;
                float blend=Mathf.InverseLerp(s.lodStart,s.lodEnd,distance);
                var shadows=distance<s.shadowDistance?ShadowCastingMode.On:ShadowCastingMode.Off;
                if(blend<1)Draw(b,s.near,camera,shadows,blend,1);
                if(blend>0)Draw(b,s.far,camera,shadows,blend,-1);
            }
        }
        void Draw(Batch b,Mesh mesh,Camera camera,ShadowCastingMode shadows,float blend,float side)
        {
            if(block==null)block=new MaterialPropertyBlock();
            block.Clear();block.SetVector(LodFade,new Vector4(blend,side,0,0));
            Graphics.DrawMeshInstanced(mesh,0,b.species.material,b.matrices,b.matrices.Length,block,shadows,true,gameObject.layer,camera,LightProbeUsage.Off);
            VisibleBatches++;
        }
        public void Rebuild() { enabled=false;enabled=true; }
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering-=DrawCamera;
            if(preparation!=null)StopCoroutine(preparation);preparation=null;
            batches.Clear();InstanceCount=0;VisibleBatches=0;IsReady=false;
        }
    }
}
