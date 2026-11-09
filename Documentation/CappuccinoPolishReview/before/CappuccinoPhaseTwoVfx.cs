using UnityEngine;
using UnityEngine.Rendering;

namespace Mavis
{
    /// <summary>Bounded mesh effects: four baked silhouettes, one expanding collision-matched ring, two steam ribbons.</summary>
    [DefaultExecutionOrder(18010)]
    public sealed class CappuccinoPhaseTwoVfx : MonoBehaviour
    {
        sealed class Echo
        {
            public Mesh[] meshes; public MeshRenderer[] renderers; public Transform[] frames;
            public Vector3[] positions, scales; public Quaternion[] rotations;
            public bool pending; public float born=-100;
        }
        CappuccinoAI ai;
        Health health;
        SkinnedMeshRenderer[] sources;
        readonly Echo[] echoes=new Echo[4];
        int echoCursor;
        float nextEcho, ringUntil, transitionWeight;
        bool transition, paused;
        Material material;
        Mesh ringMesh, steamMesh;
        Transform ring;
        MeshRenderer ringRenderer;
        readonly MeshRenderer[] steam=new MeshRenderer[2];
        AudioSource voice, music;
        AudioClip charge, release, cut;
        MaterialPropertyBlock block;
        static readonly int Tint=Shader.PropertyToID("_Tint");
        static readonly Color Crema=new Color(1.25f,.76f,.34f,.6f);

