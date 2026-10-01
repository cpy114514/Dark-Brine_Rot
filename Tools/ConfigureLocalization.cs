using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
public static class ConfigureLocalization
{
    public static Task<object> Run()
    {
        const string path="Assets/Game/Prefabs/UI/SettingsMenu/PauseSettingsMenu.prefab";
        AssetDatabase.Refresh();
        const string fontPath="Assets/Resources/Localization/NotoSansCJKsc SDF.asset";
        var existingFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if(!existingFont || existingFont.atlasWidth!=2048 || existingFont.characterTable.Count<100)
        {
            if(existingFont) AssetDatabase.DeleteAsset(fontPath);
            var font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Localization/NotoSansCJKsc-Regular.otf"),48,5,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
            font.name="NotoSansCJKsc SDF"; font.atlasPopulationMode=AtlasPopulationMode.Dynamic; font.isMultiAtlasTexturesEnabled=true;
            var catalog=(System.Collections.Generic.Dictionary<string,string[]>)typeof(Mavis.GameLocalization).GetField("table",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
            string characters=new string(string.Concat(catalog.Values.SelectMany(v=>v)).Concat(Enumerable.Range(32,95).Select(i=>(char)i)).Distinct().ToArray());
            if(!font.TryAddCharacters(characters,out string missing)) throw new Exception("Font bake missing characters: "+missing);
            font.material.SetTexture(ShaderUtilities.ID_MainTex,font.atlasTextures[0]);
            AssetDatabase.CreateAsset(font,fontPath);
            AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var texture in font.atlasTextures) if(texture) AssetDatabase.AddObjectToAsset(texture,font);
            EditorUtility.SetDirty(font);
        }
        // Remove only the temporary fallback installed by the earlier test; do not
        // disturb any other fallback fonts supplied by the project.
        var liberation=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if(liberation) liberation.fallbackFontAssetTable.RemoveAll(f=>!f || f.name=="Noto Sans Chinese (Runtime)");
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var menu=root.GetComponent<PauseSettingsMenu>();
            var oldLanguage=menu.options.FirstOrDefault(o=>o.key=="language");
            if(oldLanguage!=null && oldLanguage.button.transform.parent.parent.name.Contains("CHALLENGE"))
            {
                var wrong=oldLanguage.button.transform.parent;
                foreach(RectTransform sibling in wrong.parent) if(sibling!=wrong) sibling.anchoredPosition+=new Vector2(0,56);
                menu.options.Remove(oldLanguage); UnityEngine.Object.DestroyImmediate(wrong.gameObject);
            }
            if (!menu.options.Any(o=>o.key=="language"))
            {
                var difficulty=menu.options.First(o=>o.key=="difficulty");
                var row=difficulty.button.transform.parent.parent;
                var clone=UnityEngine.Object.Instantiate(row.gameObject,row.parent);
                clone.name="language row"; clone.transform.SetSiblingIndex(0);
                var rect=clone.GetComponent<RectTransform>();
                foreach(RectTransform other in row.parent) if(other!=rect) other.anchoredPosition-=new Vector2(0,76);
                var button=clone.GetComponentInChildren<Button>();
                var value=button.GetComponentInChildren<TextMeshProUGUI>(); value.text="English";
                var labels=clone.GetComponentsInChildren<TextMeshProUGUI>().Where(t=>t!=value).ToArray();
                foreach(var label in labels) label.text=label.fontSize>15 ? "LANGUAGE" : "Change language immediately; choice is saved";
                menu.options.Insert(0,new PauseSettingsMenu.OptionControl {key="language",button=button,value=value});
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        return Task.FromResult<object>(new {success=true,fontImported=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Localization/NotoSansCJKsc-Regular.otf")!=null});
    }
}
