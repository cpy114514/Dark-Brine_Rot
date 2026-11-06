using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;
using Object=UnityEngine.Object;

// Deliberately separate asset construction, bounded sample, validation, expansion and save.
// Existing source assets, colliders, NPCs, quests and terrain are never regenerated.
public static class BuildIslandEcology074
{
    const string Folder="Assets/Generated/IslandEcology074";
    const string W=".codex/island-ecology074";
    const string ScenePath="Assets/Scenes/First Island/Environment.unity";
    const string RootName="Island 1 - Coastal woodland ecology";
    static readonly Vector2 Sample=new Vector2(251,469);
    static readonly Color Fern=new Color(.20f,.37f,.14f),Sage=new Color(.34f,.43f,.24f),Dark=new Color(.16f,.19f,.23f),Bark=new Color(.24f,.17f,.105f);
    static void Json(string n,object v){Directory.CreateDirectory(W);File.WriteAllText(W+"/"+n+".json",Newtonsoft.Json.JsonConvert.SerializeObject(v,Newtonsoft.Json.Formatting.Indented));}
    static float R(System.Random r,float a=0,float b=1)=>Mathf.Lerp(a,b,(float)r.NextDouble());
    static Vector3 Rad(float a)=>new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
    static T Asset<T>(T value,string name) where T:Object
    {
        value.name=System.IO.Path.GetFileNameWithoutExtension(name);
        string p=Folder+"/"+name;var old=AssetDatabase.LoadAssetAtPath<T>(p);
        if(old){EditorUtility.CopySerialized(value,old);Object.DestroyImmediate(value);EditorUtility.SetDirty(old);return old;}
        AssetDatabase.CreateAsset(value,p);return value;
    }
    sealed class Shape
    {
        public readonly List<Vector3> v=new List<Vector3>();public readonly List<Vector2> uv=new List<Vector2>();
        public readonly List<Color> c=new List<Color>();public readonly List<int> t=new List<int>();
        public void Vertex(Vector3 p,Vector2 tex,Color color){v.Add(p);uv.Add(tex);c.Add(color);}
        void Quad(int a,int b,int d,int e){t.AddRange(new[]{a,d,b,b,d,e});}
        public void Leaf(Vector3 root,Vector3 direction,float length,float width,float curl,Color color,int steps=6,float serration=.04f)
        {
            direction.Normalize();var side=Vector3.Cross(direction,Vector3.up).normalized;if(side.sqrMagnitude<.1f)side=Vector3.right;
            int start=v.Count;
            for(int i=0;i<=steps;i++)
            {
                float h=i/(float)steps;var center=root+direction*length*h+Vector3.up*(Mathf.Sin(h*Mathf.PI)*length*.10f-curl*h*h);
                float profile=Mathf.Pow(Mathf.Max(0,Mathf.Sin(h*Mathf.PI)),.78f)*(1+(i%2==0?serration:-serration));
                for(int j=-1;j<=1;j++)
                {
                    var p=center+side*(width*.5f*profile*j)+Vector3.up*(j==0?width*.065f*profile:0);
                    Vertex(p,new Vector2((j+1)*.5f,h),color*(Mathf.Lerp(.68f,1,h)*(j==0?1.04f:1)));
                }
                if(i>0){int p=start+(i-1)*3;Quad(p,p+1,p+3,p+4);Quad(p+1,p+2,p+4,p+5);}
            }
        }
        public void Tube(Vector3[] points,float radius,Color color,int sides=6,bool taper=true)
        {
            int start=v.Count;
            for(int i=0;i<points.Length;i++)
            {
                var axis=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(0,i-1)]).normalized;
                var right=Vector3.Cross(axis,Mathf.Abs(axis.y)>.95f?Vector3.forward:Vector3.up).normalized;var forward=Vector3.Cross(right,axis);
                float h=i/(float)(points.Length-1),size=radius*(taper?Mathf.Lerp(1,.16f,h):1);
                for(int j=0;j<=sides;j++){float a=j/(float)sides*Mathf.PI*2;Vertex(points[i]+(right*Mathf.Cos(a)+forward*Mathf.Sin(a))*size,new Vector2(j/(float)sides,h),color);}
                if(i>0)for(int j=0;j<sides;j++){int a=start+(i-1)*(sides+1)+j;Quad(a,a+1,a+sides+1,a+sides+2);}
            }
        }
        public Mesh Mesh(string name,bool rigid=false)
        {
            if(v.Any(p=>float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z)))throw new Exception("Nonfinite botanical geometry: "+name);
            float height=Mathf.Max(.05f,v.Max(p=>p.y));
            for(int i=0;i<c.Count;i++){var color=c[i];color.a=rigid?0:Mathf.Clamp01(v[i].y/height);c[i]=color;}
            var m=new Mesh{name=name,indexFormat=v.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};m.SetVertices(v);m.SetUVs(0,uv);m.SetColors(c);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();
            var bounds=m.bounds;bounds.Expand(new Vector3(1.3f,.4f,1.3f));m.bounds=bounds;return m;
        }
    }
    static Shape FernMesh(System.Random r,bool far,bool uncanny)
    {
        var m=new Shape();int fronds=far?4:8,pairs=far?5:9;
        for(int f=0;f<fronds;f++)
        {
            float a=f*Mathf.PI*2/fronds+R(r,-.15f,.15f);var outwards=Rad(a);var side=Rad(a+Mathf.PI*.5f);float len=R(r,1.8f,2.9f);
            Vector3 P(float h)=>outwards*(len*h*.76f)+Vector3.up*(len*(Mathf.Sin(h*1.7f)*.65f-h*h*.13f));
            m.Tube(Enumerable.Range(0,9).Select(i=>P(i/8f)).ToArray(),.025f,uncanny?Dark:Fern,far?4:5);
            for(int p=1;p<=pairs;p++)
            {
                float h=.13f+p/(float)(pairs+1)*.8f;float l=len*.29f*Mathf.Sin(h*Mathf.PI)*R(r,.85f,1.1f);
                for(int sign=-1;sign<=1;sign+=2)m.Leaf(P(h),side*sign+outwards*.18f+Vector3.up*.12f,l,l*.27f,uncanny?l*.8f:l*.15f,uncanny?new Color(.23f,.23f,.29f):Fern,far?3:6,uncanny?.16f:.07f);
            }
        }
        return m;
    }
    static Shape Broadleaf(System.Random r,bool far,bool uncanny,bool tongue=false)
    {
        var m=new Shape();int leaves=far?5:11;
        for(int i=0;i<leaves;i++)
        {
            float a=i*2.39996f+R(r,-.3f,.3f);var dir=Rad(a);float len=R(r,1.1f,2.1f);var root=dir*R(r,.02f,.2f)+Vector3.up*.015f;
            m.Leaf(root,dir*(tongue?.4f:.65f)+Vector3.up*R(r,.4f,1.1f),len,len*(tongue?.19f:uncanny?.32f:.40f),uncanny?len*.6f:len*.28f,uncanny?Dark:new Color(.22f,.39f,.22f),far?4:9,tongue?.12f:uncanny?.12f:.025f);
        }return m;
    }
    static Shape Shrub(System.Random r,bool far,bool tree,bool coastal)
    {
        var m=new Shape();int branches=far?4:tree?10:12;float height=tree?7.5f:coastal?1.9f:3.1f;
        m.Tube(new[]{Vector3.zero,new Vector3(.04f,height*.38f,0),new Vector3(-.14f,height*.76f,.12f),new Vector3(.18f,height,0)},tree?.14f:.07f,Bark,far?5:8);
        for(int b=0;b<branches;b++)
        {
            float a=b*2.39996f;var dir=Rad(a);float h=tree?R(r,.33f,.85f):R(r,.10f,.7f);var root=new Vector3(0,height*h,0);
            float length=tree?R(r,1.7f,3.0f):R(r,.8f,1.5f);var tip=root+dir*length+Vector3.up*length*.45f;
            m.Tube(new[]{root,Vector3.Lerp(root,tip,.5f)+Vector3.up*.15f,tip},tree?.055f:.035f,Bark,far?4:6);
            int leaves=far?5:tree?18:13;
            for(int l=0;l<leaves;l++)
            {
                float t=R(r,.25f,1);var p=Vector3.Lerp(root,tip,t)+Rad(l*2.4f)*R(r,0,tree?.55f:.28f);
                var leafDir=Rad(a+l*2.4f)+Vector3.up*R(r,-.12f,.5f);float len=tree?R(r,.45f,.95f):R(r,.42f,.78f);
                m.Leaf(p,leafDir,len,len*(coastal?.25f:.45f),len*.18f,coastal?Sage:Fern,far?3:5,.06f);
            }
        }return m;
    }
    static Shape Flowers(System.Random r,bool far,int kind)
    {
        var m=new Shape();int flowers=far?3:7;
        Color petal=kind==0?new Color(.74f,.63f,.38f):kind==1?new Color(.40f,.31f,.53f):new Color(.70f,.69f,.59f);
        for(int f=0;f<flowers;f++)
        {
            var root=Rad(f*2.4f)*R(r,.05f,.62f);float height=R(r,1.25f,2.6f);var lean=Rad(R(r,0,6.28f))*.25f;var tip=root+Vector3.up*height+lean;
            m.Tube(new[]{root,root+Vector3.up*height*.45f+lean*.2f,tip},.023f,Fern,far?4:5);
            if(!far)for(int l=0;l<3;l++)m.Leaf(Vector3.Lerp(root,tip,.2f+l*.17f),Rad(f+l*2.4f)+Vector3.up*.3f,.55f,.16f,.11f,Fern,4);
            int petals=kind==0?7:5;float size=kind==1?.26f:.22f;
            for(int p=0;p<petals;p++)m.Leaf(tip,Rad(p*6.283f/petals)+Vector3.up*.25f,size,size*.6f,-size*.2f,petal,far?3:6);
            m.Tube(new[]{tip-Vector3.up*.035f,tip+Vector3.up*.045f},.055f,new Color(.43f,.29f,.10f),far?5:8,false);
        }return m;
    }
    static Shape Mushroom(System.Random r,bool far,bool uncanny)
    {
        var m=new Shape();int count=far?3:6;
        for(int f=0;f<count;f++)
        {
            var root=Rad(f*2.4f)*R(r,.08f,.75f);float height=R(r,.35f,uncanny?1.9f:.95f),radius=height*R(r,.36f,.6f);var tip=root+Vector3.up*height+new Vector3(height*.12f,0,0);
            m.Tube(new[]{root,Vector3.Lerp(root,tip,.5f)+Vector3.right*.06f,tip},height*.09f,new Color(.57f,.51f,.38f),far?5:8,false);
            int sides=far?10:22,rings=far?4:8;int start=m.v.Count;
            for(int j=0;j<=rings;j++)
            {
                float h=j/(float)rings;float radial=Mathf.Sin(h*Mathf.PI*.5f)*radius;
                for(int k=0;k<=sides;k++)
                {
                    float a=k/(float)sides*6.283f;float ripple=1+Mathf.Sin(a*(uncanny?5:3)+f)*.055f*h;
                    var p=tip+Rad(a)*radial*ripple+Vector3.up*(radius*(Mathf.Cos(h*1.57f)*.65f-.12f*h)+(uncanny?Mathf.Sin(a*3)*.1f*h:0));
                    var col=uncanny?Color.Lerp(new Color(.21f,.25f,.24f),new Color(.36f,.50f,.46f),h):Color.Lerp(new Color(.33f,.19f,.10f),new Color(.64f,.47f,.29f),h);
                    m.Vertex(p,new Vector2(k/(float)sides,h),col);
                    if(j>0&&k>0){int x=start+(j-1)*(sides+1)+k-1;m.t.AddRange(new[]{x,x+sides+1,x+1,x+1,x+sides+1,x+sides+2});}
                }
            }
            if(!far)for(int g=0;g<16;g++)
            {
                float a=g*6.283f/16;var d=Rad(a);m.Leaf(tip-Vector3.up*.05f,d,radius*.95f,.022f,.12f,new Color(.48f,.44f,.34f),3);
            }
        }return m;
    }
    static Shape Vine(System.Random r,bool far,bool bud)
    {
        var m=new Shape();int count=bud?3:far?2:4;
        for(int b=0;b<count;b++)
        {
            float angle=b*2.4f;var d=Rad(angle);var side=Rad(angle+1.57f);float height=bud?R(r,2.7f,4):R(r,1.9f,3.2f);
            Vector3 P(float h)=>d*(h*.7f+Mathf.Sin(h*4.8f)*.32f)+side*(Mathf.Cos(h*5.4f)*.22f-.22f)+Vector3.up*(height*h);
            m.Tube(Enumerable.Range(0,far?9:19).Select(i=>P(i/(float)(far?8:18))).ToArray(),bud?.075f:.042f,Dark,far?5:8);
            int leaves=far?3:7;
            for(int l=0;l<leaves;l++){float h=.17f+l/(float)leaves*.75f;m.Leaf(P(h),Rad(l*2.4f+angle)+Vector3.up*.2f,bud?.85f:.60f,bud?.28f:.15f,.38f,Dark,far?3:7,.08f);}
            if(bud)for(int p=0;p<7;p++)
            {
                var dir=Rad(p*6.283f/7);m.Leaf(P(1)+dir*.08f,dir*.28f+Vector3.up,.95f,.48f,.6f,new Color(.30f,.16f,.23f),far?4:9,.025f);
            }
            else
            {
                var tip=P(1);var curl=Enumerable.Range(0,far?10:24).Select(i=>{float t=i/(float)(far?9:23);float a=t*6.8f;return tip+d*(Mathf.Sin(a)*(1-t)*.6f)+Vector3.up*(.5f+(Mathf.Cos(a)-1)*(1-t)*.6f);}).ToArray();
                curl[0]=tip;m.Tube(curl,.035f,Dark,far?4:6);
            }
        }return m;
    }
    static Shape Groundcover(System.Random r,bool far,int kind)
    {
        var m=new Shape();int count=far?9:kind==0?34:19;
        if(kind==2)
        {
            for(int b=0;b<(far?2:5);b++){var dir=Rad(R(r,0,6.28f));float len=R(r,.8f,2.2f);var root=Rad(R(r,0,6.28f))*R(r,0,.5f)+Vector3.up*.055f;
                m.Tube(new[]{root,root+dir*len*.55f+Vector3.up*.12f,root+dir*len},.035f,Bark,far?4:7);
                m.Tube(new[]{root+dir*len*.45f,root+dir*len*.65f+Rad(b)*.45f+Vector3.up*.06f},.022f,Bark,far?4:5);}
        }
        else for(int l=0;l<count;l++)
        {
            var root=Rad(R(r,0,6.28f))*Mathf.Sqrt(R(r))*(kind==0?.9f:1.2f)+Vector3.up*.008f;
            float len=kind==0?R(r,.15f,.35f):R(r,.22f,.48f);var dir=Rad(l*2.4f)+Vector3.up*(kind==0?.5f:.04f);
            m.Leaf(root,dir,len,len*(kind==0?.30f:.45f),kind==0?.05f:.02f,kind==0?Sage:Color.Lerp(new Color(.20f,.12f,.06f),new Color(.48f,.30f,.12f),R(r)),far?3:5,.1f);
        }return m;
    }
    static Shape Sedge(System.Random r,bool far,bool shortGrass)
    {
        var m=new Shape();int count=far?8:shortGrass?25:22;
        for(int b=0;b<count;b++)
        {
            float a=b*2.4f;float h=shortGrass?R(r,.3f,.85f):R(r,1.2f,2.9f);
            m.Leaf(Rad(a)*R(r,.05f,.28f),Rad(a)*.32f+Vector3.up,h,shortGrass?.04f:.07f,h*.12f,shortGrass?Sage:new Color(.43f,.46f,.25f),far?3:7,.015f);
            if(!shortGrass&&b%4==0){var tip=Rad(a)*(h*.32f)+Vector3.up*(h*.86f);m.Tube(new[]{tip,tip+Vector3.up*.32f},.025f,new Color(.49f,.36f,.18f),far?4:6);}
        }return m;
    }
    public static object BuildAssets()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();BuildTextures();
        var previous=AssetDatabase.LoadAssetAtPath<IslandEcologyData>(Folder+"/Ecology.asset");
        string[] names={"Saltwind sedge","Low fescue","Broadleaf woodrush","Hart tongue fern","Bracken fern","Coastal sage shrub","Woodland hazel shrub","Rowan sapling","Wind bent alder sapling","Ochre meadow flower","Heather bell flower","Ivory woodland flower","Chestnut fungi","Cushion moss","Copper leaf litter","Fallen twigs","Ink frond fern","Tidal lantern fungi","Coiled brine vine","Dusky seedpod"};
        var species=new List<IslandEcologyData.Species>();
        for(int i=0;i<names.Length;i++)
        {
            Shape Generate(bool far){var r=new System.Random(7403+i*97);switch(i){
                case 0:return Sedge(r,far,false);case 1:return Sedge(r,far,true);case 2:return Broadleaf(r,far,false);
                case 3:return Broadleaf(r,far,false,true);case 4:return FernMesh(r,far,false);
                case 5:return Shrub(r,far,false,true);case 6:return Shrub(r,far,false,false);
                case 7:return Shrub(r,far,true,false);case 8:return Shrub(r,far,true,true);
                case 9:return Flowers(r,far,0);case 10:return Flowers(r,far,1);case 11:return Flowers(r,far,2);
                case 12:return Mushroom(r,far,false);case 13:return Groundcover(r,far,0);case 14:return Groundcover(r,far,1);case 15:return Groundcover(r,far,2);
                case 16:return FernMesh(r,far,true);case 17:return Mushroom(r,far,true);case 18:return Vine(r,far,false);default:return Vine(r,far,true);}}
            var near=Asset(Generate(false).Mesh(names[i]+" LOD0",i>=13&&i<=15),i+"-near.asset");
            var far=Asset(Generate(true).Mesh(names[i]+" LOD1",i>=13&&i<=15),i+"-far.asset");
            var mat=new Material(Shader.Find("Mavis/FoliageWind")){name=names[i]+" botanical",enableInstancing=true};
            mat.SetFloat("_BotanicalSurface",1);mat.SetFloat("_UseBladeHeight",1);mat.SetFloat("_Cull",0);mat.SetFloat("_Cutoff",.05f);mat.SetColor("_BaseColor",Color.white);
            string texture=i>=9&&i<=11?"petal":i==12||i==17?"fungus":i==15?"bark":"leaf";
            mat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/"+texture+"-base.png"));
            mat.SetTexture("_BaseMap",mat.GetTexture("_MainTex"));mat.SetTexture("_BotanicalNormal",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/"+texture+"-normal.png"));mat.SetTexture("_BotanicalSurfaceMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/"+texture+"-surface.png"));
            mat.SetFloat("_Smoothness",i==17?.8f:.55f);mat.SetFloat("_WindBend",i>=13&&i<=15?0:i==12||i==17?.035f:i==7||i==8?.16f:i>=16?.17f:.32f);
            mat.SetFloat("_WindFrequency",i==0||i==1?1.65f:i>=16?.32f:i==7||i==8?.45f:.8f);
            mat.SetFloat("_WindTrunkStiffness",i==7||i==8?.8f:.28f);mat.SetFloat("_PlayerPushStrength",i==7||i==8||i==12||i==17?.02f:i>=13&&i<=15?0:.32f);
            float distance=i==7||i==8?300:i==13||i==14||i==15?65:165;
            mat.SetVector("_EcologyDistance",new Vector4(distance*.8f,distance,0,0));
            if(i==17)mat.SetColor("_BotanicalEmission",new Color(.035f,.095f,.075f));
            if(i==19)mat.SetColor("_BotanicalEmission",new Color(.035f,.012f,.018f));
            mat=Asset(mat,i+".mat");
            species.Add(new IslandEcologyData.Species{name=names[i],uncanny=i>=16,near=near,far=far,material=mat,drawDistance=distance,lodStart=i==7||i==8?65:30,lodEnd=i==7||i==8?85:46,shadowDistance=i==7||i==8?100:40,cells=previous&&previous.species!=null&&i<previous.species.Length?previous.species[i].cells:Array.Empty<IslandEcologyData.Cell>()});
        }
        var data=ScriptableObject.CreateInstance<IslandEcologyData>();data.species=species.ToArray();data.scope=previous?previous.scope:"Unplaced botanical library";Asset(data,"Ecology.asset");AssetDatabase.SaveAssets();
        var result=species.Select(s=>new{s.name,s.uncanny,nearTriangles=s.near.triangles.Length/3,farTriangles=s.far.triangles.Length/3}).ToArray();Json("botanical-library",result);return result;
    }
    static void BuildTextures()
    {
        const int size=256;
        foreach(string kind in new[]{"leaf","petal","fungus","bark"})
        {
            var heights=new float[size*size];var colors=new Color[size*size];var surfaces=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)(size-1),v=y/(float)(size-1),noise=Mathf.PerlinNoise(u*71+7,v*87+3),broad=Mathf.PerlinNoise(u*9+23,v*13+41);
                float vein=Mathf.Exp(-Mathf.Abs(u-.5f)*170);
                float branches=Mathf.Pow(Mathf.Max(0,Mathf.Cos((v-Mathf.Abs(u-.5f)*.65f)*Mathf.PI*28)),24)*Mathf.Pow(1-Mathf.Abs(u-.5f)*2,.4f);
                float relief=vein*.22f+branches*.10f+(noise-.5f)*.13f;
                if(kind=="fungus")relief=Mathf.Sin(u*6.283f*37)*.04f+(noise-.5f)*.22f;
                if(kind=="bark")relief=Mathf.Pow(Mathf.PerlinNoise(u*29,v*3),2)*.65f+(noise-.5f)*.12f;
                heights[y*size+x]=relief;
                float value=.72f+broad*.20f+relief*.22f;
                if(kind=="petal")value=.82f+broad*.13f+branches*.05f;
                colors[y*size+x]=new Color(value,value*(kind=="bark"?.94f:.99f),value*(kind=="bark"?.83f:.96f),1);
                surfaces[y*size+x]=new Color(kind=="bark"?.88f:kind=="fungus"?.5f:.68f,.85f+.15f*broad,kind=="bark"?.02f:.5f,kind=="fungus"?Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*6.283f*37)),8)*Mathf.Pow(v,3):vein*.14f);
            }
            var normals=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=heights[y*size+Mathf.Min(size-1,x+1)]-heights[y*size+Mathf.Max(0,x-1)];
                float dy=heights[Mathf.Min(size-1,y+1)*size+x]-heights[Mathf.Max(0,y-1)*size+x];
                var n=new Vector3(-dx*2,-dy*2,1).normalized;normals[y*size+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
            }
            void Write(string suffix,Color[] pixels,bool linear)
            {
                var t=new Texture2D(size,size,TextureFormat.RGBA32,false,linear);t.SetPixels(pixels);t.Apply();string p=Folder+"/"+kind+"-"+suffix+".png";File.WriteAllBytes(p,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(p);
                var imp=(TextureImporter)AssetImporter.GetAtPath(p);imp.maxTextureSize=size;imp.mipmapEnabled=true;imp.wrapMode=TextureWrapMode.Repeat;imp.sRGBTexture=!linear;imp.textureCompression=TextureImporterCompression.Compressed;imp.anisoLevel=4;
                if(suffix=="normal")imp.textureType=TextureImporterType.NormalMap;imp.SaveAndReimport();
            }
            Write("base",colors,false);Write("normal",normals,true);Write("surface",surfaces,true);
        }
    }

    static IslandEcologySettings Settings()
    {
        var p=Folder+"/Settings.asset";var settings=AssetDatabase.LoadAssetAtPath<IslandEcologySettings>(p);if(settings)return settings;
        settings=ScriptableObject.CreateInstance<IslandEcologySettings>();
        settings.clearings=new[]{
            new IslandEcologySettings.Clearing{reason="Cappuccino combat space",center=new Vector2(239,498),radius=29},
            new IslandEcologySettings.Clearing{reason="Bombardino combat space",center=new Vector2(109,636),radius=42},
            new IslandEcologySettings.Clearing{reason="Nailong combat space",center=new Vector2(18,868),radius=36},
            new IslandEcologySettings.Clearing{reason="Shore supply and sightline",center=new Vector2(269,405),radius=9},
            new IslandEcologySettings.Clearing{reason="Ridge supply",center=new Vector2(215,545),radius=9},
            new IslandEcologySettings.Clearing{reason="North supply",center=new Vector2(50,765),radius=9},
            new IslandEcologySettings.Clearing{reason="Respawn",center=new Vector2(16.5f,593.6f),radius=12},
            new IslandEcologySettings.Clearing{reason="Authored player start",center=new Vector2(279,357),radius=12}};
        settings.corridors=new[]{
            new IslandEcologySettings.Corridor{reason="Shore camp to ridge supply",halfWidth=4.5f,points=new[]{new Vector2(307,319),new Vector2(279,357),new Vector2(269,405),new Vector2(269,415),new Vector2(252,454),new Vector2(230,515),new Vector2(215,545)}},
            new IslandEcologySettings.Corridor{reason="Ridge to lookout / boss approach",halfWidth=5,points=new[]{new Vector2(215,545),new Vector2(190,580),new Vector2(155,619),new Vector2(109,636),new Vector2(73,699),new Vector2(50,765),new Vector2(18,868)}}};
        AssetDatabase.CreateAsset(settings,p);return settings;
    }
    static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b){var d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.001f,d.sqrMagnitude)));}
    static float CorridorDistance(Vector2 p,IslandEcologySettings settings)
    {
        float min=float.MaxValue;if(settings.corridors!=null)foreach(var c in settings.corridors)for(int i=1;i<c.points.Length;i++)min=Mathf.Min(min,SegmentDistance(p,c.points[i-1],c.points[i])-c.halfWidth);return min;
    }
    static bool Protected(Vector2 p,IslandEcologySettings settings,float margin)
    {
        if(settings.clearings!=null&&settings.clearings.Any(c=>Vector2.Distance(p,c.center)<c.radius+margin))return true;
        return CorridorDistance(p,settings)<margin;
    }
    static MeshCollider Ground()=>Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).First(i=>i.isActiveAndEnabled&&i.gameObject.scene.path==ScenePath).GetComponent<MeshCollider>();
    static IslandEcologyRenderer Renderer()
    {
        var scene=SceneManager.GetSceneByPath(ScenePath);var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==RootName);
        if(!root){root=new GameObject(RootName);SceneManager.MoveGameObjectToScene(root,scene);Undo.RegisterCreatedObjectUndo(root,"Add island ecological layer");}
        return root.GetComponent<IslandEcologyRenderer>()??root.AddComponent<IslandEcologyRenderer>();
    }
    static Dictionary<Vector2Int,List<Vector2>> TreeIndex()
    {
        var index=new Dictionary<Vector2Int,List<Vector2>>();
        foreach(var g in Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None))
        {
            if(g.gameObject.scene.path!=ScenePath)continue;
            string n=g.name.ToLowerInvariant();if(!n.Contains("tree")&&!n.Contains("animate"))continue;
            var rs=g.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);if(b.size.y<6)continue;
            var p=new Vector2(b.center.x,b.center.z);var k=new Vector2Int(Mathf.FloorToInt(p.x/32),Mathf.FloorToInt(p.y/32));if(!index.TryGetValue(k,out var list))index[k]=list=new List<Vector2>();if(!list.Any(q=>Vector2.Distance(p,q)<3))list.Add(p);
        }
        // Some imported trees have no LODGroup. Use their actual canopy bounds,
        // rather than trusting an FBX pivot that may be far below the island.
        foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if(f.gameObject.scene.path!=ScenePath||!f.sharedMesh||!AssetDatabase.GetAssetPath(f.sharedMesh).Contains("Environment/Trees/"))continue;
            var r=f.GetComponent<Renderer>();if(!r||!r.enabled||r.bounds.size.y<6)continue;
            var p=new Vector2(r.bounds.center.x,r.bounds.center.z);var k=new Vector2Int(Mathf.FloorToInt(p.x/32),Mathf.FloorToInt(p.y/32));if(!index.TryGetValue(k,out var list))index[k]=list=new List<Vector2>();if(!list.Any(q=>Vector2.Distance(p,q)<5))list.Add(p);
        }return index;
    }
    static float Shelter(Vector2 p,Dictionary<Vector2Int,List<Vector2>> index)
    {
        var k=new Vector2Int(Mathf.FloorToInt(p.x/32),Mathf.FloorToInt(p.y/32));float min=60;
        for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)if(index.TryGetValue(k+new Vector2Int(x,y),out var list))foreach(var q in list)min=Mathf.Min(min,Vector2.Distance(p,q));
        return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(7,31,min));
    }
    static Vector2[] Rocks()=>Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.sharedMesh&&f.gameObject.scene.path.StartsWith("Assets/Scenes/First Island/")&&(f.name.ToLowerInvariant().Contains("rock")||f.sharedMesh.name.ToLowerInvariant().Contains("rock"))).Select(f=>{var r=f.GetComponent<Renderer>();return r?new Vector2(r.bounds.center.x,r.bounds.center.z):new Vector2(f.transform.position.x,f.transform.position.z);}).Distinct().ToArray();
    static float RockShelter(Vector2 p,Vector2[] rocks){float min=30;foreach(var rock in rocks)min=Mathf.Min(min,Vector2.Distance(p,rock));return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,9,min));}
    public static object ProtectGameplay()
    {
        var settings=Settings();var clearing=new List<IslandEcologySettings.Clearing>(settings.clearings);
        void Add(string reason,Vector3 point,float radius){if(!clearing.Any(c=>c.reason==reason))clearing.Add(new IslandEcologySettings.Clearing{reason=reason,center=new Vector2(point.x,point.z),radius=radius});}
        var cap=Object.FindFirstObjectByType<CappuccinoAI>(FindObjectsInactive.Include);var island=Ground().GetComponent<ProceduralIsland>();
        if(cap)
        {
            var dir=Vector3.ProjectOnPlane(island.transform.position-cap.transform.position,Vector3.up).normalized;
            for(float d=70;d<=150;d+=12){var at=cap.transform.position+dir*d;if(Ground().Raycast(new Ray(at+Vector3.up*180,Vector3.down),out var h,400)&&h.point.y>island.beachHeight+3){Add("Chicken flock runtime arena (derived from PlaceInland)",h.point,45);break;}}
        }
        var arrival=Object.FindFirstObjectByType<FirstIslandArrival>();
        if(arrival){if(arrival.shore)Add("Arrival recovery",arrival.shore.position,10);if(arrival.lostStickPoint)Add("Story stick pickup",arrival.lostStickPoint.position,10);}
        foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))if(c.isTrigger&&c.gameObject.scene.path.StartsWith("Assets/Scenes/First Island/")&&c.bounds.size.x<50&&c.bounds.size.z<50)Add("Trigger: "+c.name,c.bounds.center,Mathf.Max(c.bounds.extents.x,c.bounds.extents.z)+3);
        settings.clearings=clearing.ToArray();EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();Json("protected-gameplay",new{clearings=settings.clearings.Select(c=>new{c.reason,x=c.center.x,z=c.center.y,c.radius}).ToArray(),corridors=settings.corridors.Select(c=>new{c.reason,c.halfWidth,points=c.points.Select(p=>new[]{p.x,p.y}).ToArray()}).ToArray()});return new{clearings=clearing.Count};
    }
    public static object PlantSample()=>Plant(false);
    public static object Prefabs()
    {
        var data=AssetDatabase.LoadAssetAtPath<IslandEcologyData>(Folder+"/Ecology.asset");
        for(int i=0;i<data.species.Length;i++)
        {
            var s=data.species[i];var root=new GameObject(s.name);root.hideFlags=HideFlags.HideAndDontSave;
            try
            {
                var renderers=new Renderer[2];
                for(int j=0;j<2;j++){var go=new GameObject("LOD"+j,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);go.GetComponent<MeshFilter>().sharedMesh=j==0?s.near:s.far;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=s.material;renderers[j]=r;}
                var group=root.AddComponent<LODGroup>();group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=true;
                group.SetLODs(new[]{new LOD(.13f,new[]{renderers[0]}),new LOD(.015f,new[]{renderers[1]})});group.RecalculateBounds();
                PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+i+"-"+s.name+".prefab");
            }finally{Object.DestroyImmediate(root);}
        }return new{prefabs=data.species.Length,colliders=0};
    }
    public static object DressSample()=>DressGrass(false);
    public static object DressIsland()=>DressGrass(true);
    public static object Validate(string label="sample")
    {
        var data=AssetDatabase.LoadAssetAtPath<IslandEcologyData>(Folder+"/Ecology.asset");var settings=Settings();var ground=Ground();
        int invalidGeometry=0,badNormals=0,uninstanced=0,missing=0,protectedRoots=0,outsideBounds=0,floatingRoots=0,roots=0,instances=0;
        float maxGap=float.MinValue,minGap=float.MaxValue;
        foreach(var s in data.species)
        {
            if(!s.material.enableInstancing)uninstanced++;
            foreach(var mesh in new[]{s.near,s.far})
            {
                foreach(var v in mesh.vertices)if(!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z))invalidGeometry++;
                foreach(var n in mesh.normals)if(!float.IsFinite(n.x)||!float.IsFinite(n.y)||!float.IsFinite(n.z))badNormals++;
            }
            var vertices=s.near.vertices;var colors=s.near.colors;
            var rootVertices=vertices.Where(v=>v.y>=-.001f&&v.y<.025f).ToArray();
            foreach(var cell in s.cells)for(int i=0;i<cell.positions.Length;i++)
            {
                var p=cell.positions[i];var n=cell.normals[i];var position=new Vector3(p.x,p.y,p.z);instances++;
                var q=Quaternion.FromToRotation(Vector3.up,new Vector3(n.x,n.y,n.z))*Quaternion.Euler(0,p.w,0);
                if(Protected(new Vector2(p.x,p.z),settings,0))protectedRoots++;
                if(!ground.Raycast(new Ray(new Vector3(p.x,180,p.z),Vector3.down),out var hit,400))missing++;
                foreach(var root in rootVertices)
                {
                    var wp=position+q*root*n.w;roots++;
                    if(!ground.Raycast(new Ray(new Vector3(wp.x,180,wp.z),Vector3.down),out var h,400)){missing++;continue;}
                    float gap=wp.y-h.point.y;maxGap=Mathf.Max(maxGap,gap);minGap=Mathf.Min(minGap,gap);if(gap>.035f)floatingRoots++;
                }
                foreach(var v in vertices)if(!cell.bounds.Contains(position+q*v*n.w))outsideBounds++;
            }
        }
        var messages=ShaderUtil.GetShaderMessages(Shader.Find("Mavis/FoliageWind")).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
        var renderer=Object.FindFirstObjectByType<IslandEcologyRenderer>();var scene=renderer.gameObject.scene;
        int colliders=scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Collider>(true).Length);
        var report=new{passed=invalidGeometry==0&&badNormals==0&&uninstanced==0&&missing==0&&protectedRoots==0&&outsideBounds==0&&floatingRoots==0&&messages.Length==0,instances,invalidGeometry,badNormals,uninstanced,missing,protectedRoots,outsideBounds,floatingRoots,roots,maxGap,minGap,colliders,newColliders=renderer.GetComponentsInChildren<Collider>(true).Length,shaderErrors=messages,ready=renderer.IsReady};Json("validation-"+label,report);return report;
    }
    public static object SaveSample()
    {
        var validation=Validate();var result=Newtonsoft.Json.Linq.JObject.FromObject(validation);if(!(bool)result["passed"])throw new Exception("Sample validation failed");
        var scene=SceneManager.GetSceneByPath(ScenePath);if(Application.isPlaying)throw new Exception("Edit Mode required");
        EditorSceneManager.SaveScene(scene);File.Copy(ScenePath,W+"/Environment.sample.unity",true);return validation;
    }
    public static object SaveIsland()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        var validation=Validate("island");if(!(bool)Newtonsoft.Json.Linq.JObject.FromObject(validation)["passed"])throw new Exception("Island validation failed");
        var scene=SceneManager.GetSceneByPath(ScenePath);
        // ProceduralSeabed enables itself on editor ticks. Keep the original disk
        // flag while saving this unrelated layer; its generator still owns runtime state.
        var seabed=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Ocean Seabed")?.GetComponent<MeshRenderer>();bool enabled=seabed&&seabed.enabled;
        try{if(seabed)seabed.enabled=false;EditorSceneManager.SaveScene(scene);}finally{if(seabed)seabed.enabled=enabled;}
        return validation;
    }
    public static object RepairBareTrees()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Prefabs/Environment/Shared/Foliage/Materials/Island_Trunk.mat");
        var material=new Material(source){name="Weathered_Bark",enableInstancing=true};material.SetFloat("_BotanicalSurface",1);material.SetFloat("_BotanicalTriplanar",.30f);material.SetFloat("_UseBladeHeight",0);
        material.SetTexture("_BotanicalNormal",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Prefabs/Environment/Trees/IslandTree01/Shared/textures/island_tree_01_nor_gl_4k.exr"));
        material.SetTexture("_BotanicalSurfaceMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/bark-surface.png"));material.SetColor("_BaseColor",new Color(.72f,.67f,.59f));material.SetFloat("_Smoothness",.22f);material.SetFloat("_WindBend",.15f);material.SetFloat("_PlayerPushStrength",0);material.SetFloat("_WindFrequency",.4f);material.SetFloat("_Cull",2);material=Asset(material,"Weathered_Bark.mat");
        var changed=new List<object>();
        foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if(f.gameObject.scene.path!=ScenePath||!f.sharedMesh||AssetDatabase.GetAssetPath(f.sharedMesh)!="Assets/Game/Prefabs/Environment/Trees/IslandTree01/Shared/20260918140331_c8541e3e.fbx")continue;
            var r=f.GetComponent<MeshRenderer>();if(!r)continue;changed.Add(new{name=r.name,id=GlobalObjectId.GetGlobalObjectIdSlow(r).ToString(),old=r.sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray(),uvs=f.sharedMesh.uv.Length});
            Undo.RecordObject(r,"Restore untextured tree bark");r.sharedMaterial=material;EditorUtility.SetDirty(r);if(PrefabUtility.IsPartOfPrefabInstance(r))PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetSceneByPath(ScenePath));Json("bark-repair",changed);return new{renderers=changed.Count,sourceMeshPreserved=true,triplanar=true};
    }
    public static object DisableEcology()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode first");
        var scene=SceneManager.GetSceneByPath(ScenePath);var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==RootName);
        if(root){Undo.RecordObject(root,"Disable ecological layer");root.SetActive(false);}
        var scatter=Object.FindFirstObjectByType<NaturalGrassScatter>();Undo.RecordObject(scatter,"Restore original long meadow");scatter.data=AssetDatabase.LoadAssetAtPath<NaturalGrassScatterData>("Assets/Generated/NaturalGrassScatter068/Scatter.asset");scatter.enabled=false;scatter.enabled=true;
        const string model="Assets/Game/Prefabs/Environment/Trees/IslandTree01/Shared/20260918140331_c8541e3e.fbx";
        var original=AssetDatabase.LoadAllAssetsAtPath(model).OfType<Material>().First();int restored=0;
        foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))if(f.gameObject.scene==scene&&f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh)==model)
        {var r=f.GetComponent<MeshRenderer>();if(!r)continue;Undo.RecordObject(r,"Restore original bare tree material");r.sharedMaterial=original;if(PrefabUtility.IsPartOfPrefabInstance(r))PrefabUtility.RecordPrefabInstancePropertyModifications(r);restored++;}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{disabled=true,originalTreesRestored=restored,assetsRetained=true};
    }
    public static object ApproveSample()
    {
        var evidence=new[]{"sample-0.png","sample-3.png","validation-sample.json","performance-sample.json"};
        foreach(var file in evidence)if(!File.Exists(W+"/"+file))throw new Exception("Missing review evidence: "+file);
        var report=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(W+"/validation-sample.json"));if(!(bool)report["passed"])throw new Exception("Sample validation failed");
        Json("sample-approved",new{review="Bounded sample viewed in native Unity captures; shader, grounding, clearance and performance checked",utc=DateTime.UtcNow});return "Approved for same-rule expansion";
    }
    static object DressGrass(bool whole)
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        var settings=Settings();var scatter=Object.FindFirstObjectByType<NaturalGrassScatter>();
        const string original="Assets/Generated/NaturalGrassScatter068/Scatter.asset";
        var source=AssetDatabase.LoadAssetAtPath<NaturalGrassScatterData>(original);if(!source)throw new Exception("Original 068 meadow not found");
        var data=Object.Instantiate(source);data.name="Ecological long grass";
        var treeIndex=TreeIndex();int before=source.cells.Sum(c=>c.positions.Length),removed=0;
        // Patch openings use the same fixed ecological field as new botany; grass stays tall.
        data.cells=source.cells.Select(cell=>
        {
            var positions=new List<Vector4>();var normals=new List<Vector3>();
            for(int i=0;i<cell.positions.Length;i++)
            {
                var p=cell.positions[i];var point=new Vector2(p.x,p.z);bool modify=whole||Vector2.Distance(point,settings.sampleCenter)<settings.sampleRadius;
                float shade=Shelter(point,treeIndex),patch=Mathf.PerlinNoise(p.x*.018f+12.1f,p.z*.018f+61.7f);
                float retained=Mathf.Lerp(settings.meadowGrassRetention,settings.woodlandGrassRetention,shade);
                retained*=Mathf.Lerp(.3f,1,Mathf.SmoothStep(.2f,.72f,patch));
                float corridor=CorridorDistance(point,settings);if(corridor<2)retained*=Mathf.SmoothStep(.06f,1,Mathf.InverseLerp(-3,2,corridor));
                float hash=Mathf.Repeat(Mathf.Sin(p.x*12.9898f+p.z*78.233f)*43758.5453f,1);
                if(modify&&hash>retained){removed++;continue;}positions.Add(p);normals.Add(cell.normals[i]);
            }
            return new NaturalGrassScatterData.Cell{positions=positions.ToArray(),normals=normals.ToArray(),bounds=cell.bounds};
        }).ToArray();
        Mesh Subset(Mesh sourceMesh,int stride,string name)
        {
            var m=new Mesh{name=name};var v=sourceMesh.vertices;var n=sourceMesh.normals;var c=sourceMesh.colors32;
            var vs=new List<Vector3>();var ns=new List<Vector3>();var cs=new List<Color32>();var ts=new List<int>();
            for(int b=0;b<v.Length/7;b+=stride){int start=vs.Count;for(int j=0;j<7;j++){vs.Add(v[b*7+j]);ns.Add(n[b*7+j]);cs.Add(c[b*7+j]);}
                ts.AddRange(new[]{start,start+2,start+1,start+1,start+2,start+3,start+2,start+4,start+3,start+3,start+4,start+5,start+4,start+6,start+5});}
            m.SetVertices(vs);m.SetNormals(ns);m.SetColors(cs);m.SetTriangles(ts,0);m.bounds=sourceMesh.bounds;return m;
        }
        data.mediumMeshes=source.meshes.Select((m,i)=>Asset(Subset(m,3,"Tall meadow medium "+i),"grass-medium-"+i+".asset")).ToArray();
        data.farMeshes=source.meshes.Select((m,i)=>Asset(Subset(m,9,"Tall meadow far "+i),"grass-far-"+i+".asset")).ToArray();
        data=Asset(data,"LongGrass.asset");Undo.RecordObject(scatter,"Apply ecological grass openings");scatter.data=data;scatter.enabled=false;scatter.enabled=true;EditorUtility.SetDirty(scatter);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scatter.gameObject.scene);
        var report=new{whole,before,after=before-removed,removed,heightUnchanged=true,sourcePreserved=original,nearTriangles=source.meshes[0].triangles.Length/3,mediumTriangles=data.mediumMeshes[0].triangles.Length/3,farTriangles=data.farMeshes[0].triangles.Length/3};Json(whole?"grass-island":"grass-sample",report);return report;
    }
    public static object Expand()
    {
        if(!File.Exists(W+"/sample-approved.json"))throw new Exception("Validate and visually inspect the bounded sample before expansion");return Plant(true);
    }
    public static object CheckRepeatability()
    {
        string Hash()
        {
            var d=AssetDatabase.LoadAssetAtPath<IslandEcologyData>(Folder+"/Ecology.asset");
            using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream))
            {
                foreach(var s in d.species)foreach(var c in s.cells)for(int i=0;i<c.positions.Length;i++)
                {var p=c.positions[i];var n=c.normals[i];foreach(float f in new[]{p.x,p.y,p.z,p.w,n.x,n.y,n.z,n.w})writer.Write(f);}
                writer.Flush();using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream.ToArray()));
            }
        }
        string before=Hash();Plant(true);string after=Hash();var result=new{passed=before==after,before,after};Json("repeatability",result);return result;
    }
    static object Plant(bool whole)
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        var ground=Ground();var settings=Settings();var data=AssetDatabase.LoadAssetAtPath<IslandEcologyData>(Folder+"/Ecology.asset");if(!data)throw new Exception("Build botanical library first");
        var trees=TreeIndex();var rocks=Rocks();var terrain=ground.GetComponent<MeshRenderer>().sharedMaterial;
        var rootSamples=data.species.Select(s=>s.near.vertices.Where(v=>v.y>=-.001f&&v.y<.025f).ToArray()).ToArray();
        float sand=terrain.GetFloat("_SeaLevel")+terrain.GetFloat("_BeachHeight")+settings.shorelineMargin;
        var rng=new System.Random(settings.seed);var cells=new Dictionary<Vector2Int,List<(Vector4 p,Vector4 n)>>[data.species.Length];for(int i=0;i<cells.Length;i++)cells[i]=new Dictionary<Vector2Int,List<(Vector4,Vector4)>>();
        var obstacles=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c!=ground&&c.enabled&&!c.isTrigger&&!(c is CharacterController)&&!c.GetComponentInParent<ProceduralIsland>()&&c.bounds.size.sqrMagnitude>1).ToArray();
        int accepted=0,rejectedSlope=0,rejectedProtection=0,rejectedObstacles=0;var points=new List<Vector2>();var b=ground.bounds;
        for(float z=b.min.z;z<b.max.z;z+=settings.clusterSpacing)for(float x=b.min.x;x<b.max.x;x+=settings.clusterSpacing)
        {
            var center=new Vector2(x+R(rng,0,settings.clusterSpacing),z+R(rng,0,settings.clusterSpacing));
            float patch=Mathf.PerlinNoise(center.x*.018f+12.1f,center.y*.018f+61.7f);
            // Consume randomness independently of the sample crop, so expansion agrees.
            int members=rng.Next(11,26);float clusterDraw=R(rng);bool eligible=clusterDraw<settings.clusterDensity*Mathf.SmoothStep(.08f,1,patch);
            // Roughly 40% of island clusters qualify as sheltered, inland pockets.
            // Compensate for that habitat gate so the editable target describes
            // the whole new layer, rather than only the eligible pockets.
            int family=rng.Next(0,4);bool weird=R(rng)<Mathf.Clamp01(settings.uncannyFraction/.4f);
            for(int member=0;member<members;member++)
            {
                float angle=R(rng,0,6.283f),radius=Mathf.Sqrt(R(rng))*R(rng,3,7);var p=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                float pick=R(rng),scale=R(rng,.72f,1.35f),yaw=R(rng,0,360);
                if(!eligible||(!whole&&Vector2.Distance(p,settings.sampleCenter)>settings.sampleRadius))continue;
                if(!ground.Raycast(new Ray(new Vector3(p.x,180,p.y),Vector3.down),out var hit,400)||hit.point.y<sand)continue;
                if(hit.normal.y<settings.minimumGroundNormalY){rejectedSlope++;continue;}
                float shade=Shelter(p,trees),rockShade=RockShelter(p,rocks);bool coastal=hit.point.y<sand+7;float moisture=Mathf.PerlinNoise(p.x*.026f+71,p.y*.026f+23);
                int id;
                if(weird&&!coastal&&(shade>.2f||moisture>.55f)&&CorridorDistance(p,settings)>12)id=16+Mathf.Min(3,(int)(pick*4));
                else if(coastal)id=pick<.48f?0:pick<.78f?1:5;
                else if(rockShade>.2f)id=pick<.30f?13:pick<.60f?3:pick<.78f?4:pick<.90f?12:15;
                else if(shade>.35f)id=pick<.25f?4:pick<.40f?3:pick<.53f?13:pick<.65f?14:pick<.75f?12:pick<.83f?15:pick<.94f?6:7;
                else if(moisture>.60f)id=pick<.19f?2:pick<.38f?4:pick<.55f?10:pick<.66f?6:pick<.75f?13:pick<.84f?14:pick<.90f?12:pick<.95f?15:8;
                else id=pick<.22f?1:pick<.43f?9:pick<.59f?10:pick<.72f?11:pick<.84f?5:pick<.94f?2:family%2==0?7:8;
                bool tree=id==7||id==8;float footprint=tree?3.7f:id==5||id==6?1.7f:id==4||id==16?2.5f:1.3f;
                if(Protected(p,settings,footprint*scale)){rejectedProtection++;continue;}
                // Thin saplings and shrubs independently, leaving clear negative space.
                if((tree&&member%7!=0)||((id==5||id==6)&&member%3!=0))continue;
                if(obstacles.Any(c=>{var delta=c.bounds.ClosestPoint(hit.point+Vector3.up)- (hit.point+Vector3.up);delta.y=0;return delta.sqrMagnitude<footprint*footprint*scale*scale&&c.bounds.max.y>hit.point.y+.15f&&c.bounds.min.y<hit.point.y+5;})){rejectedObstacles++;continue;}
                if(tree&&points.Any(q=>Vector2.Distance(p,q)<9))continue;
                // Check the complete footprint, not only a floating centre on a slope break.
                bool support=true;float minGap=0;
                var normal=Vector3.Slerp(Vector3.up,hit.normal,id==7||id==8?.15f:1);var rotation=Quaternion.FromToRotation(Vector3.up,normal)*Quaternion.Euler(0,yaw,0);
                foreach(var offset in new[]{new Vector3(-footprint,0,0),new Vector3(footprint,0,0),new Vector3(0,0,-footprint),new Vector3(0,0,footprint)})
                {
                    var wp=hit.point+rotation*offset*scale;
                    if(!ground.Raycast(new Ray(new Vector3(wp.x,180,wp.z),Vector3.down),out var h,400)||h.point.y<sand-.3f){support=false;break;}
                    float gap=wp.y-h.point.y;if(Mathf.Abs(gap)>(id>=13&&id<=15?.18f:.65f)){support=false;break;}minGap=Mathf.Max(minGap,gap);
                }
                if(!support)continue;
                var pos=hit.point-Vector3.up*.025f;float rootGap=0;
                foreach(var root in rootSamples[id])
                {
                    var wp=pos+rotation*root*scale;
                    if(!ground.Raycast(new Ray(new Vector3(wp.x,180,wp.z),Vector3.down),out var h,400)){support=false;break;}
                    rootGap=Mathf.Max(rootGap,wp.y-h.point.y);
                }
                if(!support||rootGap>.30f)continue;pos-=Vector3.up*rootGap;
                var key=new Vector2Int(Mathf.FloorToInt(p.x/24),Mathf.FloorToInt(p.y/24));if(!cells[id].TryGetValue(key,out var list))cells[id][key]=list=new List<(Vector4,Vector4)>();
                list.Add((new Vector4(pos.x,pos.y,pos.z,yaw),new Vector4(normal.x,normal.y,normal.z,scale)));accepted++;if(tree)points.Add(p);
            }
        }
        for(int id=0;id<data.species.Length;id++)
        {
            var s=data.species[id];s.cells=cells[id].OrderBy(k=>k.Key.x).ThenBy(k=>k.Key.y).Select(k=>
            {
                Bounds bounds=default;bool first=true;
                foreach(var item in k.Value)
                {
                    var p=item.p;var n=item.n;var matrix=Matrix4x4.TRS(new Vector3(p.x,p.y,p.z),Quaternion.FromToRotation(Vector3.up,new Vector3(n.x,n.y,n.z))*Quaternion.Euler(0,p.w,0),Vector3.one*n.w);
                    var mb=s.near.bounds;mb.Encapsulate(s.far.bounds);var e=mb.extents;var ex=matrix.MultiplyVector(new Vector3(e.x,0,0));var ey=matrix.MultiplyVector(new Vector3(0,e.y,0));var ez=matrix.MultiplyVector(new Vector3(0,0,e.z));
                    var extent=new Vector3(Mathf.Abs(ex.x)+Mathf.Abs(ey.x)+Mathf.Abs(ez.x),Mathf.Abs(ex.y)+Mathf.Abs(ey.y)+Mathf.Abs(ez.y),Mathf.Abs(ex.z)+Mathf.Abs(ey.z)+Mathf.Abs(ez.z));
                    var bb=new Bounds(matrix.MultiplyPoint3x4(mb.center),extent*2);if(first){bounds=bb;first=false;}else bounds.Encapsulate(bb);
                }
                return new IslandEcologyData.Cell{positions=k.Value.Select(v=>v.p).ToArray(),normals=k.Value.Select(v=>v.n).ToArray(),bounds=bounds};
            }).ToArray();
        }
        data.seed=settings.seed;data.scope=whole?"Whole Island 1, approved coastal woodland rules":"Bounded sample (251,469), radius 68m";
        EditorUtility.SetDirty(data);var renderer=Renderer();renderer.data=data;renderer.Rebuild();EditorUtility.SetDirty(renderer);
        var report=new{whole,accepted,rejectedSlope,rejectedProtection,rejectedObstacles,treeIndex=trees.Sum(k=>k.Value.Count),species=data.species.Select(s=>new{s.name,s.uncanny,count=s.cells.Sum(c=>c.positions.Length)}).ToArray()};
        Json(whole?"placement-island":"placement-sample",report);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);return report;
    }
}