        void Awake()
        {
            block=new MaterialPropertyBlock();
            ai=GetComponent<CappuccinoAI>();health=GetComponent<Health>();
            var template=Resources.Load<Material>("CapriPhase2/CremaMesh");
            if(!template)return;
            material=new Material(template);
            var body=GetComponent<CappuccinoEnemy>();var skins=new System.Collections.Generic.List<SkinnedMeshRenderer>();
            if(body&&body.bodyRenderers!=null)foreach(var r in body.bodyRenderers)if(r is SkinnedMeshRenderer skin&&skin.sharedMesh)skins.Add(skin);
            sources=skins.ToArray();
            ringMesh=BuildRing();ring=MeshObject("Crema wave / landing cue",ringMesh,out ringRenderer);
            steamMesh=BuildSteam();
            for(int i=0;i<2;i++)MeshObject("Braided steam ribbon "+i,steamMesh,out steam[i]);
            for(int i=0;i<echoes.Length;i++)
            {
                var echo=new Echo{meshes=new Mesh[sources.Length],renderers=new MeshRenderer[sources.Length],frames=new Transform[sources.Length],
                    positions=new Vector3[sources.Length],rotations=new Quaternion[sources.Length],scales=new Vector3[sources.Length]};
                for(int j=0;j<sources.Length;j++)
                {
                    echo.meshes[j]=new Mesh{name="Capri pooled silhouette"};echo.meshes[j].MarkDynamic();
                    echo.frames[j]=MeshObject("Afterimage "+i+" / "+j,echo.meshes[j],out echo.renderers[j]);
                    // Bake once before combat so the first rush does not allocate vertex buffers.
                    sources[j].BakeMesh(echo.meshes[j],true);
                }
                echoes[i]=echo;
            }
            voice=gameObject.AddComponent<AudioSource>();voice.playOnAwake=false;voice.spatialBlend=1;
            voice.minDistance=5;voice.maxDistance=48;voice.dopplerLevel=0;voice.volume=.65f;
            music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;
            music.spatialBlend=1;music.minDistance=12;music.maxDistance=55;music.volume=0;
            music.clip=Resources.Load<AudioClip>("CapriPhase2/RistrettoRhythm");
            charge=Resources.Load<AudioClip>("CapriPhase2/SteamCharge");
            release=Resources.Load<AudioClip>("CapriPhase2/CremaImpact");
            cut=Resources.Load<AudioClip>("CapriPhase2/BladeAccent");
        }
        Transform MeshObject(string label,Mesh mesh,out MeshRenderer renderer)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;renderer=go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=false;renderer.enabled=false;return go.transform;
        }
        public void TransitionStart(){transition=true;if(voice&&charge)voice.PlayOneShot(charge,.65f);}
        public void TransitionProgress(float progress){transitionWeight=Mathf.Sin(Mathf.Clamp01(progress)*Mathf.PI);}
        public void TransitionEnd(){transition=false;if(music&&music.clip&&!music.isPlaying)music.Play();}
        public void Release()
        {
            Impact(transform.position);
            if(Camera.main)Camera.main.GetComponent<CombatCameraShake>()?.Pulse(.035f,.24f);
        }
        public void AttackCue(CappuccinoAI.AttackKind kind,bool active)
        {
            if(!voice||!GameAudioPolicy.SoundEnabled)return;
            voice.pitch=kind==CappuccinoAI.AttackKind.CoffeeTempest?.85f:kind==CappuccinoAI.AttackKind.ShadowChain?1.12f:1;
            var clip=active?cut:charge;if(clip)voice.PlayOneShot(clip,active?.28f:.22f);
        }
        public void LandingCue(Vector3 point,float radius,float seconds=1.3f){SetRing(point,radius,new Color(.65f,.37f,.15f,.48f));ringUntil=Time.time+seconds;}
        public void Wave(Vector3 point,float radius){SetRing(point,radius,Crema);ringUntil=Time.time+.08f;}
        public void Impact(Vector3 point)
        {
            SetRing(point,.6f,Crema);ringUntil=Time.time+.28f;
            if(voice&&release)voice.PlayOneShot(release,.55f);
            if(Camera.main&&(Camera.main.transform.position-point).sqrMagnitude<1600)
                Camera.main.GetComponent<CombatCameraShake>()?.Pulse(.025f,.18f);
        }
        void SetRing(Vector3 point,float radius,Color color)
        {
            if(!ring)return;
            ring.position=point+Vector3.up*.12f;ring.rotation=Quaternion.identity;
            // Width stays constant as the wave expands, matching the annular damage band.
            ring.localScale=Vector3.one;
            block.Clear();block.SetColor(Tint,color);block.SetFloat("_Radius",radius);block.SetFloat("_Ring",1);
            ringRenderer.SetPropertyBlock(block);ringRenderer.enabled=true;
        }
        public void Afterimage()
        {
            if(sources==null||Time.time<nextEcho||!material)return;
            nextEcho=Time.time+.085f;
            var echo=echoes[echoCursor];echoCursor=(echoCursor+1)%echoes.Length;
            // Deferred until LateUpdate, after native animation and the fitted rig have evaluated.
            echo.pending=true;
        }
        void LateUpdate()
        {
            if(!material)return;
            bool pause=PauseSettingsMenu.IsOpen||Time.timeScale<=0;
            if(pause!=paused)
            {
                paused=pause;
                if(pause){voice.Pause();music.Pause();}else{voice.UnPause();music.UnPause();}
            }
            if(pause)return;
            if(!ai||!ai.isActiveAndEnabled||!ai.FightActive||health.IsDead){EndEncounter();return;}
            if(ai.FightPhase==2 && !transition && music.clip&&!music.isPlaying)music.Play();
            music.volume=Mathf.MoveTowards(music.volume,ai.FightPhase==2?.2f:0,Time.deltaTime*.2f);
            if(ringRenderer.enabled&&Time.time>=ringUntil)ringRenderer.enabled=false;
            for(int e=0;e<echoes.Length;e++)
            {
                var echo=echoes[e];if(echo==null)continue;
                if(echo.pending)
                {
                    echo.born=Time.time;echo.pending=false;
                    for(int j=0;j<sources.Length;j++)
                    {
                        // Unity 6.3 needs useScale=true to keep this in renderer-local
                        // space. The frozen transform applies the fitted 2.52x scale once.
                        sources[j].BakeMesh(echo.meshes[j],true);
                        echo.positions[j]=sources[j].transform.position;echo.rotations[j]=sources[j].transform.rotation;
                        echo.scales[j]=sources[j].transform.lossyScale/Mathf.Max(.001f,transform.lossyScale.x);
                    }
                }
                float alpha=Mathf.Clamp01(1-(Time.time-echo.born)/.24f);
                block.Clear();block.SetColor(Tint,new Color(.65f,.42f,.24f,alpha*.22f));block.SetFloat("_Ring",0);
                for(int j=0;j<echo.renderers.Length;j++)
                {
                    echo.frames[j].SetPositionAndRotation(echo.positions[j],echo.rotations[j]);echo.frames[j].localScale=echo.scales[j];
                    echo.renderers[j].enabled=alpha>0;echo.renderers[j].SetPropertyBlock(block);
                }
            }
            for(int i=0;i<steam.Length;i++)
            {
                steam[i].enabled=transition;
                if(!transition)continue;
                steam[i].transform.localPosition=Vector3.up*.2f;
                steam[i].transform.localRotation=Quaternion.Euler(0,Time.time*48+i*180,0);
                block.Clear();block.SetColor(Tint,new Color(.72f,.55f,.36f,transitionWeight*.35f));block.SetFloat("_Ring",0);steam[i].SetPropertyBlock(block);
            }
        }
        public void ClearAction()
        {
            if(ringRenderer)ringRenderer.enabled=false;
            foreach(var echo in echoes)if(echo!=null){echo.born=-100;echo.pending=false;foreach(var r in echo.renderers)if(r)r.enabled=false;}
        }
        public void EndEncounter()
        {
            ClearAction();transition=false;transitionWeight=0;
            foreach(var r in steam)if(r)r.enabled=false;
            if(music){music.Stop();music.volume=0;}
            if(voice)voice.Stop();
        }
        void OnDisable()=>EndEncounter();
        void OnDestroy()
        {
            if(material)Destroy(material);if(ringMesh)Destroy(ringMesh);if(steamMesh)Destroy(steamMesh);
            foreach(var e in echoes)if(e!=null)foreach(var m in e.meshes)if(m)Destroy(m);
        }
        static Mesh BuildRing()
        {
            const int count=96;var v=new Vector3[(count+1)*4];var colors=new Color[v.Length];var tris=new int[count*18];
            for(int i=0;i<=count;i++)for(int j=0;j<4;j++)
            {
                float angle=i*Mathf.PI*2/count;float width=(j/3f-.5f)*1.2f;
                v[i*4+j]=new Vector3(Mathf.Sin(angle),width,Mathf.Cos(angle));
                colors[i*4+j]=new Color(1,1,1,j==0||j==3?0:1);
            }
            int n=0;for(int i=0;i<count;i++)for(int j=0;j<3;j++){int a=i*4+j,b=a+4;tris[n++]=a;tris[n++]=b;tris[n++]=a+1;tris[n++]=a+1;tris[n++]=b;tris[n++]=b+1;}
            var mesh=new Mesh{name="Crema feathered annulus"};mesh.vertices=v;mesh.colors=colors;mesh.triangles=tris;mesh.bounds=new Bounds(Vector3.zero,Vector3.one*40);return mesh;
        }
        static Mesh BuildSteam()
        {
            const int count=48;var v=new Vector3[(count+1)*2];var colors=new Color[v.Length];var tris=new int[count*6];
            for(int i=0;i<=count;i++)for(int j=0;j<2;j++)
            {
                float t=i/(float)count,a=t*Mathf.PI*3,r=2.4f+Mathf.Sin(t*Mathf.PI)*.65f;
                v[i*2+j]=new Vector3(Mathf.Sin(a)*r,t*6+(j-.5f)*.5f,Mathf.Cos(a)*r);
                colors[i*2+j]=new Color(1,1,1,Mathf.Sin(t*Mathf.PI)*(j==0?.1f:.8f));
                if(i<count&&j==0){int n=i*6,k=i*2;tris[n]=k;tris[n+1]=k+2;tris[n+2]=k+1;tris[n+3]=k+1;tris[n+4]=k+2;tris[n+5]=k+3;}
            }
            var mesh=new Mesh{name="Crema steam helix"};mesh.vertices=v;mesh.colors=colors;mesh.triangles=tris;mesh.RecalculateNormals();return mesh;
        }
    }
}
