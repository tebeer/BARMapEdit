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
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows
        #pragma require 2darray

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _Map;
        //sampler2D _Detail1;
        //sampler2D _Detail2;
        //sampler2D _Detail3;
        //sampler2D _Detail4;
        //sampler2D _Splat;
        sampler2D _Normal;

        struct Input
        {
            float2 uv_Map;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 map = tex2D(_Map, IN.uv_Map);

            o.Albedo = map.rgb;//splat.rgb;
            o.Normal = UnpackNormal(tex2D(_Normal, IN.uv_Map));
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
