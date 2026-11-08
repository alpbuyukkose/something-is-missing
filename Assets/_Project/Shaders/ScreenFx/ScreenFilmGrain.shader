// Noise that changes a few times a second. _GrainType picks its look.
Shader "Hidden/Something Is Missing/Screen Film Grain"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "FilmGrain"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _GrainType;
            float _Intensity;
            float _Fps;
            float _ShadowFade;

            uint Pcg(uint v)
            {
                uint state = v * 747796405u + 2891336453u;
                uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
                return (word >> 22u) ^ word;
            }

            // 0..1 per integer position, frame and channel.
            float Hash(int2 p, uint seed)
            {
                return Pcg((uint)p.x + Pcg((uint)p.y + Pcg(seed))) / 4294967295.0;
            }

            // Smoothly blended hashes, for grain bigger than a pixel.
            float ValueNoise(float2 p, uint seed)
            {
                int2 i = (int2)floor(p);
                float2 f = smoothstep(0, 1, frac(p));
                float a = Hash(i, seed), b = Hash(i + int2(1, 0), seed);
                float c = Hash(i + int2(0, 1), seed), d = Hash(i + int2(1, 1), seed);
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float3 Grain(int2 pixel, uint frame)
            {
                int type = (int)_GrainType;
                uint seed = frame * 4u;
                float3 n = (Hash(pixel, seed) - 0.5).xxx; // fine
                if (type == 0) // soft: average of four, fewer extremes
                    n = ((Hash(pixel, seed) + Hash(pixel, seed + 1u) + Hash(pixel, seed + 2u) + Hash(pixel, seed + 3u)) * 0.25 - 0.5).xxx;
                else if (type == 2) // colored
                    n = float3(Hash(pixel, seed), Hash(pixel, seed + 1u), Hash(pixel, seed + 2u)) - 0.5;
                else if (type == 3) // coarse clumps about 4 pixels wide
                    n = ((ValueNoise(pixel / 4.0, seed) - 0.5) * 1.6).xxx;
                else if (type == 4) // horizontal streaks, each row on its own
                {
                    float streak = ValueNoise(float2(pixel.x / 48.0, pixel.y), seed);
                    float row = Hash(int2(0, pixel.y), seed + 1u);
                    n = ((streak * 0.7 + row * 0.3 - 0.5) * 1.6).xxx;
                }
                return n;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
                int2 pixel = (int2)(uv * _ScreenParams.xy);
                uint frame = (uint)(_Time.y * _Fps);
                float3 noise = Grain(pixel, frame);
                half3 s = LinearToSRGB(saturate(c.rgb)); // noise in display space so darks and lights get the same grain
                half luma = dot(s, half3(0.2126, 0.7152, 0.0722));
                float amount = _Intensity * lerp(1.0, luma, _ShadowFade);
                c.rgb = SRGBToLinear(saturate(s + noise * amount));
                return c;
            }
            ENDHLSL
        }
    }
}
