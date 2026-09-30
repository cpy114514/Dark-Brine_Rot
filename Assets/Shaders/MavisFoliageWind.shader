Shader "Mavis/FoliageWind"
{
    // URP-Lit derived shader that adds per-vertex wind displacement.
    // Driven by global vectors:
    //   _MavisWindDir   (xyz: normalized wind direction, w: time)
    //   _MavisWindParams (x: strength, y: frequency, z: trunkStiffness, w: gustiness)
    // Per-renderer override: _MavisWindStrengthScale (float, via MaterialPropertyBlock) lets a
    // single mesh apply a different wind response (e.g. grass vs. tree).
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

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _BaseColor;
                half   _Smoothness;
                half   _Metallic;
                half   _Cutoff;
                half   _UseAlphaMap;
                half   _WindBend;
                half   _WindFrequency;
                half   _WindTrunkStiffness;
                half   _WindGust;
                half   _LocalWindScale;
                float  _MavisWindAnchorY;
                float  _MavisWindInvHeight;
                half   _MavisWindResponse;
                half   _PlayerPushStrength;
                half   _PlayerPushHeightBias;
            CBUFFER_END

            // Global wind state (set every frame by FoliageWindDriver)
            float4 _MavisWindDir;       // xyz = direction (normalized), w = time
            float4 _MavisWindParams;    // x = base strength, y = phase, z = trunkStiffness override, w = gustiness

            // Global player state for foliage squish (xyz = world pos, w = radius)
            float4 _MavisPlayerPos;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaMap);
            SAMPLER(sampler_AlphaMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 viewDirWS  : TEXCOORD3;
                float  squish     : TEXCOORD4;
            };

            float3 ComputeWindOffset(float3 positionWS)
            {
                float3 dir = _MavisWindDir.xyz;
                dir.y = 0.0;
                dir = dot(dir, dir) > 1e-4 ? normalize(dir) : float3(1.0, 0.0, 0.0);
                float3 sideDir = float3(-dir.z, 0.0, dir.x);
                float  time = _MavisWindDir.w;
                float  baseStrength = _MavisWindParams.x;
                float  gustiness    = _MavisWindParams.w;
                float  freq         = _WindFrequency;

                // Build a continuous wind field in world space. Adjacent vertices share
                // the same broad motion instead of receiving unrelated random phases.
                float heightFactor = saturate((positionWS.y - _MavisWindAnchorY) * _MavisWindInvHeight);
                heightFactor *= heightFactor;
                heightFactor = lerp(1.0 - _WindTrunkStiffness, 1.0, heightFactor);

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
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                positionWS += ComputeWindOffset(positionWS);

                // Player avoidance: when a vertex is within the player's push radius,
                // displace it radially away from the player and squash it toward the ground.
                float3 playerDelta = positionWS - _MavisPlayerPos.xyz;
                float  playerDist  = length(playerDelta);
                float  playerRadius = max(_MavisPlayerPos.w, 0.0001);
                float  playerInfluence = 1.0 - saturate(playerDist / playerRadius);
                playerInfluence *= playerInfluence; // ease-out

                // height bias: only push the higher portions (more grass-like / leaf-like)
                float heightAbovePlayer = saturate(positionWS.y - _MavisPlayerPos.y);
                playerInfluence *= saturate(0.3 + heightAbovePlayer * _PlayerPushHeightBias);

                if (playerInfluence > 0.0 && playerDist > 1e-4)
                {
                    float3 pushDir = playerDelta / playerDist;
                    float  pushAmt = playerInfluence * _PlayerPushStrength;
                    positionWS += pushDir * pushAmt * 0.5;          // lateral push
                    positionWS.y -= pushAmt * 0.35;                 // squish downward
                }
                OUT.squish = playerInfluence;

                OUT.positionWS = positionWS;
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.viewDirWS  = GetWorldSpaceViewDir(positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half opacity = baseTex.a * _BaseColor.a;
                if (_UseAlphaMap > 0.5h)
                    opacity *= SAMPLE_TEXTURE2D(_AlphaMap, sampler_AlphaMap, IN.uv).r;
                clip(opacity - _Cutoff);
                half3 albedo  = baseTex.rgb * _BaseColor.rgb;

                // Simple hemisphere ambient + main directional light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half3 N = normalize(IN.normalWS);
                half NdotL = saturate(dot(N, mainLight.direction));
                half3 sky    = half3(0.55, 0.62, 0.7);
                half3 ground = half3(0.18, 0.16, 0.13);
                half3 hemi   = max(SampleSH(N), lerp(ground, sky, N.y * 0.5 + 0.5) * 0.3h);
                hemi = max(hemi, half3(0.22h, 0.27h, 0.25h));

                half3 lit = albedo * (hemi * 1.05h + mainLight.color * (0.18h + NdotL * 0.72h) *
                    mainLight.distanceAttenuation * mainLight.shadowAttenuation * 0.85h);

                // Lighten leaves/branches slightly where player is pushing them
                lit += albedo * IN.squish * 0.15;

                return half4(lit, 1.0h);
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
                half opacity = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a * _BaseColor.a;
                if (_UseAlphaMap > 0.5h)
                    opacity *= SAMPLE_TEXTURE2D(_AlphaMap, sampler_AlphaMap, IN.uv).r;
                clip(opacity - _Cutoff);
                return 0;
            }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
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
            #pragma fragment shadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }

    }

    FallBack Off
}
