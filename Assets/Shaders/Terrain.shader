// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/Terrain"
{
    Properties
    {
        _Map("Map", 2D) = "white" {}
        _Detail("Detail", 2D) = "white" {}
        _Normal("Normal", 2D) = "bump" {}
        _Specular("Specular", 2D) = "black" {}
        _SplatDistr("SplatDistr", 2D) = "white" {}
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
        Tags { "RenderType" = "Opaque"  "LightMode" = "ForwardBase" }
        LOD 100

        Lighting On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase
            #pragma shader_feature NORMAL_TEXTURE

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 diffCoords : TEXCOORD0;
                float2 normalCoords : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                UNITY_FOG_COORDS(3)
                LIGHTING_COORDS(4, 5)
#if !NORMAL_TEXTURE
                    float3 normal : TEXCOORD6;
#endif
            };

            sampler2D _Map;
            sampler2D _Detail;
            sampler2D _Normal;
            sampler2D _Specular;
            sampler2D _SplatDistr;
            sampler2D _SplatDetailNormal1;
            sampler2D _SplatDetailNormal2;
            sampler2D _SplatDetailNormal3;
            sampler2D _SplatDetailNormal4;

            float _ChunksX;
            float _ChunksY;
            fixed4 _GroundDiffuseColor;

            float4 _Detail_TexelSize;

            float4 _SplatScales;
            float4 _SplatMults;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.diffCoords.x = (_ChunksX * v.uv.x);
                o.diffCoords.y = (_ChunksY * v.uv.y);
                o.normalCoords = v.uv;
                o.normalCoords.y = 1 - v.uv.y;
#if !NORMAL_TEXTURE
                o.normal = UnityObjectToWorldNormal(v.normal);
#endif
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);

                UNITY_TRANSFER_FOG(o,o.pos);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 GetDetailTextureColor(float3 vertexPos)
            {
                float2 detailTexCoord = vertexPos.xz * _Detail_TexelSize.xy;
                fixed4 detailCol = (tex2D(_Detail, detailTexCoord) * 2.0) - 1.0;
                return detailCol;
            }

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

#define SMF_INTENSITY_MULT (210.0 / 256.0) + (1.0 / 256.0) - (1.0 / 2048.0) - (1.0 / 4096.0)

            float4 GetShadeInt(float groundLightInt, float groundShadowCoeff, float groundDiffuseAlpha)
            {
                float4 groundShadeInt = float4(0.0, 0.0, 0.0, 1.0);

                groundShadeInt.rgb = unity_AmbientGround.rgb + _GroundDiffuseColor * (groundLightInt * groundShadowCoeff);
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

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 detailColor;

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
#endif

                fixed4 diffuseColor = tex2D(_Map, i.diffCoords);

                float cosAngleDiffuse = saturate(dot(_WorldSpaceLightPos0, normal));

                float3 viewDir = normalize(i.worldPos - _WorldSpaceCameraPos.xyz);
                float3 halfDir = normalize(_WorldSpaceLightPos0.xyz - viewDir);
                float cosAngleSpecular = clamp(dot(halfDir, normal), 0.001, 1.0);

                float shadowCoeff = LIGHT_ATTENUATION(i);
                float4 shadeInt = GetShadeInt(cosAngleDiffuse, shadowCoeff, diffuseColor.a);

                float4 fragColor;
                fragColor.rgb = (diffuseColor.rgb + detailColor.rgb) * shadeInt.rgb;
                fragColor.a = shadeInt.a;

                fixed4 specularColor = tex2D(_Specular, i.normalCoords);
                float specularExp = specularColor.a * 16.0;
                float specularPow = max(0.0, pow(cosAngleSpecular, specularExp));
                fragColor.rgb += (specularColor.rgb * specularPow * shadowCoeff);
                //return specularPow;

                UNITY_APPLY_FOG(i.fogCoord, fragColor);
                return fragColor;
            }

            ENDCG
        }

        Pass
        {
            Tags{ "LightMode" = "ShadowCaster" }
            CGPROGRAM
            #pragma vertex VSMain
            #pragma fragment PSMain

            float4 VSMain(float4 vertex:POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(vertex);
            }

            float4 PSMain(float4 vertex:SV_POSITION) : SV_TARGET
            {
                return 0;
            }

            ENDCG
        }
    }
}
