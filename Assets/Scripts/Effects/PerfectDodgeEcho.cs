using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace Mavis
{
    /// <summary>Two pooled, world-anchored body impressions for an actual perfect dodge.</summary>
    [DisallowMultipleComponent,DefaultExecutionOrder(18500)]
    public sealed class PerfectDodgeEcho:MonoBehaviour
    {
        readonly Mesh[] meshes=new Mesh[2];
        readonly MeshRenderer[] echoes=new MeshRenderer[2];
        readonly float[] born={-100f,-100f};
        SkinnedMeshRenderer source;
        Material material;
        MaterialPropertyBlock properties;
        float began=-100;
        int captured=2;
        static readonly int Fade=Shader.PropertyToID("_Fade"),Life=Shader.PropertyToID("_Life");
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
                echoes[i].sharedMaterial=material;echoes[i].shadowCastingMode=ShadowCastingMode.Off;echoes[i].receiveShadows=false;echoes[i].lightProbeUsage=LightProbeUsage.Off;echoes[i].enabled=false;
            }
        }
        public void Begin()
        {
            if(!source||!material)return;
            Hide();began=Time.time;
            // Preserve the impression where the attack was aimed, BEFORE the
            // CharacterController moves or the perfect-step animation starts.
            Capture(0);captured=1;
        }
        void Capture(int i)
        {
            source.BakeMesh(meshes[i],true);
            echoes[i].transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
            echoes[i].transform.localScale=source.transform.lossyScale;born[i]=Time.time;echoes[i].enabled=true;
        }
        void LateUpdate()
        {
            if(!source||!material)return;
            if(GameControls.MenusOpen){Hide();return;}
            if(captured==1&&Time.time-began>=.055f){Capture(1);captured=2;}
            for(int i=0;i<2;i++)
            {
                float life=Mathf.Clamp01((Time.time-born[i])/(i==0?.34f:.20f));
                if(life>=1){echoes[i].enabled=false;continue;}
                float fade=(1-Mathf.SmoothStep(0,1,life))*(i==0?.52f:.28f);
                properties.SetFloat(Fade,fade);properties.SetFloat(Life,life);echoes[i].SetPropertyBlock(properties);
            }
        }
        void Hide(){captured=2;for(int i=0;i<2;i++){born[i]=-100;if(echoes[i])echoes[i].enabled=false;}}
        void OnDisable()=>Hide();
        void OnDestroy()
        {
            for(int i=0;i<2;i++){if(echoes[i])Destroy(echoes[i].gameObject);if(meshes[i])Destroy(meshes[i]);}
            if(material)Destroy(material);
        }
    }
}
