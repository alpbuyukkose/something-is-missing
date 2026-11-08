// Darkens the screen edges toward a color.
Shader "Hidden/Something Is Missing/Screen Vignette"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "Vignette"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Intensity;
            float _Radius;
            float _Softness;
            float _Roundness;
            float4 _VignetteColor;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float2 d = uv - 0.5;
                d.x *= lerp(1.0, _ScreenParams.x / _ScreenParams.y, _Roundness);
                float inside = smoothstep(_Radius, _Radius - _Softness, length(d)); // 1 center, 0 edge
                c.rgb = lerp(_VignetteColor.rgb, c.rgb, lerp(1.0, inside, _Intensity));
                return c;
            }
            ENDHLSL
        }
    }
}
