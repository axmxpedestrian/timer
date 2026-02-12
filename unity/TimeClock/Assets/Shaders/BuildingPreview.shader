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
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _PreviewColor;
                half4 _InvalidColor;
                float _IsValid;
                float _PulseSpeed;
                float _PulseMin;
                float _PulseMax;
                half4 _Color;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // 选择颜色
                half4 tintColor = _IsValid > 0.5 ? _PreviewColor : _InvalidColor;

                // 脉冲效果
                float pulse = lerp(_PulseMin, _PulseMax,
                    (sin(_Time.y * _PulseSpeed * 3.14159 * 2) + 1) * 0.5);

                // 应用颜色和透明度
                // 只用 texColor.a 控制形状裁剪，pulse 控制呼吸透明度
                half4 result;
                result.rgb = tintColor.rgb;
                result.a = texColor.a * IN.color.a * pulse;

                return result;
            }
            ENDHLSL
        }
    }

    // 内置管线 Fallback（兼容非 URP 环境）
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
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            fixed4 _PreviewColor;
            fixed4 _InvalidColor;
            float _IsValid;
            float _PulseSpeed;
            float _PulseMin;
            float _PulseMax;
            fixed4 _Color;

            v2f vert(appdata IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.pos = UnityObjectToClipPos(IN.vertex);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, IN.uv);

                fixed4 tintColor = _IsValid > 0.5 ? _PreviewColor : _InvalidColor;

                float pulse = lerp(_PulseMin, _PulseMax,
                    (sin(_Time.y * _PulseSpeed * 3.14159 * 2) + 1) * 0.5);

                fixed4 result;
                result.rgb = tintColor.rgb;
                result.a = texColor.a * IN.color.a * pulse;

                return result;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
