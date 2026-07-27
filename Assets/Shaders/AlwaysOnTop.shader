// 始终渲染在最前面（ZTest Always）的透明无光照 shader。
// 给"透视"物体用：哪怕被真实墙/障碍挡住也照样显示（HL2 透视/穿墙效果）。
// 用于小车信标（CarBeacon），也可用于点云（H3）等需要穿墙显示的物体。
//
// 用法：Unity 里 Right-Click → Material，Shader 选 "MRReP/AlwaysOnTop"，调 Color，赋给信标物体。
Shader "MRReP/AlwaysOnTop"
{
    Properties
    {
        _Color ("Color", Color) = (1.0, 0.41, 0.77, 1.0) // 默认粉色，和 WebRop 点云一致
    }

    SubShader
    {
        Tags {
            "Queue" = "Overlay"        // 最后画，叠在所有东西上面
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        ZTest Always       // ★关键：不做深度测试 → 永远画在前面 → 穿墙显示
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; };

            float4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
