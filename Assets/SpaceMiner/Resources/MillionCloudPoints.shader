Shader "SpaceMiner/Million Cloud Points"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct input { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct output { float4 position : SV_POSITION; fixed4 color : COLOR; float size : PSIZE; };
            output vert(input v) { output o; o.position = UnityObjectToClipPos(v.vertex); o.color = v.color; o.size = 1; return o; }
            fixed4 frag(output i) : SV_Target { return i.color; }
            ENDCG
        }
    }
}
