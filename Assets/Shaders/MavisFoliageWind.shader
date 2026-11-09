Shader "Mavis/FoliageWind"
{
    // URP-Lit derived shader that adds per-vertex wind displacement.
    // Driven by global vectors:
    //   _MavisWindDir   (xyz: normalized wind direction, w: time)
    //   _MavisWindParams (x: strength, y: frequency, z: trunkStiffness, w: gustiness)
    // Instanced anchor, inverse height, response and stiffness keep all LODs rooted.
    // Authored botany can instead bake each plant's height into vertex colour alpha.
    Properties
    {
        _MainTex              ("Base Map", 2D) = "white" {}
        [HideInInspector] _BaseMap ("URP Shadow Base Map", 2D) = "white" {}
        _AlphaMap             ("Cutout Mask", 2D) = "white" {}
        _UseAlphaMap          ("Use Cutout Mask", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        _BaseColor            ("Base Color", Color) = (1,1,1,1)
        _Smoothness           ("Smoothness", Range(0,1)) = 0.05
        _Metallic             ("Metallic", Range(0,1)) = 0.0
        _Cutoff               ("Alpha Cutoff", Range(0,1)) = 0.4
        _WindBend             ("Wind Bend Strength", Range(0,4)) = 1.0
        _WindFrequency        ("Wind Frequency", Range(0,8)) = 1.0
        _WindTrunkStiffness   ("Trunk Stiffness (0=top moves, 1=stiff)", Range(0,1)) = 0.2
        _WindGust             ("Wind Gust", Range(0,4)) = 0.6
        _LocalWindScale       ("Per-Instance Wind Scale", Range(0,2)) = 1.0
        [HideInInspector] _MavisWindAnchorY ("Wind Anchor Y", Float) = 0.0
        [HideInInspector] _MavisWindInvHeight ("Wind Inverse Height", Float) = 1.0
        [HideInInspector] _MavisWindResponse ("Wind Response", Float) = 1.0
        _PlayerPushStrength   ("Player Push Strength", Range(0,4)) = 1.0
        _PlayerPushHeightBias ("Player Push Height Bias (height where push applies)", Range(0,4)) = 1.0
        [HideInInspector] _ClipBelowSea ("Clip On Beach", Float) = 0
        [HideInInspector] _UseBladeHeight ("Use Baked Blade Height", Float) = 0
        [HideInInspector] _AuthoredGrass ("Preserve authored grass blade colours", Float) = 0
        _GrassTintStrength ("Legacy meadow tint (0 for bark)", Range(0,1.25)) = 1
        _SunDesaturation ("Legacy sunlight desaturation", Range(0,1)) = 0.55
        [Toggle] _BotanicalSurface ("Botanical PBR Surface", Float) = 0
        _RootSeatDepth ("Tree root extension (local units)", Range(0, 0.2)) = 0
        _BotanicalTriplanar ("Bark Triplanar Tiling (0 disables)", Float) = 0
        [Normal] _BotanicalNormal ("Botanical Normal", 2D) = "bump" {}
        _BotanicalSurfaceMap ("R Roughness G Occlusion B Transmission A Emission", 2D) = "white" {}
        [HDR] _BotanicalEmission ("Botanical Emission", Color) = (0,0,0,0)
        _EcologyDistance ("Ecology Fade Start End", Vector) = (0,0,0,0)
        [HideInInspector] _EcologyLodFade ("Ecology LOD Coverage", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "True"
        }

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #if defined(LOD_FADE_CROSSFADE)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _BaseColor;
                half   _Smoothness;
                half   _Metallic;
                half   _Cutoff;
                half   _UseAlphaMap;
                half   _WindBend;
                half   _WindFrequency;
                half   _WindGust;
                half   _LocalWindScale;
                half   _PlayerPushHeightBias;
                half   _ClipBelowSea;
                half   _UseBladeHeight;
                half   _AuthoredGrass;
                half   _GrassTintStrength;
                half   _SunDesaturation;
                half   _BotanicalSurface;
                half   _RootSeatDepth;
                float  _BotanicalTriplanar;
                half4  _BotanicalEmission;
                float4 _EcologyDistance;
                float4 _EcologyLodFade;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(FoliageInstances)
                UNITY_DEFINE_INSTANCED_PROP(float, _MavisWindAnchorY)
                UNITY_DEFINE_INSTANCED_PROP(float, _MavisWindInvHeight)
                UNITY_DEFINE_INSTANCED_PROP(float, _MavisWindResponse)
                UNITY_DEFINE_INSTANCED_PROP(float, _WindTrunkStiffness)
                // FoliageWindDriver overrides this per renderer/material slot.
                // Keep it instanced so the property block does not split grass batches.
                UNITY_DEFINE_INSTANCED_PROP(float, _PlayerPushStrength)
            UNITY_INSTANCING_BUFFER_END(FoliageInstances)
            #define _MavisWindAnchorY UNITY_ACCESS_INSTANCED_PROP(FoliageInstances, _MavisWindAnchorY)
            #define _MavisWindInvHeight UNITY_ACCESS_INSTANCED_PROP(FoliageInstances, _MavisWindInvHeight)
            #define _MavisWindResponse UNITY_ACCESS_INSTANCED_PROP(FoliageInstances, _MavisWindResponse)
            #define _WindTrunkStiffness UNITY_ACCESS_INSTANCED_PROP(FoliageInstances, _WindTrunkStiffness)
            #define _PlayerPushStrength UNITY_ACCESS_INSTANCED_PROP(FoliageInstances, _PlayerPushStrength)

            // Global wind state (set every frame by FoliageWindDriver)
            float4 _MavisWindDir;       // xyz = direction (normalized), w = time
            float4 _MavisWindParams;    // x = strength, y = pulse frequency, z = frequency multiplier, w = gustiness

            // Grass renderers set _ClipBelowSea. FoliageInteractionSystem fills the sand band.
            float _MavisSeaLevel;
            float _MavisBeachCeiling;
            float4 _MavisGrassFadeRange; // start/end/inverse span/enabled
            float4 _MavisGrassShadowRange;

            // Global player state for foliage squish (xyz = world pos, w = radius)
            float4 _MavisPlayerPos;
            // Four moving bodies plus eight short-lived footprints. Shared by all LODs.
            float4 _MavisFoliageBodies[12]; // xyz = feet, w = radius
            float4 _MavisFoliageMotion[12]; // xy = horizontal motion, z = strength
            int _MavisFoliageBodyCount;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaMap);
            SAMPLER(sampler_AlphaMap);
            TEXTURE2D(_BotanicalNormal);
            SAMPLER(sampler_BotanicalNormal);
            TEXTURE2D(_BotanicalSurfaceMap);
            SAMPLER(sampler_BotanicalSurfaceMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 restPositionWS : TEXCOORD3;
                float  squish     : TEXCOORD4;
                half4  color      : TEXCOORD5;
                half4 tangentWS   : TEXCOORD6;
                half fogFactor    : TEXCOORD7;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float BladeHeight(float3 positionWS, float bakedHeight)
            {
                // Combined terrain-following grass cannot share one world-Y pivot.
                // Its vertex colour alpha stores height within each individual blade.
                return lerp(saturate((positionWS.y - _MavisWindAnchorY) * _MavisWindInvHeight),
                    saturate(bakedHeight), _UseBladeHeight);
            }

            float3 ComputeWindOffset(float3 positionWS, float bakedHeight)
            {
                float3 dir = _MavisWindDir.xyz;
                dir.y = 0.0;
                dir = dot(dir, dir) > 1e-4 ? normalize(dir) : float3(1.0, 0.0, 0.0);
                float3 sideDir = float3(-dir.z, 0.0, dir.x);
                float  time = _MavisWindDir.w;
                float  baseStrength = _MavisWindParams.x;
                float  gustiness    = _MavisWindParams.w;
                float  freq         = _WindFrequency * max(_MavisWindParams.z, 0.05);

                // Build a continuous wind field in world space. Adjacent vertices share
                // the same broad motion instead of receiving unrelated random phases.
                float heightFactor = BladeHeight(positionWS, bakedHeight);
                heightFactor = pow(heightFactor, lerp(1.25, 3.0, _WindTrunkStiffness));

                float2 windPosition = positionWS.xz;
                float broadPhase = time * freq * 0.72 +
                    dot(windPosition, dir.xz * 0.052 + sideDir.xz * 0.018);
                float detailPhase = time * freq * 1.18 +
                    dot(windPosition, dir.xz * -0.13 + sideDir.xz * 0.16) + 1.7;
                float crossPhase = time * freq * 0.56 +
                    dot(windPosition, sideDir.xz * 0.075 + dir.xz * 0.025) - 0.9;
                float sway = sin(broadPhase) * 0.72 + sin(detailPhase) * 0.22 + sin(crossPhase) * 0.06;
                float gustPhase = time * max(_MavisWindParams.y, 0.08) * 0.55 +
                    dot(windPosition, dir.xz * 0.021) + 0.6;
                float gust = sin(gustPhase) * 0.5 + 0.5;
                float gustAmount = saturate(gustiness * _WindGust);
                float gustEnvelope = lerp(1.0 - gustAmount * 0.16, 1.0 + gustAmount * 0.34, gust);

                float strength = _WindBend * _LocalWindScale * baseStrength *
                                _MavisWindResponse * heightFactor * gustEnvelope;

                // The crosswind term keeps crowns and grass from moving like a rigid wall.
                // Roots remain anchored by heightFactor; only tips get the small lift.
                float3 offset = dir * (sway * strength)
                              + sideDir * (sin(detailPhase + 0.8) * strength * 0.12)
                              + float3(0.0, gust * strength * 0.035, 0.0);
                return offset;
            }

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                float3 positionOS = IN.positionOS.xyz;
                // Extend the visible foot of the large island trunk into slopes.
                // The same vertices drive the color, depth, and shadow passes.
                [branch] if (_RootSeatDepth > 0.0h)
                    positionOS.y -= _RootSeatDepth * (1.0 - smoothstep(0.04, 0.29, positionOS.y));
                float3 originalWS = TransformObjectToWorld(positionOS);
                float3 positionWS = originalWS + ComputeWindOffset(originalWS, IN.color.a);

                float playerInfluence = 0;
                float3 push = 0;
                float rootMask = BladeHeight(originalWS, IN.color.a);
                rootMask = rootMask * rootMask;
                // Distant foliage still moves in the wind, without paying for close-up interaction.
                float3 cameraDelta = originalWS - _WorldSpaceCameraPos;
                if (dot(cameraDelta, cameraDelta) < 65.0 * 65.0)
                {
                    [loop] for (int i = 0; i < _MavisFoliageBodyCount; i++)
                    {
                        float2 delta = originalWS.xz - _MavisFoliageBodies[i].xz;
                        float bodyRadius = max(_MavisFoliageBodies[i].w, 0.001);
                        // Almost all vertices are outside a body's footprint. Avoid
                        // its length, division and direction work when influence is zero.
                        [branch] if (dot(delta, delta) >= bodyRadius * bodyRadius ||
                            abs(originalWS.y - _MavisFoliageBodies[i].y - 0.65) >= 2.5)
                            continue;
                        float dist = length(delta);
                        float influence = saturate(1.0 - dist / bodyRadius);
                        influence *= influence * saturate(1.0 - abs(originalWS.y - _MavisFoliageBodies[i].y - 0.65) / 2.5);
                        influence *= _MavisFoliageMotion[i].z;
                        float2 direction = delta / max(dist, 0.05) + _MavisFoliageMotion[i].xy * 0.35;
                        push += float3(direction.x, -0.35, direction.y) * influence;
                        playerInfluence = max(playerInfluence, influence);
                    }
                }
                // Saturate overlapping footprints instead of letting them flatten the entire patch.
                push /= max(1.0, length(push.xz));
                positionWS += push * rootMask * _PlayerPushStrength * 0.65;
                OUT.squish = playerInfluence;

                OUT.positionWS = positionWS;
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.tangentWS = half4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w * GetOddNegativeScale());
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color      = IN.color;
                OUT.restPositionWS = originalWS;
                return OUT;
            }

            void GrassDistanceCoverage(float3 restWS)
            {
                if (_ClipBelowSea < .5h || _MavisGrassFadeRange.w < .5) return;
                float3 delta = restWS - _WorldSpaceCameraPos;
                float squared = dot(delta,delta);
                if (squared <= _MavisGrassFadeRange.x * _MavisGrassFadeRange.x) return;
                float visibility = 1-smoothstep(_MavisGrassFadeRange.x,_MavisGrassFadeRange.y,sqrt(squared));
                // Stable rest-space cells follow blades through wind/bending.
                // No screen-space stipple swimming or whole-patch density wipe.
                float2 cell = floor(restWS.xz * 3.0);
                float noise = frac(sin(dot(cell,float2(127.1,311.7))) * 43758.5453);
                clip(visibility-noise-.00001);
            }

            void EcologyCoverage(float3 restWS)
            {
                if (abs(_EcologyLodFade.y) > .5)
                {
                    float noise = frac(sin(dot(floor(restWS.xz * 37),float2(127.1,311.7))) * 43758.5453);
                    clip(_EcologyLodFade.y > 0 ? noise - _EcologyLodFade.x : _EcologyLodFade.x - noise - .00001);
                }
                if (_EcologyDistance.y > 0)
                {
                    float fade=1-smoothstep(_EcologyDistance.x,_EcologyDistance.y,distance(restWS,_WorldSpaceCameraPos));
                    float noise=frac(sin(dot(floor(restWS.xz*19),float2(71.3,119.7)))*27183.17);
                    clip(fade-noise-.00001);
                }
            }

            half4 frag(Varyings IN, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                GrassDistanceCoverage(IN.restPositionWS);
                EcologyCoverage(IN.restPositionWS);
                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(IN.positionCS);
                #endif
                if (_ClipBelowSea > 0.5h && IN.positionWS.y < _MavisSeaLevel + _MavisBeachCeiling)
                    clip(-1);
                half4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half opacity = baseTex.a * _BaseColor.a;
                if (_UseAlphaMap > 0.5h)
                    opacity *= SAMPLE_TEXTURE2D(_AlphaMap, sampler_AlphaMap, IN.uv).r;
                clip(opacity - _Cutoff);
                half edgeCoverage = _AlphaToMaskAvailable > .5 && _Cutoff > .01h ? saturate((opacity-_Cutoff)/max(fwidth(opacity),.005h)+.5h) : 1.0h;
                half3 albedo  = baseTex.rgb * _BaseColor.rgb;
                // Baked root-to-tip vertex colour: roots sit in their own shadow while
                // tips catch light, so each clump gains volume without extra passes.
                albedo *= IN.color.rgb;
                // Opt-in for authored botany. Existing meadow/tree materials retain
                // their calibrated colour response; flowers and fungi keep their hue.
                if (_BotanicalSurface > .5h)
                {
                    half3 normalTS=UnpackNormal(SAMPLE_TEXTURE2D(_BotanicalNormal,sampler_BotanicalNormal,IN.uv));
                    half3 normal=normalize(IN.normalWS);
                    half3 tangent=dot(IN.tangentWS.xyz,IN.tangentWS.xyz)>.001?normalize(IN.tangentWS.xyz):normalize(cross(normal,abs(normal.y)>.95?half3(0,0,1):half3(0,1,0)));
                    half3 bitangent=cross(normal,tangent)*IN.tangentWS.w;
                    normal=normalize(mul(normalTS,half3x3(tangent,bitangent,normal)));
                    normal*=IS_FRONT_VFACE(face,1.0h,-1.0h);
                    half4 map=SAMPLE_TEXTURE2D(_BotanicalSurfaceMap,sampler_BotanicalSurfaceMap,IN.uv);
                    if (_BotanicalTriplanar > .001)
                    {
                        float3 pos=IN.restPositionWS*_BotanicalTriplanar;
                        half3 N=normalize(IN.normalWS);
                        half3 weight=pow(abs(N),4);weight/=max(dot(weight,half3(1,1,1)),.001h);
                        half3 base=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,pos.zy).rgb*weight.x+
                            SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,pos.xz).rgb*weight.y+
                            SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,pos.xy).rgb*weight.z;
                        albedo=base*_BaseColor.rgb*IN.color.rgb;
                        half3 nx=UnpackNormal(SAMPLE_TEXTURE2D(_BotanicalNormal,sampler_BotanicalNormal,pos.zy));
                        half3 ny=UnpackNormal(SAMPLE_TEXTURE2D(_BotanicalNormal,sampler_BotanicalNormal,pos.xz));
                        half3 nz=UnpackNormal(SAMPLE_TEXTURE2D(_BotanicalNormal,sampler_BotanicalNormal,pos.xy));
                        normal=normalize(half3(nx.z*sign(N.x),nx.y,nx.x)*weight.x+
                            half3(ny.x,ny.z*sign(N.y),ny.y)*weight.y+half3(nz.x,nz.y,nz.z*sign(N.z))*weight.z);
                    }
                    // Whole specimens vary gently, without stretching texture frequency.
                    float2 root=GetObjectToWorldMatrix()._m03_m23;
                    half tone=frac(sin(dot(root,float2(12.9898,78.233)))*43758.5453);
                    albedo*=lerp(half3(.88h,.94h,.86h),half3(1.05h,1.0h,.94h),tone);
                    SurfaceData surface=(SurfaceData)0;
                    surface.albedo=albedo;surface.alpha=1;surface.metallic=_Metallic;
                    surface.smoothness=saturate((1-map.r)*_Smoothness);
                    half rootOcclusion=lerp(.74h,1.0h,smoothstep(.02h,.65h,IN.color.a));
                    surface.occlusion=map.g*lerp(1.0h,rootOcclusion,saturate(_UseBladeHeight));surface.normalTS=normalTS;
                    surface.emission=_BotanicalEmission.rgb*map.a;
                    InputData input=(InputData)0;
                    input.positionWS=IN.positionWS;input.normalWS=normal;
                    input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(IN.positionWS);
                    input.shadowCoord=TransformWorldToShadowCoord(IN.positionWS);
                    input.bakedGI=SampleSH(normal);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(IN.positionCS);
                    input.shadowMask=half4(1,1,1,1);
                    half4 lit=UniversalFragmentPBR(input,surface);
                    Light sun=GetMainLight(input.shadowCoord);
                    half transmit=pow(saturate(dot(-input.viewDirectionWS,sun.direction)),3)*map.b*.13h;
                    lit.rgb+=albedo*sun.color*transmit*sun.shadowAttenuation;
                    lit.rgb=MixFog(lit.rgb,IN.fogFactor);
                    lit.a=edgeCoverage;
                    return lit;
                }
                // Broad and fine patches keep one shared grass texture from reading as a flat carpet.
                float2 meadow = IN.positionWS.xz;
                float broad = sin(meadow.x * 0.041 + meadow.y * 0.017) * sin(meadow.y * 0.033 - meadow.x * 0.012);
                float fine = sin(meadow.x * 0.19 + 1.7) * sin(meadow.y * 0.16);
                // The diffuse card is a warm yellow-green; pull most of the hue out
                // of it and rebuild a deeper green, otherwise the whole field reads
                // as neon. Keep the brightness drift small so it stays a tint, not
                // alternating light and dark stripes.
                half3 authoredBlade = albedo;
                half luma = dot(albedo, half3(0.299h, 0.587h, 0.114h));
                // Rebuild the hue almost entirely from luma. The card texture is
                // dominated by yellow-green, so a partial mix always leaks lime
                // back in on the sunlit face; the last 22% keeps blade detail.
                half3 tint = lerp(half3(0.52h, 0.88h, 0.54h), half3(0.84h, 0.90h, 0.68h),
                    saturate(broad * 0.5 + 0.5) * 0.65h);
                albedo = lerp(albedo, luma * tint, saturate(0.82h * _GrassTintStrength));
                albedo *= 0.88h + 0.10h * (fine * 0.5 + 0.5);
                albedo = lerp(authoredBlade, albedo, saturate(_GrassTintStrength) * (1.0h - _AuthoredGrass));

                // Simple hemisphere ambient + main directional light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half3 N = normalize(IN.normalWS);
                half NdotL = saturate(dot(N, mainLight.direction));
                // Grass cards are near-vertical, so a plain N.L makes whole clumps go
                // black when backlit. Wrap the diffuse and add a cheap transmission
                // term so sun behind the field still reads as glowing blades.
                half wrap = saturate((NdotL + 0.40h) / 1.40h);
                half back = saturate(dot(-N, mainLight.direction)) * 0.20h;
                half3 sky    = half3(0.55, 0.62, 0.7);
                half3 ground = half3(0.18, 0.16, 0.13);
                half3 hemi   = max(SampleSH(N), lerp(ground, sky, N.y * 0.5 + 0.5) * 0.3h);
                hemi = max(hemi, half3(0.11h, 0.14h, 0.13h));

                // Warm sunlight multiplied into a green albedo is what pushes the field
                // toward chartreuse no matter how the texture is tinted. Desaturate the
                // sun for foliage only; the terrain keeps its warmer response.
                half sunLuma = dot(mainLight.color, half3(0.333h, 0.334h, 0.333h));
                half3 sun = lerp(mainLight.color, sunLuma.xxx, _SunDesaturation);
                // Match the island terrain's exposure: the old 0.80 direct term plus a
                // 1.2 wrap peak made grass ~40% brighter than the ground beneath it,
                // which is what read as a glowing carpet instead of a lit surface.
                half3 lit = albedo * (hemi * 0.86h + sun * (wrap + back) *
                    mainLight.distanceAttenuation * mainLight.shadowAttenuation * 0.54h);
                // Curved grass ribbons transmit a little light on the shaded face.
                lit += albedo * _AuthoredGrass * (0.07h + 0.09h * back);

                // Lighten leaves/branches slightly where player is pushing them
                lit += albedo * IN.squish * 0.15;
                lit = MixFog(lit, IN.fogFactor);

                return half4(lit, edgeCoverage);
            }
            float3 _LightDirection;
            float3 _LightPosition;

            Varyings shadowVert(Attributes IN)
            {
                // Reuse the visible vertex deformation, including player avoidance.
                Varyings OUT = vert(IN);
                float3 lightDirection = _LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    lightDirection = normalize(_LightPosition - OUT.positionWS);
                #endif
                OUT.positionCS = TransformWorldToHClip(ApplyShadowBias(
                    OUT.positionWS, normalize(OUT.normalWS), lightDirection));
                #if UNITY_REVERSED_Z
                    OUT.positionCS.z = min(OUT.positionCS.z, UNITY_NEAR_CLIP_VALUE * OUT.positionCS.w);
                #else
                    OUT.positionCS.z = max(OUT.positionCS.z, UNITY_NEAR_CLIP_VALUE * OUT.positionCS.w);
                #endif
                return OUT;
            }

            half4 shadowFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                GrassDistanceCoverage(IN.restPositionWS);
                EcologyCoverage(IN.restPositionWS);
                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(IN.positionCS);
                #endif
                if (_ClipBelowSea > 0.5h && IN.positionWS.y < _MavisSeaLevel + _MavisBeachCeiling)
                    clip(-1);
                half opacity = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a * _BaseColor.a;
                if (_UseAlphaMap > 0.5h)
                    opacity *= SAMPLE_TEXTURE2D(_AlphaMap, sampler_AlphaMap, IN.uv).r;
                clip(opacity - _Cutoff);
                return 0;
            }
            half4 grassShadowFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                if(_ClipBelowSea>.5h && _MavisGrassShadowRange.w>.5)
                {
                    float distance=length(IN.restPositionWS-_WorldSpaceCameraPos);
                    float coverage=1-smoothstep(_MavisGrassShadowRange.x,_MavisGrassShadowRange.y,distance);
                    float2 cell=floor(IN.restPositionWS.xz*9);
                    float noise=frac(sin(dot(cell,float2(127.1,311.7)))*43758.5453);
                    clip(coverage-noise-.00001);
                }
                return shadowFrag(IN);
            }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            AlphaToMask On
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma multi_compile_instancing
            #pragma fragment frag
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull [_Cull]
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma multi_compile_instancing
            #pragma fragment shadowFrag
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex shadowVert
            #pragma multi_compile_instancing
            #pragma fragment grassShadowFrag
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }

    }

    FallBack Off
}
