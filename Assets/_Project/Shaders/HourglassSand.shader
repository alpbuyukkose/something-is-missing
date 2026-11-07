// Sand inside an hourglass chamber. Everything above _SandPlane is cut away (set per renderer by HourglassView).
// Back faces are shaded as the flat sand surface, so the cut reads as solid sand. One pass, no textures.
Shader "Something Is Missing/Hourglass Sand"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0.79, 0.64, 0.42, 1)
        [HideInInspector] _SandPlane ("Sand Plane (normal xyz, offset w)", Vector) = (0, 1, 0, -1000)
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _SandPlane;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            // Half-Lambert: sides turned away from the light stay readable, so the fill level is always visible.
            half3 Diffuse(Light light, float3 n)
            {
                half wrap = saturate(dot(n, light.direction) * 0.5 + 0.5);
                return light.color * (light.distanceAttenuation * light.shadowAttenuation * wrap * wrap);
            }

            half4 Frag(Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                clip(-(dot(_SandPlane.xyz, i.positionWS) + _SandPlane.w));
                float3 n = front ? normalize(i.normalWS) : _SandPlane.xyz;

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalWS = n;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                half4 shadowMask = half4(1, 1, 1, 1);

                half3 light = SampleSH(n);
                light += Diffuse(GetMainLight(TransformWorldToShadowCoord(i.positionWS)), n);
                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    light += Diffuse(GetAdditionalLight(lightIndex, i.positionWS, shadowMask), n);
                LIGHT_LOOP_END
                #endif
                return half4(light * _BaseColor.rgb, 1);
            }
            ENDHLSL
        }
    }
}
