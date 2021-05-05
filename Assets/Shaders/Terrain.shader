Shader "Custom/Terrain"
{
    Properties
    {
        _Map("Map", 2D) = "white" {}
        _Normal("Normal", 2D) = "bump" {}
        _ChunksX("ChunksX", Float) = 1
        _ChunksY("ChunksY", Float) = 1
        _SplatDistr("SplatDistr", 2D) = "white" {}
        _SplatDetailNormal1("SplatDetailNormal1", 2D) = "bump" {}
        _SplatDetailNormal2("SplatDetailNormal2", 2D) = "bump" {}
        _SplatDetailNormal3("SplatDetailNormal3", 2D) = "bump" {}
        _SplatDetailNormal4("SplatDetailNormal4", 2D) = "bump" {}
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

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 chunkCoords : TEXCOORD0;
                float2 normalCoords : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                LIGHTING_COORDS(3, 4)
            };

            sampler2D _Map;
            sampler2D _Normal;
            float _ChunksX;
            float _ChunksY;
            fixed4 _GroundDiffuseColor;

            sampler2D _SplatDistr;
            sampler2D _SplatDetailNormal1;
            sampler2D _SplatDetailNormal2;
            sampler2D _SplatDetailNormal3;
            sampler2D _SplatDetailNormal4;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.chunkCoords.x = (_ChunksX * v.uv.x);
                o.chunkCoords.y = (_ChunksY * v.uv.y);
                o.normalCoords = v.uv;
                o.normalCoords.y = 1 - v.uv.y;
                UNITY_TRANSFER_FOG(o,o.vertex);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_Map, i.chunkCoords);
                col.rgb *= _GroundDiffuseColor;


                float3 normal;
                normal.xz = tex2D(_Normal, i.normalCoords).rg;
                normal.y = sqrt(1.0 - dot(normal.xz, normal.xz));

                float3 tTangent = normalize(cross(normal, float3(1.0, 0.0, 0.0)));
                float3 sTangent = cross(normal, tTangent);
                float3x3 stnMatrix = float3x3(sTangent, tTangent, normal);
                stnMatrix = transpose(stnMatrix);

                float4 splatDistr = tex2D(_SplatDetailNormal1, i.normalCoords);

                float2 splatDetailStrength = float2(0.0, 0.0);
                splatDetailStrength.x = min(1.0, dot(splatDistr, 1.0));

                float4 splatDetailNormal;
                splatDetailNormal = ((tex2D(_SplatDetailNormal1,  i.normalCoords*50) * 2.0 - 1.0) * splatDistr.r);
                splatDetailNormal += ((tex2D(_SplatDetailNormal2, i.normalCoords*50) * 2.0 - 1.0) * splatDistr.g);
                splatDetailNormal += ((tex2D(_SplatDetailNormal3, i.normalCoords*50) * 2.0 - 1.0) * splatDistr.b);
                splatDetailNormal += ((tex2D(_SplatDetailNormal4, i.normalCoords*50) * 2.0 - 1.0) * splatDistr.a);

                // note: y=0.01 (pointing up) in case all splat-cofacs are zero
                splatDetailNormal.y = max(splatDetailNormal.y, 0.01);

                //splatDetailStrength.y = clamp(splatDetailNormal.a, -1.0, 1.0);

                normal = normalize(lerp(normal, normalize(mul(stnMatrix, splatDetailNormal.xyz)), splatDetailStrength.x));

                float d = dot(_WorldSpaceLightPos0, normal);
                d = saturate(d);

                float atten = LIGHT_ATTENUATION(i);

                float3 light = unity_AmbientGround.rgb + atten * d * _LightColor0.rgb;

                col.rgb *= light;

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
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
