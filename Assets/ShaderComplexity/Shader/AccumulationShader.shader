Shader "Custom/AccumulationShader"
{
    Properties
    {
        complexity ("Complexity (frag, vert, layer, unknown)", Vector) = (0, 0, -1, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend One One
            BlendOp Add

            ZWrite Off
            ZTest LEqual

            Cull Back

            ColorMask RGBA

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 complexity;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return complexity;
            }
            ENDHLSL
        }
    }
}
