Shader "Custom/Terrain"
{
    Properties
    {
        _Map("Map", 2D) = "white" {}
        _Normal("Normal", 2D) = "bump" {}
        _ChunksX("ChunksX", Float) = 1
        _ChunksY("ChunksY", Float) = 1
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

                float3 normal = UnpackNormal(tex2D(_Normal, i.normalCoords));
                normal = float3(normal.x, normal.z, -normal.y);

                float d = dot(_WorldSpaceLightPos0, normal);
                d = saturate(2*d);

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
