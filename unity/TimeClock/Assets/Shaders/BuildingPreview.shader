Shader "Custom/BuildingPreview"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _PreviewColor ("Preview Color", Color) = (0,1,0,0.5)
        _InvalidColor ("Invalid Color", Color) = (1,0,0,0.5)
        _IsValid ("Is Valid", Float) = 1
        _PulseSpeed ("Pulse Speed", Float) = 2
        _PulseMin ("Pulse Min Alpha", Float) = 0.3
        _PulseMax ("Pulse Max Alpha", Float) = 0.7
        _Color ("Color", Color) = (1,1,1,1)
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
            "Queue"="Transparent+100"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment PreviewFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            fixed4 _PreviewColor;
            fixed4 _InvalidColor;
            float _IsValid;
            float _PulseSpeed;
            float _PulseMin;
            float _PulseMax;

            fixed4 PreviewFrag(v2f IN) : SV_Target
            {
                fixed4 texColor = SampleSpriteTexture(IN.texcoord);

                // 选择颜色
                fixed4 tintColor = _IsValid > 0.5 ? _PreviewColor : _InvalidColor;

                // 脉冲效果
                float pulse = lerp(_PulseMin, _PulseMax,
                    (sin(_Time.y * _PulseSpeed * 3.14159 * 2) + 1) * 0.5);

                // 应用颜色和透明度
                fixed4 result;
                result.rgb = tintColor.rgb;
                result.a = texColor.a * pulse * tintColor.a;

                return result;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
