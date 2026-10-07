using UnityEngine;
using UnityEngine.UI;

/// <summary>Preserves artwork color, authored sprite swaps and UV alignment within the neutral menu.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Image))]
public sealed class MinimalArtworkStyle : MonoBehaviour
{
    Material normal, hover;
    void Awake()
    {
        var image=GetComponent<Image>();var interaction=GetComponent<SahurDecorationHover>();
        normal=Copy(image.material==image.defaultMaterial?null:image.material);
        if(normal){image.material=normal;if(interaction)interaction.SetNormalMaterial(normal);}
        if(interaction && interaction.hoverMaterial)
        {hover=Copy(interaction.hoverMaterial);if(hover)interaction.hoverMaterial=hover;}
    }
    static Material Copy(Material source)
    {
        var shader=source&&source.HasProperty("_Saturation")?source.shader:Shader.Find("UI/Hand Drawn Contrast");
        if(!shader)return null;
        var material=source&&source.shader==shader?new Material(source):new Material(shader);
        material.name="Menu artwork";material.SetFloat("_Saturation",1);
        if(!source){material.SetFloat("_Contrast",1);material.SetFloat("_Pivot",.5f);}
        return material;
    }
    void OnDestroy(){Release(normal);Release(hover);}
    static void Release(Material material){if(!material)return;if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
}
