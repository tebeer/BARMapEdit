Shader "Custom/Splat"
{
    Properties
    {
        //_TileMap("TileMap", 2DArray) = "" {}
        _Map("Map", 2D) = "white" {}
        //_Detail1("Detail 1(RGB)", 2D) = "white" {}
        //_Detail2("Detail 2(RGB)", 2D) = "white" {}
        //_Detail3("Detail 3(RGB)", 2D) = "white" {}
        //_Detail4("Detail 4(RGB)", 2D) = "white" {}
        //_Splat("Splat (RGBA)", 2D) = "white" {}
        _Normal("Normal", 2D) = "bump" {}
        _ChunksX("ChunksX", Float) = 1
        _ChunksY("ChunksY", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _Map;
        //sampler2D _Detail1;
        //sampler2D _Detail2;
        //sampler2D _Detail3;
        //sampler2D _Detail4;
        //sampler2D _Splat;
        sampler2D _Normal;
        float _ChunksX;
        float _ChunksY;

        struct Input
        {
            float2 uv_Map;
            float2 chunkCoords;
            float2 normalCoords;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            //v.texcoord = v.texcoord;
            o.chunkCoords.x = (_ChunksX * v.texcoord.x);
            o.chunkCoords.y = (_ChunksY * v.texcoord.y);
            o.normalCoords = v.texcoord;
            o.normalCoords.y = 1 - o.normalCoords.y;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            //float2 uv_Chunk = float2(_ChunksX * IN.uv_Map.x, _ChunksY * IN.uv_Map.y);
            //uv_Chunk = frac(uv_Chunk);

            fixed4 map = tex2D(_Map, IN.chunkCoords);

            o.Albedo = map.rgb;//splat.rgb;
            o.Normal = UnpackNormal(tex2D(_Normal, IN.normalCoords));
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
