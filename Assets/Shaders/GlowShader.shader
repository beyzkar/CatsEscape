Shader "Custom/GlowShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (0, 1, 1, 1)
        _GlowPower ("Glow Power", Range(0.1, 8.0)) = 2.0
        _GlowIntensity ("Glow Intensity", Range(0, 20)) = 10.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows

        sampler2D _MainTex;
        float4 _GlowColor;
        float _GlowPower;
        float _GlowIntensity;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            half4 c = tex2D (_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            
            // Stronger Rim lighting logic
            half rim = 1.0 - saturate(dot (normalize(IN.viewDir), o.Normal));
            // Lower power = thicker line. Higher intensity = brighter glow.
            o.Emission = _GlowColor.rgb * pow (rim, _GlowPower) * _GlowIntensity;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
