Shader "Custom/SpriteOutlineGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Glow Settings)]
        [HDR] _GlowColor ("Glow Color", Color) = (1,1,0,1)
        _GlowAmount ("Inner Glow Intensity", Range(0, 10)) = 1
        _SoftGlowSpread ("Soft Glow Spread", Range(0, 10)) = 2
        _SoftGlowIntensity ("Soft Glow Intensity", Range(0, 5)) = 0.5
        
        [Header(Outline Settings)]
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 5)) = 1
        
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _GlowColor;
            float _GlowAmount;
            float _SoftGlowSpread;
            float _SoftGlowIntensity;
            fixed4 _OutlineColor;
            float _OutlineWidth;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                float2 texelSize = _MainTex_TexelSize.xy;
                
                // --- Soft Glow Spread (Halo) ---
                float softGlow = 0;
                if (_SoftGlowIntensity > 0)
                {
                    // Sample alpha in a cross pattern further out
                    float2 spread = texelSize * _SoftGlowSpread;
                    softGlow += tex2D(_MainTex, IN.texcoord + float2(spread.x, spread.y)).a;
                    softGlow += tex2D(_MainTex, IN.texcoord + float2(-spread.x, -spread.y)).a;
                    softGlow += tex2D(_MainTex, IN.texcoord + float2(spread.x, -spread.y)).a;
                    softGlow += tex2D(_MainTex, IN.texcoord + float2(-spread.x, spread.y)).a;
                    softGlow *= 0.25;
                }

                // --- Outline Logic ---
                if (_OutlineWidth > 0 && c.a < 0.9)
                {
                    float2 outlineSize = texelSize * _OutlineWidth;
                    float alphaSum = 0;
                    alphaSum += tex2D(_MainTex, IN.texcoord + float2(outlineSize.x, 0)).a;
                    alphaSum += tex2D(_MainTex, IN.texcoord + float2(-outlineSize.x, 0)).a;
                    alphaSum += tex2D(_MainTex, IN.texcoord + float2(0, outlineSize.y)).a;
                    alphaSum += tex2D(_MainTex, IN.texcoord + float2(0, -outlineSize.y)).a;
                    
                    if (alphaSum > 0 && c.a < 0.1)
                    {
                        fixed4 o = _OutlineColor;
                        o.rgb *= o.a;
                        // Add some of the glow color to the outline too
                        o.rgb += _GlowColor.rgb * _GlowAmount * 0.5;
                        return o;
                    }
                }

                // --- Final Color Assembly ---
                fixed4 finalColor = c;
                
                // Add emissive inner glow
                finalColor.rgb += (c.rgb * _GlowColor.rgb * _GlowAmount);
                
                // Add procedural soft outer halo
                if (c.a < 0.5)
                {
                    finalColor.rgb += (_GlowColor.rgb * softGlow * _SoftGlowIntensity);
                    finalColor.a = max(finalColor.a, softGlow * _SoftGlowIntensity);
                }
                
                finalColor.rgb *= finalColor.a;
                return finalColor;
            }
        ENDCG
        }
    }
}
