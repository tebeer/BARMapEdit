Shader "Custom/BAR"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _MetalRoughness("MetalRoughness", 2D) = "black" {}
        _Normal("Normal", 2D) = "bump" {}
        _Glossiness ("Smoothness", Range(0,1)) = 1
        _Metallic("Metallic", Range(0,1)) = 1.0
        _Emission("Emission", Range(0,1)) = 1.0
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

        sampler2D _MainTex;
        sampler2D _MetalRoughness;
        sampler2D _Normal;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        half _Emission;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            fixed4 mr = tex2D(_MetalRoughness, IN.uv_MainTex);

            half emission = mr.r * _Emission;

            o.Albedo = c.rgb * (1 - emission);
            o.Normal = UnpackNormal(tex2D(_Normal, IN.uv_MainTex));
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic * mr.g;
            o.Smoothness = _Glossiness * (1-mr.b);
            o.Emission = c.rgb * emission;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
