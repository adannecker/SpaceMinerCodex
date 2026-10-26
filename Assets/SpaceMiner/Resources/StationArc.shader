Shader "SpaceMiner/StationArc"
{
    Properties { _Color ("Arc color", Color) = (0.45,0.83,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Lighting Off Cull Off ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct Input { float4 vertex : POSITION; };
            float4 vert(Input input) : SV_POSITION { return UnityObjectToClipPos(input.vertex); }
            fixed4 frag() : SV_Target { return _Color; }
            ENDCG
        }
    }
}
