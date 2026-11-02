using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace Mavis
{
    [DisallowMultipleComponent,DefaultExecutionOrder(18500)]
    public sealed class PerfectDodgeEcho:MonoBehaviour
    {
        readonly Mesh[] meshes=new Mesh[2];
        readonly MeshRenderer[] echoes=new MeshRenderer[2];
        readonly float[] born=new float[2];
        SkinnedMeshRenderer source;
        Material material;
        MaterialPropertyBlock properties;
        float began=-100;
        int captured=2;
        static readonly int Fade=Shader.PropertyToID("_Fade");
        void Awake()
        {
            source=GetComponentInChildren<SkinnedMeshRenderer>();properties=new MaterialPropertyBlock();
            var template=Resources.Load<Material>("Combat/PerfectDodge/Echo");if(!source||!template)return;
            material=Instantiate(template);material.hideFlags=HideFlags.DontSave;
            for(int i=0;i<2;i++)
            {
                var go=new GameObject("Perfect dodge body echo "+i,typeof(MeshFilter),typeof(MeshRenderer));
                SceneManager.MoveGameObjectToScene(go,gameObject.scene);
                meshes[i]=new Mesh{name="Pooled dodge silhouette "+i};meshes[i].MarkDynamic();
                go.GetComponent<MeshFilter>().sharedMesh=meshes[i];echoes[i]=go.GetComponent<MeshRenderer>();
                echoes[i].sharedMaterial=material;echoes[i].shadowCastingMode=ShadowCastingMode.Off;echoes[i].receiveShadows=false;echoes[i].enabled=false;
            }
        }
        public void Begin(){began=Time.time;captured=0;}
        void LateUpdate()
        {
            if(!source||!material)return;
            if(GameControls.MenusOpen){Hide();return;}
            if(captured<2&&Time.time-began>=(captured==0 ? .025f : .085f))
            {
                int i=captured++;source.BakeMesh(meshes[i],true);
                echoes[i].transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
                echoes[i].transform.localScale=source.transform.lossyScale;born[i]=Time.time;echoes[i].enabled=true;
            }
            for(int i=0;i<2;i++)
            {
                float life=1-(Time.time-born[i])/.14f;
                if(life<=0){echoes[i].enabled=false;continue;}
                properties.SetFloat(Fade,Mathf.SmoothStep(0,1,life)*.14f);echoes[i].SetPropertyBlock(properties);
            }
        }
        void Hide(){captured=2;for(int i=0;i<2;i++)if(echoes[i])echoes[i].enabled=false;}
        void OnDisable()=>Hide();
        void OnDestroy()
        {
            for(int i=0;i<2;i++){if(echoes[i])Destroy(echoes[i].gameObject);if(meshes[i])Destroy(meshes[i]);}
            if(material)Destroy(material);
        }
    }
}
