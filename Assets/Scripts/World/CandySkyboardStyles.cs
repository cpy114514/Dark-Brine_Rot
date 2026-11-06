using UnityEngine;

/// <summary>Five colourful, age-independent patterns for the fairy's reward.</summary>
public static class CandySkyboardStyles
{
    public static readonly string[] Names = { "Strawberry Dream", "Ocean Breeze", "Starry Night", "Citrus Pop", "Rainbow Parade", "Original Look" };
    public static readonly Color[] Colors = {
        new Color(1f,.53f,.70f), new Color(.22f,.75f,.85f), new Color(.20f,.22f,.48f),
        new Color(1f,.75f,.25f), new Color(.72f,.54f,.88f), Color.white
    };
    public static Texture2D CreateTexture(int style)
    {
        style=Mathf.Clamp(style,0,Names.Length-1);
        const int size=128;
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,true){name=Names[style]+" pattern",wrapMode=TextureWrapMode.Repeat};
        var pixels=new Color32[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=x/(float)size,v=y/(float)size;
            Color color=Colors[style];
            if(style==0)
            {
                float dx=(u*5f)%1f-.5f,dy=(v*5f)%1f-.5f;
                if(dx*dx+dy*dy<.075f)color=new Color(1f,.85f,.90f);
            }
            else if(style==1)
            {
                if(Mathf.Sin((v+Mathf.Sin(u*12f)*.035f)*Mathf.PI*10f)>.30f)color=new Color(.76f,.95f,.94f);
            }
            else if(style==2)
            {
                float dx=(u*8f)%1f-.5f,dy=(v*8f)%1f-.5f;
                if(Mathf.Abs(dx)<.045f && Mathf.Abs(dy)<.22f || Mathf.Abs(dy)<.045f && Mathf.Abs(dx)<.22f)color=new Color(1f,.89f,.61f);
            }
            else if(style==3)
            {
                if(Mathf.Repeat(u+v, .25f)<.09f)color=new Color(1f,.93f,.56f);
            }
            else
            {
                color=Color.HSVToRGB(Mathf.Repeat((u+v)*.65f,1f),.42f,.97f);
                if(Mathf.Repeat((u-v)*5f,1f)<.09f)color=Color.Lerp(color,Color.white,.65f);
            }
            pixels[y*size+x]=color;
        }
        texture.SetPixels32(pixels);texture.Apply(true,false);return texture;
    }
}
