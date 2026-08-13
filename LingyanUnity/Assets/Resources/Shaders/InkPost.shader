// 水墨后处理（阶段 9）：轻降饱和、暖纸色偏、暗角。
// 参数刻意收敛——底色氛围靠场景光，本 pass 只做"收在一张纸上"的最后一笔。
Shader "Lingyan/InkPost"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed gray = dot(col.rgb, fixed3(0.299, 0.587, 0.114));
                col.rgb = lerp(col.rgb, fixed3(gray, gray, gray), 0.12);
                col.rgb *= fixed3(1.030, 1.000, 0.955);
                float2 d = i.uv - 0.5;
                float vig = 1.0 - dot(d, d) * 0.50;
                col.rgb *= saturate(vig);
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
