using UnityEngine;
namespace Mavis
{
    /// <summary>A real hit location, separate from the boss's main body collider.</summary>
    public sealed class BossWeakPoint : MonoBehaviour
    {
        public const float DamageMultiplier=1.5f;
        Renderer marker; Material material; NailongHealth nailong; Health health;
        Texture2D reticle; GUIStyle labelStyle;
        public string LocationLabel { get; set; } = "弱点";
        public SphereCollider Volume { get; private set; }
        public static BossWeakPoint Create(Transform anchor, Vector3 offset, float radius)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Boss weak point";
            go.transform.SetParent(anchor,false);go.transform.localPosition=offset;
            go.transform.localScale=Vector3.one*(radius*2/Mathf.Max(.001f,Mathf.Abs(anchor.lossyScale.y)));
            return go.AddComponent<BossWeakPoint>();
        }
        void Awake()
        {
            Volume=GetComponent<SphereCollider>();Volume.isTrigger=true;
            marker=GetComponent<Renderer>();material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor",new Color(0,1,.8f));marker.sharedMaterial=material;marker.enabled=false;
            marker.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            nailong=GetComponentInParent<NailongHealth>();health=GetComponentInParent<Health>();
        }
        void LateUpdate(){marker.enabled=WeaknessGlasses.AnyActive && !(nailong && nailong.IsDead) && !(health && health.IsDead);}
        // Bone centres can lie inside opaque skin. Project the physical hit volume
        // onto the HUD so glasses can reveal it without changing its collider.
        void OnGUI()
        {
            if(!WeaknessGlasses.AnyActive || SahurLoadoutUI.BlocksInput || PauseSettingsMenu.IsOpen ||
                (nailong && nailong.IsDead) || (health && health.IsDead) || !Volume || !Volume.enabled)return;
            var camera=Camera.main;if(!camera || (camera.cullingMask & (1<<gameObject.layer))==0)return;
            Vector3 centre=Volume.transform.TransformPoint(Volume.center);
            Vector3 screen=camera.WorldToScreenPoint(centre);
            if(screen.z<=camera.nearClipPlane || screen.x<0 || screen.x>Screen.width || screen.y<0 || screen.y>Screen.height)return;
            EnsureReticle();
            float scale=Mathf.Clamp(Screen.height/1080f,.65f,2f);
            float radius=Volume.radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.y),Mathf.Abs(transform.lossyScale.z));
            float projected=Vector2.Distance(screen,camera.WorldToScreenPoint(centre+camera.transform.up*radius))*2;
            float diameter=Mathf.Clamp(projected,36*scale,100*scale);
            float y=Screen.height-screen.y;
            Color previous=GUI.color;
            Color previousContent=GUI.contentColor;int previousDepth=GUI.depth;
            GUI.contentColor=Color.white;GUI.depth=-100;
            GUI.color=new Color(1,1,1,.85f+.15f*Mathf.Sin(Time.unscaledTime*4));
            GUI.DrawTexture(new Rect(screen.x-diameter*.5f,y-diameter*.5f,diameter,diameter),reticle);
            GUI.color=new Color(0,0,0,.8f);
            var label=new Rect(screen.x-100*scale,y+diameter*.5f+4*scale,200*scale,28*scale);
            GUI.DrawTexture(label,Texture2D.whiteTexture);
            GUI.color=new Color(.1f,1,.8f);labelStyle.fontSize=Mathf.RoundToInt(18*scale);
            GUI.Label(label,LocationLabel+"  ×1.5",labelStyle);
            GUI.color=previous;GUI.contentColor=previousContent;GUI.depth=previousDepth;
        }
        void EnsureReticle()
        {
            if(reticle)return;
            const int size=96;reticle=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Weak point HUD ring",filterMode=FilterMode.Bilinear,hideFlags=HideFlags.HideAndDontSave};
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float distance=Vector2.Distance(new Vector2(x+.5f,y+.5f),Vector2.one*(size*.5f));
                float ring=Mathf.Clamp01(2.5f-Mathf.Abs(distance-38));
                float outline=Mathf.Clamp01(4.5f-Mathf.Abs(distance-38));
                float dot=Mathf.Clamp01(3-distance);
                pixels[y*size+x]=Color.Lerp(new Color(0,0,0,outline*.9f),new Color(.05f,1,.8f,1),Mathf.Max(ring,dot));
            }
            reticle.SetPixels(pixels);reticle.Apply(false,true);
            labelStyle=new GUIStyle{font=GameLocalization.Font?GameLocalization.Font:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
            labelStyle.normal.textColor=Color.white;
        }
        void OnDisable(){if(marker)marker.enabled=false;}
        public static bool Contains(Component target,Vector3 point)
        {
            if(!target)return false;
            foreach(var weak in target.GetComponentsInChildren<BossWeakPoint>())
                if(weak.Volume.enabled && (weak.Volume.ClosestPoint(point)-point).sqrMagnitude<.0025f)return true;
            return false;
        }
        void OnDestroy(){if(material)Destroy(material);if(reticle)Destroy(reticle);}
    }
}
