Shader "Custom/ComplexityVisualization"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Blend Off
        Cull Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _max_complexity;

            static const float3 _unknown_color = float3(0.171, 0.522, 1.000);

            //Color from https://github.com/EpicGames/UnrealEngine/blob/f45729c0b6f097038e72b52d366a62baeca3275b/Engine/Config/BaseEngine.ini#L2709
            static const float3 _ramp[9] =
            {
                float3(0.000, 1.000, 0.127),
                float3(0.000, 1.000, 0.000),
                float3(0.046, 0.520, 0.000),
                float3(0.215, 0.215, 0.000),
                float3(0.520, 0.046, 0.000),
                float3(0.700, 0.000, 0.000),
                float3(1.000, 0.000, 0.000),
                float3(1.000, 0.000, 0.500),
                float3(1.000, 0.900, 0.900)
            };

            float3 SampleRamp(float t)
            {
                t = saturate(t);

                float scaled = t * 8.0;
                int index = min((int)floor(scaled), 7);
                float f = scaled - (float)index;
                return lerp(_ramp[index], _ramp[index + 1], f);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 complexity = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);

                if (complexity.b <= 0.0)
                    return float4(0.0, 0.0, 0.0, 1.0);

                if (complexity.a > 0.0)
                    return float4(_unknown_color, 1.0);

                float3 color = SampleRamp(complexity.r / max(_max_complexity, 1.0));
                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
