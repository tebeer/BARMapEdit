// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/Terrain"
{
    Properties
    {
        _Map("Map", 2D) = "white" {}
        _Detail("Detail", 2D) = "gray" {}
        _Normal("Normal", 2D) = "bump" {}
        _Specular("Specular", 2D) = "black" {}
        _SplatDistr("SplatDistr", 2D) = "white" {}
        _SplatDetailTex("SplatDetailTex", 2D) = "white" {}
        _SplatDetailNormal1("SplatDetailNormal1", 2D) = "bump" {}
        _SplatDetailNormal2("SplatDetailNormal2", 2D) = "bump" {}
        _SplatDetailNormal3("SplatDetailNormal3", 2D) = "bump" {}
        _SplatDetailNormal4("SplatDetailNormal4", 2D) = "bump" {}
        _ChunksX("ChunksX", Float) = 1
        _ChunksY("ChunksY", Float) = 1
        _SplatScales("Splat scales", Vector) = (1, 1, 1, 1)
        _SplatMults("Splat mults", Vector) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #pragma shader_feature NORMAL_TEXTURE
            #pragma shader_feature SPLAT_NORMAL

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 position                 : SV_POSITION;
                float2 diffCoords : TEXCOORD0;
                float2 normalCoords : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float fogCoord : TEXCOORD3;
#if !NORMAL_TEXTURE
                float3 normal : TEXCOORD6;
#endif
            };
             
            CBUFFER_START(UnityPerMaterial)
            sampler2D _Map;
            sampler2D _Detail;
            sampler2D _Normal;
            sampler2D _Specular;
            sampler2D _SplatDistr;
            sampler2D _SplatDetailTex;
            sampler2D _SplatDetailNormal1;
            sampler2D _SplatDetailNormal2;
            sampler2D _SplatDetailNormal3;
            sampler2D _SplatDetailNormal4;

            float _ChunksX;
            float _ChunksY;
            half4 _GroundDiffuseColor;

            float4 _Detail_TexelSize;

            float4 _SplatScales;
            float4 _SplatMults;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.position = TransformObjectToHClip(v.position.xyz);
                o.worldPos = TransformObjectToWorld(v.position.xyz);
                o.diffCoords.x = (_ChunksX * v.uv.x);
                o.diffCoords.y = (_ChunksY * v.uv.y);
                o.normalCoords.x = v.uv.x;
                o.normalCoords.y = 1 - v.uv.y;
#if !NORMAL_TEXTURE
                o.normal = TransformObjectToWorldNormal(v.normal);
#endif
                o.fogCoord = ComputeFogFactor(o.position.z);

                return o;
            }

            half4 GetDetailTextureColor(float3 vertexPos, float2 uv)
            {
//#ifndef SMF_DETAIL_TEXTURE_SPLATTING
//                float2 detailTexCoord = vertexPos.xz * _Detail_TexelSize.xy;
//                fixed4 detailCol = (tex2D(_Detail, detailTexCoord) * 2.0) - 1.0;
//#else
                float4 splatTexCoord0 = vertexPos.xzxz * _SplatScales.rrgg;
                float4 splatTexCoord1 = vertexPos.xzxz * _SplatScales.bbaa;
                float4 splatDetails;
                splatDetails.r = tex2D(_SplatDetailTex, splatTexCoord0.xy).r;
                splatDetails.g = tex2D(_SplatDetailTex, splatTexCoord0.zw).g;
                splatDetails.b = tex2D(_SplatDetailTex, splatTexCoord1.xy).b;
                splatDetails.a = tex2D(_SplatDetailTex, splatTexCoord1.zw).a;
                splatDetails = (splatDetails * 2.0) - 1.0;
                float4 splatDist = tex2D(_SplatDistr, uv) * _SplatMults;
                float4 detailCol = dot(splatDetails, splatDist);
//#endif
                return detailCol;
            }

