// Low resolution, then posterize or map to a palette (closest color in Oklab).
// Dither only in the darkest and brightest areas, so the mid tones stay clean bands.
Shader "Hidden/Something Is Missing/Screen Retro"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "Retro"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define MAX_COLORS 32

            float _PixelHeight;
            float _Brightness;
            float _Contrast;
            float _Saturation;
            float _Mode;
            float _Levels;
            float _PosterizeSpace;
            float _PaletteCount;
            float _KeepStart;
            float _KeepTolerance;
            float4 _PaletteRgb[MAX_COLORS];
            float4 _PaletteLab[MAX_COLORS];
            float _DitherStrength;
            float _ShadowRange;
            float _HighlightRange;
            float _DitherPattern;

            float Bayer2(float2 a)
            {
                a = floor(a);
                return frac(a.x / 2.0 + a.y * a.y * 0.75);
            }
            float Bayer4(float2 a) { return Bayer2(0.5 * a) * 0.25 + Bayer2(a); }
            float Bayer8(float2 a) { return Bayer4(0.5 * a) * 0.25 + Bayer2(a); }

            uint Pcg(uint v)
            {
                uint state = v * 747796405u + 2891336453u;
                uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
                return (word >> 22u) ^ word;
            }

            // Threshold 0..1 for this virtual pixel. Pixels with a low threshold switch to the next color first.
            float DitherThreshold(float2 cell)
            {
                int pattern = (int)_DitherPattern;
                float t = Bayer4(cell);
                if (pattern == 1) t = frac(52.9829189 * frac(dot(cell, float2(0.06711056, 0.00583715)))); // interleaved gradient noise
                else if (pattern == 3) t = Bayer8(cell);
                else if (pattern == 5) t = fmod(cell.x + cell.y, 2) * 0.5 + 0.25;
                else if (pattern == 6) t = Pcg((uint)cell.x + Pcg((uint)cell.y)) / 4294967295.0;
                else if (pattern == 7) t = Bayer2(cell);
                else if (pattern == 8) t = (fmod(cell.y, 4) + 0.5) / 4;
                else if (pattern == 9) t = (fmod(cell.x + cell.y, 4) + 0.5) / 4;
                else if (pattern == 10)
                {
                    float a = abs(fmod(cell.x + cell.y, 6) - 2.5) / 2.5;
                    float b = abs(fmod(cell.x - cell.y + 6000, 6) - 2.5) / 2.5;
                    t = min(a, b);
                }
                else if (pattern == 11) t = saturate(length(frac((cell + 0.5) / 6) - 0.5) * 1.414); // dots grow from their centers
                return t;
            }

            float3 Oklab(float3 c)
            {
                float l = pow(max(0, 0.4122214708 * c.r + 0.5363325363 * c.g + 0.0514459929 * c.b), 1.0 / 3.0);
                float m = pow(max(0, 0.2119034982 * c.r + 0.6806995451 * c.g + 0.1073969566 * c.b), 1.0 / 3.0);
                float s = pow(max(0, 0.0883024619 * c.r + 0.2817188376 * c.g + 0.6299787005 * c.b), 1.0 / 3.0);
                return float3(
                    0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
                    1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
                    0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s);
            }

            float3 OklabToLinear(float3 c)
            {
                float l = c.x + 0.3963377774 * c.y + 0.2158037573 * c.z;
                float m = c.x - 0.1055613458 * c.y - 0.0638541728 * c.z;
                float s = c.x - 0.0894841775 * c.y - 1.2914855480 * c.z;
                l = l * l * l; m = m * m * m; s = s * s * s;
                return float3(
                    4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
                    -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
                    -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s);
            }

            // Steps only the perceived lightness; chroma scales with it so the hue never shifts.
            float3 PosterizeLightness(float3 srgb, float steps, float offset)
            {
                float3 lab = Oklab(SRGBToLinear(srgb));
                float l = round(saturate(lab.x + offset / steps) * steps) / steps;
                lab.yz *= lab.x > 1e-4 ? l / lab.x : 0;
                lab.x = l;
                return saturate(OklabToLinear(lab));
            }

            // 1 at black fading to 0 at _ShadowRange, and 0 below 1 - _HighlightRange rising to 1 at white.
            float DitherMask(float luma)
            {
                float dark = _ShadowRange > 0 ? 1 - smoothstep(0, _ShadowRange, luma) : 0;
                float light = _HighlightRange > 0 ? smoothstep(1 - _HighlightRange, 1, luma) : 0;
                return max(dark, light) * _DitherStrength;
            }

            float3 Palette(float3 srgb, float threshold, float dither)
            {
                float3 p = Oklab(SRGBToLinear(srgb));
                int first = 0, second = 0;
                float firstDist = 1e9, secondDist = 1e9;
                for (int i = 0; i < MAX_COLORS; i++)
                {
                    if (i >= (int)_PaletteCount) break;
                    float3 d = p - _PaletteLab[i].xyz;
                    float dist = dot(d, d);
                    if (i >= (int)_KeepStart && dist > _KeepTolerance * _KeepTolerance) continue;
                    if (dist < firstDist)
                    {
                        second = first; secondDist = firstDist;
                        first = i; firstDist = dist;
                    }
                    else if (dist < secondDist)
                    {
                        second = i; secondDist = dist;
                    }
                }
                // How far the pixel sits from the closest color toward the second one (0..~0.5).
                float3 a = _PaletteLab[first].xyz, b = _PaletteLab[second].xyz;
                float3 ab = b - a;
                float t = saturate(dot(p - a, ab) / max(dot(ab, ab), 1e-6));
                int pick = threshold < t * dither ? second : first;
                return _PaletteRgb[pick].rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 screen = _ScreenParams.xy;
                float2 res = _PixelHeight > 0 ? float2(_PixelHeight * screen.x / screen.y, _PixelHeight) : screen;
                float2 cell = floor(uv * res);
                float2 cellUV = (cell + 0.5) / res;

                float3 c = LinearToSRGB(saturate(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, cellUV).rgb));
                c = (c - 0.5) * _Contrast + 0.5 + _Brightness;
                float luma = dot(c, float3(0.2126, 0.7152, 0.0722));
                c = saturate(lerp(luma.xxx, c, _Saturation));

                float threshold = DitherThreshold(cell);
                float dither = DitherMask(saturate(luma));

                if (_Mode > 0.5) return half4(Palette(c, threshold, dither), 1);

                if (_Levels >= 2)
                {
                    float steps = _Levels - 1;
                    float offset = (threshold - 0.5) * dither;
                    if (_PosterizeSpace < 0.5) return half4(PosterizeLightness(c, steps, offset), 1);
                    c += offset / steps;
                    c = round(saturate(c) * steps) / steps;
                }
                return half4(SRGBToLinear(saturate(c)), 1);
            }
            ENDHLSL
        }
    }
}
