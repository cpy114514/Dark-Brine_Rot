#ifndef DARK_BRINE_PIXEL_STYLE_UI
#define DARK_BRINE_PIXEL_STYLE_UI
float _PixelStyleUI, _PixelStyleTargetHeight, _PixelStyleMinScale;
float4 _PixelStyleDisplay;
float PixelUIEnabled(float4 textureSampleAdd)
{
    // Alpha-only dynamic font atlases are also exempt while a newly-created canvas registers.
    return step(.5, _PixelStyleUI) * (1-step(.5,max(textureSampleAdd.r,max(textureSampleAdd.g,textureSampleAdd.b))));
}
float2 PixelUIGrid()
{
    float height=max(1,_PixelStyleDisplay.y);
    float scale=height<=_PixelStyleTargetHeight?1:clamp((_PixelStyleTargetHeight+.001)/height,_PixelStyleMinScale,1);
    return max(1,floor(_PixelStyleDisplay.xy*scale));
}
float4 PixelUIVertex(float4 position, float2 uv, float enabled)
{
    if(enabled<.5)return position;
    float2 grid=PixelUIGrid();float2 unit=position.xy/position.w*.5+.5;
    // Cover at least one cell for fine rules/handles instead of losing sub-cell rectangles.
    float2 snapped=lerp(floor(unit*grid),ceil(unit*grid),step(.5,uv))/grid;
    position.xy=(snapped*2-1)*position.w;return position;
}
float2 PixelUIDelta(float4 fragmentPosition,float enabled)
{
    if(enabled<.5)return 0;
    float2 screen=fragmentPosition.xy/fragmentPosition.w;
    float2 delta=(floor(screen*PixelUIGrid())+.5)/PixelUIGrid()-screen;
    float2 derivative=float2(ddx(screen.x),ddy(screen.y));
    derivative=(step(0,derivative)*2-1)*max(abs(derivative),1e-7);
    return delta/derivative;
}
float2 PixelUIUV(float2 uv,float2 delta) { return uv+ddx(uv)*delta.x+ddy(uv)*delta.y; }
float4 PixelUIColor(float4 color,float2 delta) { return color+ddx(color)*delta.x+ddy(color)*delta.y; }
#endif