#if SPLAT_NORMAL
            float4 GetSplatDetailTextureNormal(float3 vertexPos, float2 uv, out float2 splatDetailStrength)
            {
                float4 splatTexCoord0 = vertexPos.xzxz * _SplatScales.rrgg;
                float4 splatTexCoord1 = vertexPos.xzxz * _SplatScales.bbaa;
                float4 splatDist = tex2D(_SplatDistr, uv) * _SplatMults;

                splatDetailStrength.x = min(1.0, dot(splatDist, 1.0));

                float4 splatDetailNormal;
                splatDetailNormal = ((tex2D(_SplatDetailNormal1, splatTexCoord0.xy) * 2.0 - 1.0) * splatDist.r);
                splatDetailNormal += ((tex2D(_SplatDetailNormal2, splatTexCoord0.zw) * 2.0 - 1.0) * splatDist.g);
                splatDetailNormal += ((tex2D(_SplatDetailNormal3, splatTexCoord1.xy) * 2.0 - 1.0) * splatDist.b);
                splatDetailNormal += ((tex2D(_SplatDetailNormal4, splatTexCoord1.zw) * 2.0 - 1.0) * splatDist.a);

                // note: y=0.01 (pointing up) in case all splat-cofacs are zero
                splatDetailNormal.y = max(splatDetailNormal.y, 0.01);

                splatDetailStrength.y = clamp(splatDetailNormal.a, -1.0, 1.0);

                //splatDetailNormal.x = -splatDetailNormal.x;

                return splatDetailNormal;
            }
#endif

