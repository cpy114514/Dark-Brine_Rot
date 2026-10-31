Shader "Hidden/DarkBrine/DarkFairytale"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Illustrated opaque color"
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float4 _Style, _Palette, _InkSettings;
            float _StyleMode;
            float4 _Paper, _PaperShape, _Film, _FilmOptions;
            half4 _Ink, _ShadowTint, _HighlightTint;
            TEXTURE2D_X_FLOAT(_PaperDepthTexture);
            float StyleDepth(float2 uv)
            {
                return _StyleMode > 1.5 ? SAMPLE_TEXTURE2D_X(_PaperDepthTexture, sampler_PointClamp, uv).r : SampleSceneDepth(uv);
            }
            float3 PaperSurfaceNormal(float2 uv, float rawDepth)
            {
                float3 world = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 n = cross(ddy(world), ddx(world));
                n *= rsqrt(max(dot(n,n),1e-12));
                return dot(n,GetCameraPositionWS()-world) < 0 ? -n : n;
            }
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            half4 FilmColor(half4 source, float2 uv)
            {
                float l = max(0, dot(source.rgb, float3(.2126,.7152,.0722)));
                // Shoulder and toe retain highlight texture and readable deep shadows.
                float exposed = l / (1 + .18 * l);
                // Curve operates on linear luminance: a low toe preserves dark
                // surface variation without lifting every shadow to middle gray.
                float gray = pow(max(.00001,exposed),_Film.x);
                gray /= 1 + .06 * gray;
                float black = lerp(.016,.004,_Film.w);
                gray = black + (1-black)*gray;
                gray = lerp(gray,gray*gray*(3-2*gray),_Film.w*.12);
                if (_FilmOptions.x > .001)
                {
                    float2 tap = 2 / _ScaledScreenParams.xy;
                    float bright = 0;
                    bright += max(0, dot(SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv+float2(tap.x,0)).rgb,float3(.2126,.7152,.0722))-.7);
                    bright += max(0, dot(SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv-float2(tap.x,0)).rgb,float3(.2126,.7152,.0722))-.7);
                    bright += max(0, dot(SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv+float2(0,tap.y)).rgb,float3(.2126,.7152,.0722))-.7);
                    bright += max(0, dot(SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv-float2(0,tap.y)).rgb,float3(.2126,.7152,.0722))-.7);
                    gray += min(.025, bright * _FilmOptions.x * .25);
                }
                float2 p = (uv-.5) * float2(1,.8);
                gray *= 1 - _Film.z * smoothstep(.10,.48,dot(p,p));
                float grain = Hash(floor(uv * min(_ScaledScreenParams.xy,float2(1920,1080))))-.5;
                gray += grain * _Film.y * (_InkSettings.y>.5 ? .25 : 1);
                float dither = (frac(dot(floor(uv*_ScaledScreenParams.xy),float2(.754877,.56984)))-.5)*_FilmOptions.y;
                gray = clamp(gray + dither, .003, .985);
                // Intensity blends tonal response, never reintroduces scene hue.
                float natural = saturate(l / (1 + .08*l));
                gray = lerp(natural,gray,_Style.x);
                return half4(gray.xxx,source.a);
            }
            half3 PaperColor(half3 source, float2 uv, float rawDepth, float3 normal, float depth, float edge)
            {
                float3 world = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 n = normalize(normal + float3(0,.00001,0));
                float3 a = abs(n);
                bool top = a.y >= a.x && a.y >= a.z;
                bool side = !top && a.x > a.z;
                float2 sheet = top ? world.xz : (side ? world.zy : world.xy);
                float scale = max(.1,_PaperShape.x);
                float2 grid = sheet * scale;
                float2 cell = floor(grid), f = frac(grid);
                float upper = step(1,f.x+f.y);
                float2 center2 = (cell + lerp(1.0/3.0,2.0/3.0,upper)) / scale;
                float2 delta = center2-sheet;
                float3 center = world + (top ? float3(delta.x,0,delta.y) : (side ? float3(0,delta.y,delta.x) : float3(delta.x,delta.y,0)));
                float planeError = dot(n,center-world);
                if (top) center.y -= planeError*n.y/max(n.y*n.y,.001);
                else if (side) center.x -= planeError*n.x/max(n.x*n.x,.001);
                else center.z -= planeError*n.z/max(n.z*n.z,.001);
                float2 centerUV = ComputeNormalizedDeviceCoordinates(center,UNITY_MATRIX_VP);
                float centerDepth = LinearEyeDepth(StyleDepth(saturate(centerUV)),_ZBufferParams);
                float expectedDepth = -TransformWorldToView(center).z;
                float inside = all(centerUV > .002) && all(centerUV < .998) && expectedDepth > .1;
                float sameSurface = 1-smoothstep(.025+depth*.003,.08+depth*.008,abs(centerDepth-expectedDepth));
                half3 centerColor = SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,saturate(centerUV)).rgb;
                float footprint = max(length(ddx(grid)),length(ddy(grid)));
                float resolved = 1-smoothstep(.25,.8,footprint);
                float flatten = sameSurface*inside*resolved*_Paper.z;
                float sourceL = max(.001,dot(source,float3(.2126,.7152,.0722)));
                float centerL = max(.001,dot(centerColor,float3(.2126,.7152,.0722)));
                float ratio = sourceL/centerL;
                float contact = lerp(1,min(1,ratio),1-smoothstep(.25,.65,ratio));
                half3 paint = lerp(source,centerColor*contact,flatten*.92);
                float l = max(.001,dot(paint,float3(.2126,.7152,.0722)));
                float3 light = normalize(_MainLightPosition.xyz + float3(.0001,.0001,0));
                float smoothLight = .4+.6*saturate(dot(n,light));
                float albedo = min(1.2,l/max(.35,smoothLight));
                float tone = floor(sqrt(albedo)*4+.5)/4;
                half3 chroma = paint/l;
                chroma = lerp(chroma,floor(chroma*4+.5)/4,_Paper.z*.45);
                chroma = lerp(1.0.xxx,chroma,.84);
                half3 color = chroma*(tone*tone)*smoothLight;
                color = lerp(paint,color,_Paper.z*.95);
                float seed = Hash(cell+float2(upper*17,upper*31));
                float angle = Hash(cell+7.3)*6.2831853;
                float2 slope = float2(cos(angle),sin(angle))*(upper*2-1)*.38*resolved;
                float3 tangent = top ? float3(1,0,0) : (side ? float3(0,0,1) : float3(1,0,0));
                float3 bitangent = top ? float3(0,0,1) : float3(0,1,0);
                float3 folded = normalize(n+tangent*slope.x+bitangent*slope.y);
                float bands = clamp(round(_PaperShape.y)-2,3,6);
                float face = floor(saturate(dot(folded,light))*bands+.5)/bands;
                float paperLight = .4+.6*face;
                color *= lerp(1,clamp(paperLight/max(.3,smoothLight),.55,1.55),_Paper.y);
                color *= 1+(seed-.5)*.10*_Paper.y*resolved;
                float green = saturate((paint.g-max(paint.r,paint.b))*7);
                float lum = dot(color,float3(.2126,.7152,.0722));
                color = lerp(color,lum*half3(.74,1.07,.64),green*.35*_Paper.z);
                color *= lerp(half3(.82,.92,.94),half3(1.08,1.025,.88),saturate(paperLight));
                color = lerp(color,color*.90+half3(.08,.065,.035),_Paper.z*.4);
                float diagonal = abs(f.x+f.y-1)*.7071;
                float seam = min(diagonal,min(min(f.x,1-f.x),min(f.y,1-f.y)));
                float crease = 1-smoothstep(.004,.004+max(fwidth(seam),.002),seam);
                color *= 1-crease*.12*_Paper.y*resolved;
                color += crease*half3(.075,.065,.045)*_Paper.w*resolved;
                float fiber = Hash(floor(sheet*24))-.5;
                float fiberFootprint = max(length(ddx(sheet*24)),length(ddy(sheet*24)));
                color += fiber*_Paper.x*.6*(1-smoothstep(.35,1.2,fiberFootprint))*(_InkSettings.y>.5?.2:1);
                color = lerp(color,color*.65+half3(.045,.038,.025),edge*_Paper.w*.65);
                return max(0,color);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv=input.texcoord;
                half4 source=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv);
                if (_StyleMode > 2.5) return FilmColor(source,uv);
                float rawDepth=StyleDepth(uv);
                float depth=LinearEyeDepth(rawDepth,_ZBufferParams);
                float3 n=_StyleMode > 1.5 ? PaperSurfaceNormal(uv,rawDepth) : SampleSceneNormals(uv);
                float pixelWidth=max(.75,_Style.y*_ScaledScreenParams.y/1080);
                if(_InkSettings.y>.5)pixelWidth=1;
                float2 stepUV=pixelWidth/_ScaledScreenParams.xy;
                float dz=0,normalEdge=0;
                const float2 axes[4]={float2(1,0),float2(-1,0),float2(0,1),float2(0,-1)};
                [unroll]for(int i=0;i<4;i++)
                {
                    float2 p=saturate(uv+axes[i]*stepUV);
                    float d=LinearEyeDepth(StyleDepth(p),_ZBufferParams);
                    dz=max(dz,abs(d-depth)/max(.25,min(d,depth)));
                    if (_StyleMode < 1.5) normalEdge=max(normalEdge,1-saturate(dot(n,SampleSceneNormals(p))));
                }
                float distanceFade=1-smoothstep(_Style.w*.45,_Style.w,depth);
                float edge=max(smoothstep(.025,.11,dz),smoothstep(.18,.55,normalEdge)*_Palette.w)*distanceFade;
                if (_StyleMode > 1.5)
                {
                    if (depth > _ProjectionParams.z * .99) return source;
                    half3 paperColor = PaperColor(source.rgb,uv,rawDepth,n,depth,edge);
                    return half4(lerp(source.rgb,paperColor,_Style.x),source.a);
                }
                // Constant screen-space fibres: no time noise, temporal jitter or camera-dependent random seed.
                float2 cells=min(_ScaledScreenParams.xy,float2(960,540));
                if(_InkSettings.y>.5)cells=min(cells,float2(320,180));
                float paper=Hash(floor(uv*cells))-.5;
                float inkVariation=.92+.08*Hash(floor(uv*float2(90,50)));
                float l=max(.0001,dot(source.rgb,float3(.2126,.7152,.0722)));
                float tone=log2(1+l);
                float bands=_Palette.y;
                float bandL=exp2(floor(tone*bands+.5)/bands)-1;
                half3 col=source.rgb*lerp(1,bandL/l,.66*_Style.x);
                col=lerp(dot(col,float3(.2126,.7152,.0722)).xxx,col,_Palette.x);
                col*=lerp(_ShadowTint.rgb,_HighlightTint.rgb,saturate(l));
                float hatch=pow(saturate(sin((uv.x+uv.y)*(_InkSettings.y>.5?200:640))*.5+.5),12)*_InkSettings.x*(1-saturate(l*2));
                col=max(0,col*(1-hatch)+paper*_Palette.z);
                col=lerp(col,_Ink.rgb,saturate(edge*_Style.z*inkVariation));
                return half4(lerp(source.rgb,col,_Style.x),source.a);
            }
            ENDHLSL
        }
    }
}
