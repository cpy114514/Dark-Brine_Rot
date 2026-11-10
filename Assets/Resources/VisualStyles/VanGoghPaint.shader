Shader "Hidden/DarkBrine/VanGoghPainting"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline"}
        ZWrite Off ZTest Always Cull Off
        HLSLINCLUDE
        #pragma target 4.5
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Assets/Shaders/VanGoghPigment.hlsl"
        TEXTURE2D_X_FLOAT(_VGDepth);TEXTURE2D_X(_VGAnchors);TEXTURE2D_X(_VGTensor);
        float RawDepth(float2 uv){return SAMPLE_TEXTURE2D_X_LOD(_VGDepth,sampler_PointClamp,uv,0).r;}
        float4 Anchor(float2 uv){return SAMPLE_TEXTURE2D_X_LOD(_VGAnchors,sampler_PointClamp,uv,0);}
        float3 ColorAt(float2 uv){return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_LinearClamp,uv,0).rgb;}
        float4 Tensor(Varyings v):SV_Target
        {
            float2 uv=v.texcoord,dx=_BlitTexture_TexelSize.xy*2;
            float3 gx=ColorAt(uv+float2(dx.x,0))-ColorAt(uv-float2(dx.x,0));
            float3 gy=ColorAt(uv+float2(0,dx.y))-ColorAt(uv-float2(0,dx.y));
            return float4(dot(gx,gx),dot(gx,gy),dot(gy,gy),0);
        }
        float4 Paint(Varyings v):SV_Target
        {
            float2 uv=v.texcoord;float3 original=ColorAt(uv);float raw=RawDepth(uv);
            #if UNITY_REVERSED_Z
            if(raw<.000001)return float4(original,1);
            #else
            if(raw>.999999)return float4(original,1);
            raw=lerp(UNITY_NEAR_CLIP_VALUE,1,raw);
            #endif
            float3 world=ComputeWorldSpacePosition(uv,raw,UNITY_MATRIX_I_VP);
            float3 wx=ddx(world),wy=ddy(world),normal=normalize(cross(wy,wx)+1e-12);
            float4 anchor=Anchor(uv);bool skin=anchor.a>.5;
            float3 an=abs(normal);float2 plane=an.y>max(an.x,an.z)?world.xz:(an.x>an.z?world.zy:world.xy);
            float vegetation=saturate((original.g-max(original.r,original.b))*6);
            float size=_VGBrush.x*lerp(1,.55,vegetation);
            float2 coordinate=(skin?anchor.xy:plane)/max(.015,size);
            float eye=LinearEyeDepth(RawDepth(uv),_ZBufferParams),eyeX=ddx(eye),eyeY=ddy(eye);
            float2 cdx=ddx(coordinate),cdy=ddy(coordinate);
            float det=cdx.x*cdy.y-cdx.y*cdy.x;
            float3 tensor=0;float2 t=_BlitTexture_TexelSize.xy*3;
            tensor+=SAMPLE_TEXTURE2D_X_LOD(_VGTensor,sampler_LinearClamp,uv,0).xyz*2;
            tensor+=SAMPLE_TEXTURE2D_X_LOD(_VGTensor,sampler_LinearClamp,uv+float2(t.x,0),0).xyz;
            tensor+=SAMPLE_TEXTURE2D_X_LOD(_VGTensor,sampler_LinearClamp,uv-float2(t.x,0),0).xyz;
            tensor+=SAMPLE_TEXTURE2D_X_LOD(_VGTensor,sampler_LinearClamp,uv+float2(0,t.y),0).xyz;
            tensor+=SAMPLE_TEXTURE2D_X_LOD(_VGTensor,sampler_LinearClamp,uv-float2(0,t.y),0).xyz;
            float angle=.5*atan2(2*tensor.y,tensor.x-tensor.z+1e-8)+1.5707963;
            float2 imageDirection=float2(cos(angle),sin(angle));
            float2 surfaceDirection=cdx*imageDirection.x+cdy*imageDirection.y;
            float imageAngle=atan2(surfaceDirection.y,surfaceDirection.x);
            float stableAngle=skin?1.1:(an.y>.65?.18:1.45);
            stableAngle=lerp(stableAngle,1.5,vegetation*.7);
            float direction=stableAngle+sin(coordinate.y*.08+coordinate.x*.035)*_VGBrush.w*.4;
            float flowMix=(1-_VGControl.x)*_VGBrush.z;
            direction=atan2(lerp(sin(direction),sin(imageAngle),flowMix),lerp(cos(direction),cos(imageAngle),flowMix));
            float3 painted=VGPalette(original,.5);float coverage=0;
            int layers=_VGControl.w<.5?1:(_VGControl.w>1.5?3:2);
            [loop]for(int layer=0;layer<layers;layer++)
            {
                float scale=layer==0?1:(layer==1?1.93:3.41);float2 p=coordinate*scale;
                float2 cell=floor(p);float sum=0,height=0;float3 pigment=0;
                float footprint=max(length(cdx),length(cdy))*scale;
                [unroll]for(int y=-1;y<=1;y++)[unroll]for(int x=-1;x<=1;x++)
                {
                    float2 id=cell+float2(x,y);float seed=VGHash(id+layer*73+(skin?anchor.b*.17:0));
                    float2 center=id+.5+float2(seed-.5,VGHash(id+31)-.5)*.5;
                    float2 delta=center/scale-coordinate;
                    float inv=rcp(abs(det)>1e-9?det:1e-9);
                    float2 offset=float2(delta.x*cdy.y-delta.y*cdy.x,cdx.x*delta.y-cdx.y*delta.x)*inv;
                    float2 sampleUv=uv+offset*_BlitTexture_TexelSize.xy;
                    float4 other=Anchor(sampleUv);float depth=LinearEyeDepth(RawDepth(sampleUv),_ZBufferParams);
                    float expected=eye+eyeX*offset.x+eyeY*offset.y;
                    float valid=step(abs(depth-expected),max(.04,eye*.0015))*step(length(offset),64);
                    valid*=skin?step(abs(other.b-anchor.b),.1)*other.a:1-other.a;
                    valid*=step(0,sampleUv.x)*step(sampleUv.x,1)*step(0,sampleUv.y)*step(sampleUv.y,1);
                    float relief;float w=VGMark(p-center,seed,direction+(seed-.5)*(1-_VGBrush.z)*1.3,footprint,relief)*valid*step(1-_VGBrush.y,VGHash(id+135));
                    float3 c=VGPalette(ColorAt(sampleUv),seed);
                    c*=1+(seed-.5)*.16+relief*_VGPigment.z*.2;
                    pigment+=c*w;sum+=w;height+=relief*w;
                }
                float visible=(1-smoothstep(.6,1.8,footprint))*saturate(sum*2)*_VGBrush.y;
                float strength=(layer==0?.78:.42)*visible;
                painted=lerp(painted,pigment/max(.0001,sum),strength);coverage+=visible;
            }
            // Second difference rejects a continuous sloping plane. A depth
            // gradient alone would darken the whole beach as if it were an edge.
            float2 texel=_BlitTexture_TexelSize.xy;
            float left=LinearEyeDepth(RawDepth(uv-float2(texel.x,0)),_ZBufferParams),right=LinearEyeDepth(RawDepth(uv+float2(texel.x,0)),_ZBufferParams);
            float up=LinearEyeDepth(RawDepth(uv-float2(0,texel.y)),_ZBufferParams),down=LinearEyeDepth(RawDepth(uv+float2(0,texel.y)),_ZBufferParams);
            float edge=saturate(max(abs(left+right-2*eye),abs(up+down-2*eye))/max(.03,eye*.003));
            painted*=1-edge*_VGPigment.w*.35;
            return float4(lerp(original,painted,_VGControl.y),1);
        }
        ENDHLSL
        Pass { Name "Directional pigment" HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Paint
            ENDHLSL }
        Pass { Name "Structure tensor" HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Tensor
            ENDHLSL }
    }
}
