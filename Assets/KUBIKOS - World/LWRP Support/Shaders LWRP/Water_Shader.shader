// Enhanced Animmal (URP)/Water Shader with advanced lighting features
Shader "Animmal (URP)/Water"
{
    Properties
    {
        _TopTexture0("Top Texture 0", 2D) = "white" {}
        _TextureSample6("Texture Sample 6", 2D) = "white" {}
        _Cubes("Cubes", 2D) = "white" {}
        _Glossiness("Glossiness", Range(0, 1)) = 0.5
        _Metallic("Metallic", Range(0, 1)) = 0.0
        _EmissionColor("Emission Color", Color) = (0, 0, 0, 0)
        _RimPower("Rim Power", Range(1, 10)) = 3
        _RimColor("Rim Color", Color) = (0.5, 0.5, 1.0, 1.0)
        [HideInInspector] _texcoord("", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "Base"
            Tags { "LightMode"="UniversalForward" }

            Blend One Zero
            ZWrite On
            ZTest LEqual
            Offset 0 , 0
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

            #define ASE_TEXTURE_PARAMS(textureName) textureName

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            sampler2D _TopTexture0;
            sampler2D _TextureSample6;
            sampler2D _Cubes;
            float4 _Cubes_ST;
            half _Glossiness;
            half _Metallic;
            half4 _EmissionColor;
            float _RimPower;
            float4 _RimColor;

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

            inline float4 TriplanarSamplingSF(sampler2D topTexMap, float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale, float3 index)
            {
                float3 projNormal = pow(abs(worldNormal), falloff);
                projNormal /= (projNormal.x + projNormal.y + projNormal.z + 1e-5);
                float3 nsign = sign(worldNormal);
                half4 xNorm = tex2D(topTexMap, tiling * worldPos.zy * float2(nsign.x, 1.0));
                half4 yNorm = tex2D(topTexMap, tiling * worldPos.xz * float2(nsign.y, 1.0));
                half4 zNorm = tex2D(topTexMap, tiling * worldPos.xy * float2(-nsign.z, 1.0));
                return xNorm * projNormal.x + yNorm * projNormal.y + zNorm * projNormal.z;
            }

            GraphVertexOutput vert(GraphVertexInput v)
            {
                GraphVertexOutput o = (GraphVertexOutput)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.ase_texcoord7.xy = v.ase_texcoord.xy;
                o.ase_texcoord7.zw = 0;

                float3 worldNormal = TransformObjectToWorldNormal(v.ase_normal);
                float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                float3 worldTangent = TransformObjectToWorldDir(v.ase_tangent.xyz);
                float3 worldBinormal = normalize(cross(worldNormal, worldTangent) * v.ase_tangent.w);

                o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
                o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
                o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                OUTPUT_LIGHTMAP_UV(v.texcoord1, unity_LightmapST, o.lightmapUVOrVertexSH.xy);
                OUTPUT_SH(worldNormal, o.lightmapUVOrVertexSH.xyz);
                half3 vertexLight = VertexLighting(vertexInput.positionWS, worldNormal);
                half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
                o.clipPos = vertexInput.positionCS;
                o.shadowCoord = GetShadowCoord(vertexInput);
                return o;
            }

            half4 frag(GraphVertexOutput IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 worldNormal = normalize(float3(IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z));
                float3 worldTangent = float3(IN.tSpace0.x, IN.tSpace1.x, IN.tSpace2.x);
                float3 worldBinormal = float3(IN.tSpace0.y, IN.tSpace1.y, IN.tSpace2.y);
                float3 worldPos = float3(IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w);
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - worldPos);

                float4 triplanarColor = TriplanarSamplingSF(_TopTexture0, worldPos, worldNormal, 0.5, 0.5, 1.0, 0);
                float2 uv = IN.ase_texcoord7.xy + (_Time.x + 10.0).xx;
                float2 panner = uv;
                float2 uvCubes = IN.ase_texcoord7.xy * _Cubes_ST.xy + _Cubes_ST.zw;

                float3 albedo = triplanarColor.rgb * (tex2D(_TextureSample6, panner) + tex2D(_Cubes, uvCubes)).rgb;

                float rim = 1.0 - saturate(dot(viewDir, worldNormal));
                float3 rimLight = _RimColor.rgb * pow(rim, _RimPower);

                InputData inputData = (InputData)0;
                inputData.positionWS = worldPos;
                inputData.normalWS = worldNormal;
                inputData.viewDirectionWS = viewDir;
                inputData.shadowCoord = IN.shadowCoord;
                inputData.fogCoord = IN.fogFactorAndVertexLight.x;
                inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
                inputData.bakedGI = SAMPLE_GI(IN.lightmapUVOrVertexSH.xy, IN.lightmapUVOrVertexSH.xyz, worldNormal);

                half4 color = UniversalFragmentPBR(
                    inputData,
                    albedo,
                    _Metallic,
                    float3(0.5, 0.5, 0.5),
                    _Glossiness,
                    1.0,
                    _EmissionColor.rgb + rimLight,
                    1.0
                );

                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/InternalErrorShader"
    CustomEditor "ASEMaterialInspector"
}
