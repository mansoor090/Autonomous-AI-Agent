Shader "Animmal (URP)/Ice"
{
    Properties
    {
        _Specularity("Specularity", Range( 0 , 5)) = 0.2041201
        _Tint("Tint", Color) = (0.2867647,0.704868,1,0)
        _Glow("Glow Emission", Range(0, 5)) = 1
        _TextureSample0("Texture Sample 0", 2D) = "white" {}
        _TextureSample1("Texture Sample 1", 2D) = "bump" {}
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

            #define _NORMALMAP 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            float4 _Tint;
            float _Glow;
            sampler2D _TextureSample0;
            float4 _TextureSample0_ST;
            sampler2D _TextureSample1;
            float4 _TextureSample1_ST;
            float _Specularity;

            struct GraphVertexInput
            {
                float4 vertex : POSITION;
                float3 ase_normal : NORMAL;
                float4 ase_tangent : TANGENT;
                float4 texcoord1 : TEXCOORD1;
                float4 ase_texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct GraphVertexOutput
            {
                float4 clipPos : SV_POSITION;
                float4 lightmapUVOrVertexSH : TEXCOORD0;
                half4 fogFactorAndVertexLight : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
                float4 tSpace0 : TEXCOORD3;
                float4 tSpace1 : TEXCOORD4;
                float4 tSpace2 : TEXCOORD5;
                float4 ase_texcoord7 : TEXCOORD7;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            GraphVertexOutput vert (GraphVertexInput v)
            {
                GraphVertexOutput o = (GraphVertexOutput)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.ase_texcoord7.xy = v.ase_texcoord.xy;
                o.ase_texcoord7.zw = 0;

                float3 lwWNormal = TransformObjectToWorldNormal(v.ase_normal);
                float3 lwWorldPos = TransformObjectToWorld(v.vertex.xyz);
                float3 lwWTangent = TransformObjectToWorldDir(v.ase_tangent.xyz);
                float3 lwWBinormal = normalize(cross(lwWNormal, lwWTangent) * v.ase_tangent.w);

                o.tSpace0 = float4(lwWTangent.x, lwWBinormal.x, lwWNormal.x, lwWorldPos.x);
                o.tSpace1 = float4(lwWTangent.y, lwWBinormal.y, lwWNormal.y, lwWorldPos.y);
                o.tSpace2 = float4(lwWTangent.z, lwWBinormal.z, lwWNormal.z, lwWorldPos.z);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);

                OUTPUT_LIGHTMAP_UV(v.texcoord1, unity_LightmapST, o.lightmapUVOrVertexSH.xy);
                OUTPUT_SH(lwWNormal, o.lightmapUVOrVertexSH.xyz);

                half3 vertexLight = VertexLighting(vertexInput.positionWS, lwWNormal);
                half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
                o.clipPos = vertexInput.positionCS;

                #ifdef _MAIN_LIGHT_SHADOWS
                o.shadowCoord = GetShadowCoord(vertexInput);
                #endif

                return o;
            }

            half4 frag (GraphVertexOutput IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 WorldSpaceNormal = normalize(float3(IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z));
                float3 WorldSpaceTangent = float3(IN.tSpace0.x, IN.tSpace1.x, IN.tSpace2.x);
                float3 WorldSpaceBiTangent = float3(IN.tSpace0.y, IN.tSpace1.y, IN.tSpace2.y);
                float3 WorldSpacePosition = float3(IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w);
                float3 WorldSpaceViewDirection = SafeNormalize(_WorldSpaceCameraPos.xyz - WorldSpacePosition);

                float2 uv_TextureSample0 = IN.ase_texcoord7.xy * _TextureSample0_ST.xy + _TextureSample0_ST.zw;
                float2 uv_TextureSample1 = IN.ase_texcoord7.xy * _TextureSample1_ST.xy + _TextureSample1_ST.zw;

                float3 Albedo = (_Tint * tex2D(_TextureSample0, uv_TextureSample0)).rgb;
                float3 Normal = UnpackNormalScale(tex2D(_TextureSample1, uv_TextureSample1), 1.0f);
                float3 Emission = Albedo * _Glow;
                float3 Specular = float3(0.5, 0.5, 0.5);
                float Metallic = 0;
                float Smoothness = _Specularity;
                float Occlusion = 1;
                float Alpha = 1;
                float AlphaClipThreshold = 0;

                InputData inputData;
                inputData.positionWS = WorldSpacePosition;
                inputData.normalWS = normalize(TransformTangentToWorld(Normal, half3x3(WorldSpaceTangent, WorldSpaceBiTangent, WorldSpaceNormal)));
                inputData.viewDirectionWS = normalize(WorldSpaceViewDirection);
                inputData.shadowCoord = IN.shadowCoord;
                inputData.fogCoord = IN.fogFactorAndVertexLight.x;
                inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
                inputData.bakedGI = SAMPLE_GI(IN.lightmapUVOrVertexSH.xy, IN.lightmapUVOrVertexSH.xyz, inputData.normalWS);

                half4 color = UniversalFragmentPBR(
                    inputData,
                    Albedo,
                    Metallic,
                    Specular,
                    Smoothness,
                    Occlusion,
                    Emission,
                    Alpha);

                color.rgb = MixFog(color.rgb, IN.fogFactorAndVertexLight.x);

                #ifdef _ALPHATEST_ON
                clip(Alpha - AlphaClipThreshold);
                #endif

                #if ASE_LW_FINAL_COLOR_ALPHA_MULTIPLY
                color.rgb *= color.a;
                #endif

                return color;
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/InternalErrorShader"
    CustomEditor "ASEMaterialInspector"
}