#define SMF_INTENSITY_MULT (210.0 / 256.0) + (1.0 / 256.0) - (1.0 / 2048.0) - (1.0 / 4096.0)

            float4 GetShadeInt(float groundLightInt, float groundShadowCoeff, float groundDiffuseAlpha)
            {
                float4 groundShadeInt = float4(0.0, 0.0, 0.0, 1.0);

                groundShadeInt.rgb = unity_AmbientGround.rgb + _GroundDiffuseColor.rgb * (groundLightInt * groundShadowCoeff);
                groundShadeInt.rgb *= SMF_INTENSITY_MULT;
//
//#ifdef SMF_VOID_WATER
//                // cut out all underwater fragments indiscriminately
//                groundShadeInt.a = float(vertexPos.y >= 0.0);
//#endif
//
//#ifdef SMF_VOID_GROUND
//                // assume the map(per)'s diffuse texture provides sensible alphas
//                // note that voidground overrides voidwater if *both* are enabled
//                // (limiting it to just above-water fragments would be arbitrary)
//                groundShadeInt.a = groundDiffuseAlpha;
//#endif
//
//#ifdef SMF_WATER_ABSORPTION
//                // use groundShadeInt alpha value; allows voidground maps to create
//                // holes in the seabed (SMF_WATER_ABSORPTION == 1 implies voidwater
//                // is not enabled but says nothing about the voidground state)
//                vec4 rawWaterShadeInt = vec4(waterBaseColor.rgb, groundShadeInt.a);
//                vec4 modWaterShadeInt = rawWaterShadeInt;
//
//                { //if (mapHeights.x <= 0.0) {
//                    float waterShadeAlpha = abs(vertexPos.y) * SMF_SHALLOW_WATER_DEPTH_INV;
//                    float waterShadeDecay = 0.2 + (waterShadeAlpha * 0.1);
//                    float vertexStepHeight = min(1023.0, -vertexPos.y);
//                    float waterLightInt = min(groundLightInt * 2.0 + 0.4, 1.0);
//
//                    // vertex below shallow water depth --> alpha=1
//                    // vertex above shallow water depth --> alpha=waterShadeAlpha
//                    waterShadeAlpha = min(1.0, waterShadeAlpha + float(vertexPos.y <= -SMF_SHALLOW_WATER_DEPTH));
//
//                    modWaterShadeInt.rgb -= (waterAbsorbColor.rgb * vertexStepHeight);
//                    modWaterShadeInt.rgb = max(waterMinColor.rgb, modWaterShadeInt.rgb);
//                    modWaterShadeInt.rgb *= vec3(SMF_INTENSITY_MULT * waterLightInt);
//
//                    // make shadowed areas darker over deeper water
//                    modWaterShadeInt.rgb *= (1.0 - waterShadeDecay * (1.0 - groundShadowCoeff));
//
//                    // if depth is greater than _SHALLOW_ depth, select waterShadeInt
//                    // otherwise interpolate between groundShadeInt and waterShadeInt
//                    // (both are already cosine-weighted)
//                    modWaterShadeInt.rgb = mix(groundShadeInt.rgb, modWaterShadeInt.rgb, waterShadeAlpha);
//                }
//
//                modWaterShadeInt = mix(rawWaterShadeInt, modWaterShadeInt, float(mapHeights.x <= 0.0));
//                groundShadeInt = mix(groundShadeInt, modWaterShadeInt, float(vertexPos.y < 0.0));
//#endif
//
                return groundShadeInt;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 detailColor;

                float3 normal;
#if NORMAL_TEXTURE
                normal = normalize(tex2D(_Normal, i.normalCoords).rbg * 2 - 1);
                //normal = tex2D(_Normal, i.normalCoords).rbg * 2 - 1;
                //normal.y = sqrt(1.0 - dot(normal.xz, normal.xz));
                //normal.xz = 2 * (tex2D(_Normal, i.normalCoords).rg - .5);
                //normal.y = sqrt(1.0 - dot(normal.xz, normal.xz));
                //normal.xyz = tex2D(_Normal, i.normalCoords).rbg;
                //normal = normalize(normal);
#else
                normal = i.normal;
#endif

#if SPLAT_NORMAL
                float2 splatDetailStrength = float2(0.0, 0.0);

                float4 splatDetailNormal = GetSplatDetailTextureNormal(i.worldPos, i.normalCoords, splatDetailStrength);
                splatDetailNormal.x = -splatDetailNormal.x;

                detailColor = splatDetailStrength.y;

                float3 tTangent = normalize(cross(normal, float3(1.0, 0.0, 0.0)));
                float3 sTangent = cross(normal, tTangent);
                float3x3 stnMatrix = float3x3(sTangent, tTangent, normal);
                stnMatrix = transpose(stnMatrix);

                normal = normalize(lerp(normal, normalize(mul(stnMatrix, splatDetailNormal.xyz)), splatDetailStrength.x));

#else
                detailColor = GetDetailTextureColor(i.worldPos, i.normalCoords);
#endif

                half4 diffuseColor = tex2D(_Map, i.diffCoords);

                Light mainLight = GetMainLight();

                float cosAngleDiffuse = saturate(dot(mainLight.direction, normal));

                float3 viewDir = normalize(i.worldPos - _WorldSpaceCameraPos.xyz);
                float3 halfDir = normalize(mainLight.direction - viewDir);
                float cosAngleSpecular = clamp(dot(halfDir, normal), 0.001, 1.0);

                float shadowCoeff = 1;
#ifdef _MAIN_LIGHT_SHADOWS
                shadowCoeff = MainLightRealtimeShadow(TransformWorldToShadowCoord(i.worldPos));
#endif

                float4 shadeInt = GetShadeInt(cosAngleDiffuse, shadowCoeff, diffuseColor.a);

                float4 fragColor;
                fragColor.rgb = (diffuseColor.rgb + detailColor.rgb) * shadeInt.rgb;
                fragColor.a = shadeInt.a;

                half4 specularColor = tex2D(_Specular, i.normalCoords);
                float specularExp = specularColor.a * 16.0;
                float specularPow = max(0.0, pow(cosAngleSpecular, specularExp));
                fragColor.rgb += (specularColor.rgb * specularPow * shadowCoeff);

                //UNITY_APPLY_FOG(i.fogCoord, fragColor);
                return fragColor;
            }

            ENDHLSL
        }

        Pass
        {
            Tags{ "LightMode" = "ShadowCaster" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformWorldToHClip(v.vertex.xyz);
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                return float4(0.0, 0.0, 0.0, 1);
            }
            ENDHLSL

        }
    }
}
