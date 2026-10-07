Shader "SpaceMiner/OrbitGuide" {
 Properties { _Color("Color",Color)=(.25,.4,.55,.3) }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Color;
 float4 vert(float4 vertex:POSITION):SV_POSITION{return UnityObjectToClipPos(vertex);}
 fixed4 frag():SV_Target{return _Color;}
 ENDCG }}
}
