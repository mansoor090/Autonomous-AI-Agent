// Enhanced Surface Shader for URP (Lighting Reactivity + Emission + Functional Specular)
Shader "Animmal (URP)/Surface"
{
    Properties
    {
        _Albedo("Albedo", 2D) = "white" {}
        _Specular("Specular", 2D) = "white" {}
        _Specular_Intensity("Specular_Intensity", Range(0, 3)) = 0
        _Normal("Normal", 2D) = "bump" {}
        _EmissionStrength("Emission Strength", Range(0, 2)) = 1
        [HideInInspector] _texcoord("", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero
            ZWrite On
            ZTest LEqual
            ColorMask RGBA

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #define _NORMALMAP 1

        	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
        	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            sampler2D _Albedo;
            float4 _Albedo_ST;
            sampler2D _Normal;
            float4 _Normal_ST;
            sampler2D _Specular;
            float4 _Specular_ST;
            float _Specular_Intensity;
            float _EmissionStrength;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float3 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 shadowCoord : TEXCOORD6;
                float fogFactor : TEXCOORD7;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionHCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = normInputs.tangentWS;
                output.bitangentWS = normInputs.bitangentWS;
                output.uv = TRANSFORM_TEX(input.uv, _Albedo);
                output.uv1 = input.uv1;
                output.shadowCoord = GetShadowCoord(posInputs);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 normalTS = UnpackNormalScale(tex2D(_Normal, input.uv), 1.0);
                float3x3 TBN = float3x3(normalize(input.tangentWS), normalize(input.bitangentWS), normalize(input.normalWS));
                float3 normalWS = normalize(mul(normalTS, TBN));

                float3 albedo = max(tex2D(_Albedo, input.uv).rgb, 0.01);
                float smoothness = tex2D(_Specular, input.uv).r;
                float3 specColor = tex2D(_Specular, input.uv).rgb * _Specular_Intensity;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = VertexLighting(input.positionWS, normalWS);
                inputData.bakedGI = SampleSH(normalWS);

                float metallic = 0.0;
                float occlusion = 1.0;
                float alpha = 1.0;
                float3 emission = albedo * _EmissionStrength;

                half4 color = UniversalFragmentPBR(
                    inputData,
                    albedo,
                    metallic,
                    specColor,
                    smoothness,
                    occlusion,
                    emission,
                    alpha
                );

                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/InternalErrorShader"
    CustomEditor "ASEMaterialInspector"
}
