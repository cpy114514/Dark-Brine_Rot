using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Authors the drifting course's rounded, powdery visual assets.</summary>
public static class ImproveFloatingMarshmallows
{
    const string Root="Assets/Game/Prefabs/Environment/PlainMarshmallows";
    const string Art=Root+"/Floating Marshmallow Art";
    const string Course=Root+"/Marshmallow Hop Challenge.prefab";
    const string MeshPath=Art+"/Pillowy Marshmallow.asset";
    const string TexturePath=Art+"/Fine Sugar Surface.png";

    [MenuItem("Tools/Second Island/Improve floating marshmallow visuals")]
    public static void Build()
    {
        Directory.CreateDirectory(Art);
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if(!mesh){mesh=BuildMesh();AssetDatabase.CreateAsset(mesh,MeshPath);}
        else
        {
            var updated=BuildMesh();EditorUtility.CopySerialized(updated,mesh);UnityEngine.Object.DestroyImmediate(updated);EditorUtility.SetDirty(mesh);
        }
        var surface=MakeSugarTexture();File.WriteAllBytes(TexturePath,surface.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(surface);AssetDatabase.ImportAsset(TexturePath);
        var importer=(TextureImporter)AssetImporter.GetAtPath(TexturePath);
        importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;
        importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        var prefab=PrefabUtility.LoadPrefabContents(Course);
        try
        {
            int count=0;
            foreach(Transform row in prefab.transform)
            {
                if(!row.name.StartsWith("Row ",StringComparison.Ordinal))continue;
                foreach(Transform platform in row)
                {
                    var filter=platform.GetComponent<MeshFilter>();var renderer=platform.GetComponent<MeshRenderer>();
                    if(!filter || !renderer || !renderer.sharedMaterial)throw new InvalidOperationException("Incomplete course platform: "+platform.name);
                    var original=renderer.sharedMaterial;
                    string label=original.name.Replace(" Floating","");
                    string path=Art+"/"+label+" Floating.mat";
                    var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(!material)
                    {
                        material=new Material(original){name=label+" Floating"};
                        material.SetTexture("_BaseMap",texture);material.SetTexture("_MainTex",texture);
                        material.SetFloat("_Smoothness",.045f);
                        AssetDatabase.CreateAsset(material,path);
                    }
                    filter.sharedMesh=mesh;renderer.sharedMaterial=material;count++;
                }
            }
            if(count!=33)throw new InvalidOperationException("Expected 33 floating marshmallows, found "+count);
            PrefabUtility.SaveAsPrefabAsset(prefab,Course);
            Debug.Log("Updated 33 floating marshmallow visuals; movement and colliders were preserved.");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }

    static Mesh BuildMesh()
    {
        const int sides=64, bevel=7;
        // Match the previous mesh footprint and its existing side collider.
        const float radius=.65f, edgeX=.065f, edgeY=.21f, height=1.36f;
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        void Ring(float y,float r,float normalR,float normalY)
        {
            for(int i=0;i<=sides;i++)
            {
                float angle=2f*Mathf.PI*i/sides,cos=Mathf.Cos(angle),sin=Mathf.Sin(angle);
                float irregularity=1f+.006f*Mathf.Sin(3f*angle+.4f)+.003f*Mathf.Cos(7f*angle);
                vertices.Add(new Vector3(r*irregularity*cos,y,r*irregularity*sin));
                normals.Add(new Vector3(normalR*cos,normalY,normalR*sin).normalized);
                uv.Add(new Vector2(i/(float)sides,y/height));
            }
        }
        for(int i=0;i<=bevel;i++)
        {
            float angle=-Mathf.PI*.5f+i*Mathf.PI*.5f/bevel;
            Ring(edgeY+edgeY*Mathf.Sin(angle),radius-edgeX+edgeX*Mathf.Cos(angle),Mathf.Cos(angle),Mathf.Sin(angle));
        }
        // A slight rounded belly catches light more softly than a hard cylinder.
        Ring(height*.5f,radius+.009f,1,0);
        Ring(height-edgeY,radius,1,0);
        for(int i=1;i<=bevel;i++)
        {
            float angle=i*Mathf.PI*.5f/bevel;
            Ring(height-edgeY+edgeY*Mathf.Sin(angle),radius-edgeX+edgeX*Mathf.Cos(angle),Mathf.Cos(angle),Mathf.Sin(angle));
        }
        int ringCount=vertices.Count/(sides+1);
        for(int r=0;r<ringCount-1;r++)for(int i=0;i<sides;i++)
        {
            int a=r*(sides+1)+i,b=a+sides+1;
            triangles.Add(a);triangles.Add(b);triangles.Add(a+1);
            triangles.Add(a+1);triangles.Add(b);triangles.Add(b+1);
        }
        void Cap(float y,bool top)
        {
            int center=vertices.Count;vertices.Add(new Vector3(0,y,0));normals.Add(top?Vector3.up:Vector3.down);uv.Add(new Vector2(.5f,.5f));
            for(int i=0;i<=sides;i++)
            {
                float a=2f*Mathf.PI*i/sides,cos=Mathf.Cos(a),sin=Mathf.Sin(a);
                float r=(radius-edgeX)*(1f+.006f*Mathf.Sin(3f*a+.4f)+.003f*Mathf.Cos(7f*a));
                vertices.Add(new Vector3(r*cos,y,r*sin));normals.Add(top?Vector3.up:Vector3.down);
                uv.Add(new Vector2(.5f+cos*.49f,.5f+sin*.49f));
            }
            for(int i=0;i<sides;i++)
            {
                triangles.Add(center);triangles.Add(center+1+i+(top?1:0));triangles.Add(center+1+i+(top?0:1));
            }
        }
        Cap(0,false);Cap(height,true);
        var mesh=new Mesh{name="Pillowy Floating Marshmallow"};
        mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
    }

    static Texture2D MakeSugarTexture()
    {
        const int size=256;var tex=new Texture2D(size,size,TextureFormat.RGBA32,true,false){name="Fine Sugar Surface"};
        var pixels=new Color32[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float broad=PeriodicNoise(x,y,12),fine=PeriodicNoise(x,y,64);
            uint hash=(uint)(x*374761393+y*668265263);hash=(hash^(hash>>13))*1274126177u;
            float pore=(hash&1023)<22 ? .025f : 0f;
            float value=Mathf.Clamp01(.92f+.025f*broad+.05f*fine-pore);
            byte b=(byte)Mathf.RoundToInt(value*255);pixels[y*size+x]=new Color32(b,b,b,255);
        }
        tex.SetPixels32(pixels);tex.Apply(true,false);return tex;
    }
    static float PeriodicNoise(int x,int y,int frequency)
    {
        float px=x*frequency/256f,py=y*frequency/256f;int x0=Mathf.FloorToInt(px),y0=Mathf.FloorToInt(py);
        float u=Mathf.SmoothStep(0,1,px-x0),v=Mathf.SmoothStep(0,1,py-y0);
        float Hash(int a,int b)
        {
            uint h=(uint)((a%frequency)*374761393+(b%frequency)*668265263);h=(h^(h>>13))*1274126177u;
            return (h&65535)/65535f;
        }
        return Mathf.Lerp(Mathf.Lerp(Hash(x0,y0),Hash(x0+1,y0),u),Mathf.Lerp(Hash(x0,y0+1),Hash(x0+1,y0+1),u),v);
    }
}
