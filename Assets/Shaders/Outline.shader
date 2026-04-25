Shader "Custom/URP_Outline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.5, 0, 1)
        _OutlineWidth ("Outline Width", Float) = 0.05
        _OutlineEmission ("Emission Intensity", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry-1" }

        Pass
        {
            Name "Outline"
            
            Cull Front   // <-- Key: render back faces only
            ZWrite On
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _OutlineEmission;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Push vertices outward along normals in clip space
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                
                float3 normalCS = TransformWorldToHClipDir(normalWS);
                
                // Scale outline by distance so it stays consistent
                float2 offset = normalize(normalCS.xy);
                offset *= _OutlineWidth * positionCS.w * 0.01;
                positionCS.xy += offset;

                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(_OutlineColor.rgb * _OutlineEmission, 1.0);
            }
            ENDHLSL
        }
    }
}