Shader "Custom/Splat"
{
    Properties
    {
        _Detail1("Detail 1(RGB)", 2D) = "white" {}
        _Detail2("Detail 2(RGB)", 2D) = "white" {}
        _Detail3("Detail 3(RGB)", 2D) = "white" {}
        _Detail4("Detail 4(RGB)", 2D) = "white" {}
        _Splat("Splat (RGBA)", 2D) = "white" {}
        _Normal("Normal", 2D) = "bump" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _Detail1;
        sampler2D _Detail2;
        sampler2D _Detail3;
        sampler2D _Detail4;
        sampler2D _Splat;
        sampler2D _Normal;

        struct Input
        {
            float2 uv_Splat;
        };

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 splat = tex2D(_Splat, IN.uv_Splat);

            o.Albedo = splat.rgb;
            o.Normal = UnpackNormal(tex2D(_Normal, IN.uv_Splat));
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
