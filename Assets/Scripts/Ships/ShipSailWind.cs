using UnityEngine;

/// <summary>Four GPU cloth panels sharing the ocean wind; no cloth/mesh rebuild per frame.</summary>
[DisallowMultipleComponent,DefaultExecutionOrder(320)]
public sealed class ShipSailWind:MonoBehaviour
{
    static readonly int WindId=Shader.PropertyToID("_WindLocal"),TimeId=Shader.PropertyToID("_SailTime");
    MaterialPropertyBlock properties;
    Renderer[] panels;
    OceanWorld ocean;
    void Start()
    {
        properties=new MaterialPropertyBlock();
        var set=Resources.Load<ShipSailSet>("Sailing/v039/ShipSails");
        if(set==null || set.panels==null){enabled=false;return;}
        ocean=FindFirstObjectByType<OceanWorld>();
        var children=GetComponentsInChildren<MeshFilter>(true);
        panels=new Renderer[set.panels.Length];
        for(int i=0;i<set.panels.Length;i++)
        {
            var panel=set.panels[i];var filter=System.Array.Find(children,f=>f.name==panel.node);
            if(filter==null)continue;
            filter.sharedMesh=panel.mesh;var renderer=filter.GetComponent<Renderer>();
            renderer.sharedMaterial=panel.material;panels[i]=renderer;
        }
    }
    void LateUpdate()
    {
        if(panels==null)return;
        // Native property blocks are recreated after a script reload.
        if(properties==null)properties=new MaterialPropertyBlock();
        Vector2 direction=ocean!=null ? ocean.wave1.direction.normalized : new Vector2(.82f,.57f);
        Vector3 wind=new Vector3(direction.x,0,direction.y);
        for(int i=0;i<panels.Length;i++)
        {
            var panel=panels[i];if(panel==null)continue;
            panel.GetPropertyBlock(properties);
            properties.SetVector(WindId,panel.transform.InverseTransformVector(wind));
            properties.SetFloat(TimeId,Time.time);
            panel.SetPropertyBlock(properties);
        }
    }
}
