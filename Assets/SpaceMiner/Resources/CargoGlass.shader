Shader "SpaceMiner/Cargo Glass"
{
    Properties { _Color ("Glass tint", Color) = (0.25,0.65,0.80,0.12) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct Input { float4 vertex : POSITION; };
            struct Output { float4 position : SV_POSITION; };
            Output vert(Input v) { Output o; o.position=UnityObjectToClipPos(v.vertex); return o; }
            fixed4 frag(Output i) : SV_Target { return _Color; }
            ENDCG
        }
    }
}
