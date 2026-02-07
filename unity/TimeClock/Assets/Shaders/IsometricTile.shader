Shader "Custom/IsometricTile"
{
    Properties
    {
        _Color ("Main Color", Color) = (0.4, 0.6, 0.3, 1)
        _GridColor ("Grid Color", Color) = (0.2, 0.3, 0.2, 1)
        _GridWidth ("Grid Width", Range(0.01, 0.1)) = 0.02
        _Glossiness ("Smoothness", Range(0,1)) = 0.3
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma multi_compile_instancing
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        fixed4 _Color;
        fixed4 _GridColor;
        float _GridWidth;
        half _Glossiness;
        half _Metallic;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // 计算网格线
            float2 uv = IN.uv_MainTex;

            // 边缘检测
            float edgeX = step(uv.x, _GridWidth) + step(1.0 - _GridWidth, uv.x);
            float edgeY = step(uv.y, _GridWidth) + step(1.0 - _GridWidth, uv.y);
            float edge = saturate(edgeX + edgeY);

            // 混合颜色
            fixed4 c = lerp(_Color, _GridColor, edge);

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
