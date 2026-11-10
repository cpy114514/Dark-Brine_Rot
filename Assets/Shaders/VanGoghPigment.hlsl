#ifndef DARK_BRINE_VAN_GOGH_PIGMENT
#define DARK_BRINE_VAN_GOGH_PIGMENT
float4 _VGBrush; // physical size, density, directional strength, curvature
float4 _VGPigment; // abstraction, contrast, relief, contour
float4 _VGControl; // stable surface preference, intensity, colour expression, quality
float VGHash(float2 p){float3 q=frac(float3(p.xyx)*.1031);q+=dot(q,q.yzx+33.33);return frac((q.x+q.y)*q.z);}
float VGLuma(float3 c){return dot(c,float3(.2126,.7152,.0722));}
float2 VGRotate(float2 p,float angle){float s,c;sincos(angle,s,c);return float2(c*p.x-s*p.y,s*p.x+c*p.y);}
float3 VGPalette(float3 c,float pigment)
{
    float l=VGLuma(c),light=saturate(l);
    float bands=lerp(15,7,_VGPigment.x);
    float band=l*bands;
    float painted=(floor(band)+smoothstep(.23,.77,frac(band)))/bands;
    c*=lerp(1,painted/max(.045,l),_VGPigment.x*.38);
    float green=saturate((c.g-max(c.r,c.b)) * 3.5);
    float3 shadow=float3(.075,.015,.12)*(1-light)*(1-light);
    float3 warm=float3(.11,.065,-.055)*light;
    float3 shift=lerp(shadow,warm,light)+green*float3(.065,.025,-.035);
    c+=shift*_VGControl.z*.65;
    c=lerp(float3(l,l,l),c,lerp(1,1.16,_VGPigment.y)*_VGControl.z);
    // Neighbouring pigment marks break a gradient without flattening scene illumination.
    c+=float3(.048,.027,-.025)*pigment+float3(-.017,.006,.036)*(1-pigment);
    c=(c-.18)*(1+_VGPigment.y*.18)+.18;
    return max(c,0);
}
float VGMark(float2 delta,float seed,float angle,float footprint,out float relief)
{
    float2 q=VGRotate(delta,angle);
    float length=lerp(.7,1.45,seed),width=lerp(.25,.47,VGHash(float2(seed*71,9)));
    q.y+=_VGBrush.w*(q.x*q.x-.2)*.28;
    float edge=abs(q.y)/width+pow(abs(q.x)/length,5);
    float ragged=sin(q.x*18+seed*31)*.05+sin(q.x*37-seed*13)*.025;
    float aa=max(.035,footprint*1.4);
    float mask=1-smoothstep(1-aa,1+aa,edge+ragged);
    float bristle=sin(q.y*78+sin(q.x*7+seed*10)*.7);
    bristle*=1-smoothstep(.025,.11,footprint);
    relief=(bristle*.2+sin(q.y*13+seed*23)*.6)*(1-saturate(edge))+.22*(1-smoothstep(.5,1,edge));
    return mask;
}
// Surface variants retain the original 3D light/wave/cloud solution; they alter pigment,
// not geometry. Marks are continuous in physical wave coordinates / celestial direction.
float3 VGSurface(float3 source,float2 coordinate,float angle,float category)
{
    float2 cell=floor(coordinate);float footprint=max(length(ddx(coordinate)),length(ddy(coordinate)));
    float sum=0,pigment=0,height=0;
    [unroll]for(int y=-1;y<=1;y++)[unroll]for(int x=-1;x<=1;x++)
    {
        float2 id=cell+float2(x,y);float seed=VGHash(id+category*17);
        float2 center=id+.5+float2(seed-.5,VGHash(id+13)-.5)*.52;
        float relief;float w=VGMark(coordinate-center,seed,angle+(seed-.5)*.38,footprint,relief)*step(1-_VGBrush.y,VGHash(id+135));
        sum+=w;pigment+=w*seed;height+=w*relief;
    }
    pigment=sum>.001?pigment/sum:.5;height/=max(.001,sum);
    float visibility=1-smoothstep(.6,2,footprint);
    float3 painted=VGPalette(source,pigment);
    painted*=1+((pigment-.5)*.26+height*_VGPigment.z*.32)*visibility*_VGBrush.y;
    return lerp(source,painted,_VGControl.y);
}
#endif
