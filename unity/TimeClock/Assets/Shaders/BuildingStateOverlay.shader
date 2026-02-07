Shader "Custom/BuildingStateOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        // 建造进度
        _ConstructionProgress ("Construction Progress", Range(0, 1)) = 1
        _ConstructionColor ("Construction Color", Color) = (0.5, 0.5, 0.5, 1)

        // 选中发光
        _GlowColor ("Glow Color", Color) = (1, 1, 0.5, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 0
        _GlowPulseSpeed ("Glow Pulse Speed", Float) = 2

        // 受损效果
        _DamageLevel ("Damage Level", Range(0, 1)) = 0
        _DamageColor ("Damage Color", Color) = (0.3, 0.2, 0.1, 1)

        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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
            #pragma vertex SpriteVert
            #pragma fragment StateOverlayFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float _ConstructionProgress;
            fixed4 _ConstructionColor;
            fixed4 _GlowColor;
            float _GlowIntensity;
            float _GlowPulseSpeed;
            float _DamageLevel;
            fixed4 _DamageColor;

            fixed4 StateOverlayFrag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;

                // 建造进度效果 - 从下往上渐显
                if (_ConstructionProgress < 1)
                {
                    float constructionMask = step(IN.texcoord.y, _ConstructionProgress);
                    float edgeGlow = 1 - abs(IN.texcoord.y - _ConstructionProgress) * 10;
                    edgeGlow = saturate(edgeGlow) * (1 - _ConstructionProgress);

                    c.rgb = lerp(_ConstructionColor.rgb, c.rgb, constructionMask);
                    c.rgb += _GlowColor.rgb * edgeGlow * 0.5;
                    c.a *= lerp(0.3, 1, constructionMask);
                }

                // 受损效果
                if (_DamageLevel > 0)
                {
                    // 添加裂纹/污渍效果
                    float noise = frac(sin(dot(IN.texcoord, float2(12.9898, 78.233))) * 43758.5453);
                    float damageMask = step(noise, _DamageLevel * 0.5);
                    c.rgb = lerp(c.rgb, _DamageColor.rgb, damageMask * _DamageLevel);
                }

                // 选中发光效果
                if (_GlowIntensity > 0)
                {
                    float pulse = (sin(_Time.y * _GlowPulseSpeed * 3.14159 * 2) + 1) * 0.5;
                    float glow = _GlowIntensity * (0.5 + pulse * 0.5);
                    c.rgb += _GlowColor.rgb * glow * c.a;
                }

                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
