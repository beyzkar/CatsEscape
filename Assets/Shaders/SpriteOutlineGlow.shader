Shader "Custom/SpriteOutlineGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Glow Settings)]
        [HDR] _GlowColor ("Glow Color", Color) = (0, 0.5, 1, 1)
        _SoftGlowSpread ("Halo Spread", Range(0, 100)) = 10
        _SoftGlowIntensity ("Halo Intensity", Range(0, 50)) = 5
        
        [Header(Outline Settings)]
        _OutlineColor ("Outline Color", Color) = (0, 0.8, 1, 1)
        _OutlineWidth ("Outline Width", Range(0, 30)) = 5
        
        [MaterialToggle] _AuraMode ("Aura Mode (Transparent Center)", Float) = 0
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _GlowAmount ("Inner Glow", Float) = 0
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
            float _AuraMode;

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

            // High-quality Octagon sampling helper
            float SampleAlpha(float2 uv, float spread)
            {
                float2 s = _MainTex_TexelSize.xy * spread;
                float a = 0;
                a += tex2D(_MainTex, uv + float2(s.x, 0)).a;
                a += tex2D(_MainTex, uv + float2(-s.x, 0)).a;
                a += tex2D(_MainTex, uv + float2(0, s.y)).a;
                a += tex2D(_MainTex, uv + float2(0, -s.y)).a;
                float2 ds = s * 0.707;
                a += tex2D(_MainTex, uv + ds).a;
                a += tex2D(_MainTex, uv - ds).a;
                a += tex2D(_MainTex, uv + float2(ds.x, -ds.y)).a;
                a += tex2D(_MainTex, uv + float2(-ds.x, ds.y)).a;
                return a * 0.125;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // 1. Original Alpha
                float aSprite = c.a;

                // 2. Outline Alpha
                float aOutline = (_OutlineWidth > 0) ? saturate(SampleAlpha(IN.texcoord, _OutlineWidth) * 10.0) : aSprite;

                // 3. Halo Alpha
                float totalSpread = _OutlineWidth + _SoftGlowSpread;
                float aHaloRaw = SampleAlpha(IN.texcoord, totalSpread);
                float aHalo = saturate(aHaloRaw * _SoftGlowIntensity);

                // --- COMPOSITING ---
                fixed4 glowColor = fixed4(0,0,0,0);

                // Build the Glow/Outline layer
                if (aHalo > 0.001) {
                    glowColor.rgb = _GlowColor.rgb * aHalo;
                    glowColor.a = aHalo;
                }

                if (aOutline > 0.001) {
                    float outlineMask = saturate(aOutline - aSprite);
                    glowColor.rgb = lerp(glowColor.rgb, _OutlineColor.rgb, outlineMask);
                    glowColor.a = max(glowColor.a, aOutline);
                }

                fixed4 finalColor = glowColor;

                // Conditional Masking based on Aura Mode
                if (_AuraMode > 0.5)
                {
                    // Aura Mode: Middle is transparent (for dedicated aura objects)
                    finalColor.a *= (1.0 - aSprite);
                }
                else
                {
                    // Normal Mode: Show Character + Glow (for the cat itself)
                    finalColor.rgb = lerp(finalColor.rgb, c.rgb, aSprite);
                    finalColor.a = max(finalColor.a, aSprite);
                }

                finalColor.rgb *= finalColor.a;
                return finalColor;
            }
        ENDCG
        }
    }
}
