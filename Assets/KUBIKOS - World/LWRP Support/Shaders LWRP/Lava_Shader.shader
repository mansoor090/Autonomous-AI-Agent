Shader "Animmal (URP)/Lava"
{
    Properties
    {
        _TopTexture0("Top Texture 0", 2D) = "white" {}
        _TextureSample6("Texture Sample 6", 2D) = "white" {}
        _Contrast("Contrast", Range( 0.1 , 2)) = 0.1
        _Intensity("Intensity", Range( 0 , 2)) = 0
        _Float0("Float 0", Range( 0 , 2)) = 0
        _Cubes("Cubes", 2D) = "white" {}
        [HideInInspector] _texcoord("", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend One Zero
            ZWrite On
            ZTest LEqual
            ColorMask RGBA

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
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
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
        	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            float _Float0;
            sampler2D _TopTexture0;
            float _Contrast;
            sampler2D _TextureSample6;
            float _Intensity;
            sampler2D _Cubes;
            float4 _Cubes_ST;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float2 uv2        : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float3 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
                float4 shadowCoord : TEXCOORD6;
                half fogFactor : TEXCOORD7;
                half3 vertexLighting : TEXCOORD8;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 CalculateContrast(float contrastValue, float4 colorTarget)
            {
                float t = 0.5 * (1.0 - contrastValue);
                return mul(float4x4(
                    contrastValue, 0, 0, t,
                    0, contrastValue, 0, t,
                    0, 0, contrastValue, t,
                    0, 0, 0, 1), colorTarget);
            }

            float4 TriplanarSampling(sampler2D tex, float3 worldPos, float3 normal, float falloff, float2 tiling)
            {
                float3 blend = pow(abs(normal), falloff);
                blend /= dot(blend, 1.0);
                float3 signNormal = sign(normal);
                float4 xTex = tex2D(tex, worldPos.zy * tiling * float2(signNormal.x, 1.0));
                float4 yTex = tex2D(tex, worldPos.xz * tiling * float2(signNormal.y, 1.0));
                float4 zTex = tex2D(tex, worldPos.xy * tiling * float2(-signNormal.z, 1.0));
                return xTex * blend.x + yTex * blend.y + zTex * blend.z;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                float3 bitangentWS = cross(normalWS, tangentWS) * IN.tangentOS.w;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;
                OUT.worldPos = positionWS;
                OUT.normalWS = normalWS;
                OUT.viewDirWS = _WorldSpaceCameraPos - positionWS;
                OUT.tangentWS = tangentWS;
                OUT.bitangentWS = bitangentWS;
                OUT.uv = IN.uv;
                OUT.shadowCoord = GetShadowCoord(posInputs);
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                OUT.vertexLighting = VertexLighting(positionWS, normalWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 normalWS = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);
                float3 worldPos = IN.worldPos;

                float4 triplanar = TriplanarSampling(_TopTexture0, worldPos, normalWS, 1.0, 2.0);
                float2 uvOffset = IN.uv + (_Time.x + 10.0);
                float2 uvCubes = IN.uv * _Cubes_ST.xy + _Cubes_ST.zw;

                float3 emission = ((CalculateContrast(_Float0, triplanar * (_Contrast * (_SinTime.w + 3.0))) * tex2D(_TextureSample6, uvOffset)) * (_Intensity * (_SinTime.w + 3.0)) + tex2D(_Cubes, uvCubes)).rgb;

                InputData inputData;
                inputData.positionWS = worldPos;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDir;
                inputData.shadowCoord = IN.shadowCoord;
                inputData.fogCoord = IN.fogFactor;
                inputData.vertexLighting = IN.vertexLighting;
                inputData.bakedGI = SAMPLE_GI(IN.uv, 0, normalWS);

                float3 albedo = float3(0.5, 0.5, 0.5);
                float3 specular = float3(0.5, 0.5, 0.5);
                float smoothness = 0.5;
                float metallic = 0.0;
                float occlusion = 1.0;
                float alpha = 1.0;

                half4 color = UniversalFragmentPBR(inputData, albedo, metallic, specular, smoothness, occlusion, emission, alpha);
                color.rgb = MixFog(color.rgb, IN.fogFactor);

            #ifdef _ALPHATEST_ON
                clip(alpha - 0.5);
            #endif

                return color;
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/InternalErrorShader"
    CustomEditor "ASEMaterialInspector"
} 