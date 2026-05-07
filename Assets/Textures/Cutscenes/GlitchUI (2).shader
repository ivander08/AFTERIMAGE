Shader "Custom/GlitchUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // --- Glitch Controls ---
        _GlitchIntensity   ("Glitch Intensity",   Range(0, 1))   = 0.4
        _GlitchSpeed       ("Glitch Speed",        Range(0, 20))  = 6.0
        _BlockSize         ("Block Size",          Range(0.01, 0.3)) = 0.08
        _ShiftAmount       ("RGB Shift Amount",    Range(0, 0.05)) = 0.012
        _ScanlineIntensity ("Scanline Intensity",  Range(0, 1))   = 0.25
        _ScanlineDensity   ("Scanline Density",    Range(10, 300)) = 120.0
        _DigitalNoise      ("Digital Noise",       Range(0, 1))   = 0.08
        _VignetteStrength  ("Vignette Strength",   Range(0, 2))   = 0.6
        _ChromaAberration  ("Chroma Aberration",   Range(0, 0.03)) = 0.008

        // --- Ember/Ash Particles ---
        _EmberCount        ("Ember Count",         Range(0, 60))  = 30
        _EmberSpeed        ("Ember Rise Speed",    Range(0, 2))   = 0.18
        _EmberSize         ("Ember Size",          Range(0.001, 0.05)) = 0.012
        _EmberBrightness   ("Ember Brightness",    Range(0, 3))   = 1.8
        _EmberColor        ("Ember Color",         Color)         = (1.0, 0.75, 0.3, 1.0)

        // --- Required for UI ---
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline"    = "UniversalPipeline"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "GlitchUI"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // -------------------------------------------------------
            // Structs
            // -------------------------------------------------------
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
            };

            // -------------------------------------------------------
            // Uniforms
            // -------------------------------------------------------
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _GlitchIntensity;
                float  _GlitchSpeed;
                float  _BlockSize;
                float  _ShiftAmount;
                float  _ScanlineIntensity;
                float  _ScanlineDensity;
                float  _DigitalNoise;
                float  _VignetteStrength;
                float  _ChromaAberration;
                float  _EmberCount;
                float  _EmberSpeed;
                float  _EmberSize;
                float  _EmberBrightness;
                float4 _EmberColor;
            CBUFFER_END

            // -------------------------------------------------------
            // Helpers
            // -------------------------------------------------------

            // Cheap hash (no texture needed)
            float hash(float2 p)
            {
                p = frac(p * float2(443.897, 441.423));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float hash1(float n) { return frac(sin(n) * 43758.5453); }

            // Soft circular dot — used for each ember
            float emberDot(float2 uv, float2 center, float radius)
            {
                float d = length(uv - center);
                return saturate(1.0 - d / radius);
            }

            // Returns additive ember glow accumulated across N particles
            half3 embers(float2 uv, float t)
            {
                half3 result = 0;
                int count = (int)clamp(_EmberCount, 0, 60);

                for (int i = 0; i < count; i++)
                {
                    float fi = float(i);

                    // Each ember has a unique random seed
                    float rx = hash1(fi * 3.7);          // x spawn (0..1)
                    float ry = hash1(fi * 7.13 + 1.0);   // y spawn offset
                    float speed = hash1(fi * 2.91 + 2.0) * _EmberSpeed + _EmberSpeed * 0.5;
                    float drift = (hash1(fi * 5.3 + 3.0) - 0.5) * 0.08; // gentle horizontal sway
                    float phase = hash1(fi * 11.0 + 4.0); // stagger start time

                    // Loop: each ember cycles from bottom (1.0) upward (0.0)
                    float cycle = frac(phase + t * speed);
                    float py = cycle;                    // rises upward (1=bottom, 0=top in UV)
                    float px = rx + sin(cycle * 6.28 * 1.5 + fi) * drift;

                    // Fade in near bottom, fade out near top
                    float alpha = smoothstep(0.0, 0.15, cycle) * smoothstep(1.0, 0.7, cycle);

                    // Size slightly shrinks as it rises
                    float sz = _EmberSize * (0.5 + 0.5 * (1.0 - cycle));

                    float dot_ = emberDot(uv, float2(px, py), sz);

                    // Warm color variation per ember (orange → white-yellow)
                    float warmth = hash1(fi * 4.1 + 5.0);
                    half3 ec = lerp(_EmberColor.rgb, half3(1.0, 1.0, 0.9), warmth * 0.4);

                    result += ec * dot_ * alpha * _EmberBrightness;
                }

                return result;
            }

            // Stepped time — creates "freeze-frame" glitch rhythm
            float steppedTime(float speed, float steps)
            {
                float t = _Time.y * speed;
                return floor(t * steps) / steps;
            }

            // -------------------------------------------------------
            // Vert
            // -------------------------------------------------------
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color * _Color;
                return OUT;
            }

            // -------------------------------------------------------
            // Frag
            // -------------------------------------------------------
            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float  t  = _Time.y * _GlitchSpeed;

                // ── 1. Block-level horizontal RGB shift ──────────────
                // Divide screen into horizontal bands; each band may shift
                float bandT       = steppedTime(_GlitchSpeed, 8.0);
                float band        = floor(uv.y / _BlockSize);
                float bandRand    = hash(float2(band, bandT));
                float glitchOn    = step(1.0 - _GlitchIntensity, bandRand);
                float shift       = (bandRand * 2.0 - 1.0) * _ShiftAmount * glitchOn;

                // ── 2. Chromatic aberration (always-on, subtle) ───────
                float chromaShift = _ChromaAberration;

                float2 uvR = float2(uv.x + shift + chromaShift, uv.y);
                float2 uvG = float2(uv.x + shift,               uv.y);
                float2 uvB = float2(uv.x + shift - chromaShift, uv.y);

                half4 colR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvR);
                half4 colG = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvG);
                half4 colB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvB);

                half4 col = half4(colR.r, colG.g, colB.b, colG.a);

                // ── 3. Scanlines ──────────────────────────────────────
                float scanline = sin(uv.y * _ScanlineDensity * 3.14159265) * 0.5 + 0.5;
                // Animate them slightly for CRT feel
                float scanAnim  = sin(uv.y * _ScanlineDensity * 3.14159265 + t * 2.0) * 0.5 + 0.5;
                scanline = lerp(scanline, scanAnim, 0.3);
                col.rgb *= 1.0 - _ScanlineIntensity * (1.0 - scanline);

                // ── 4. Digital pixel noise ────────────────────────────
                float noiseT    = floor(t * 12.0); // flicker at ~12fps
                float noise     = hash(uv * float2(200.0, 100.0) + noiseT);
                // Only fire noise on random pixels
                float noiseMask = step(1.0 - _DigitalNoise, noise);
                // Noise color: cyan or white flash (cyberpunk)
                half3 noiseCol  = lerp(half3(0.0, 1.0, 1.0), half3(1.0, 1.0, 1.0),
                                       hash1(noiseT + 7.3));
                col.rgb = lerp(col.rgb, noiseCol, noiseMask * 0.6);

                // ── 5. Occasional full-line bright flash ──────────────
                float flashT    = steppedTime(_GlitchSpeed, 4.0);
                float flashBand = floor(uv.y / (_BlockSize * 0.5));
                float flashR    = hash(float2(flashBand, flashT + 99.0));
                float flashOn   = step(0.97, flashR) * step(1.0 - _GlitchIntensity * 0.5, flashR);
                // Cyan tint for the flash line
                col.rgb += half3(0.0, 0.9, 1.0) * flashOn * 0.5;

                // ── 6. Vignette ───────────────────────────────────────
                float2 vigUV  = uv * 2.0 - 1.0;
                float  vig    = 1.0 - dot(vigUV, vigUV) * _VignetteStrength * 0.4;
                col.rgb      *= saturate(vig);

                // ── 7. Cyan/teal color grade (cyberpunk mood) ─────────
                // Slightly push shadows toward teal, highlights toward white
                half3 teal   = half3(0.0, 0.9, 0.85);
                float luma   = dot(col.rgb, half3(0.299, 0.587, 0.114));
                col.rgb      = lerp(col.rgb, col.rgb * (1.0 + teal * 0.15), 0.3 * (1.0 - luma));

                // ── 8. Ember / ash particles (additive, rises upward) ─
                float emberT = _Time.y; // unscaled so speed prop is self-contained
                half3 emberGlow = embers(uv, emberT);
                col.rgb += emberGlow;

                // ── 9. Tint & alpha from UI vertex color ──────────────
                col *= IN.color;

                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
