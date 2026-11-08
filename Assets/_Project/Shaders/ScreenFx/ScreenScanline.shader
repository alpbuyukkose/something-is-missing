// Horizontal dark lines in screen pixels. Each pixel row is darkened by the exact share of it a line covers,
// so the lines move smoothly when scrolling and the overall brightness never pulses.
Shader "Hidden/Something Is Missing/Screen Scanline"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "Scanline"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Spacing;
            float _Thickness;
            float _Intensity;
            float _ScrollSpeed;

            // Total dark length from 0 up to y.
            float DarkUpTo(float y)
            {
                float lineWidth = _Thickness * _Spacing;
                return floor(y / _Spacing) * lineWidth + min(frac(y / _Spacing) * _Spacing, lineWidth);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
                float offset = frac(_Time.y * _ScrollSpeed) * _Spacing; // wraps every line, keeps numbers small
                float y = floor(uv.y * _ScreenParams.y) + offset; // uv.y grows upward, so a growing offset moves the lines down
                float dark = DarkUpTo(y + 1) - DarkUpTo(y);
                c.rgb *= 1.0 - dark * _Intensity;
                return c;
            }
            ENDHLSL
        }
    }
}
