Shader "SpaceMiner/EmissiveSurface" {
 Properties {_MainTex("Surface",2D)="white"{} _Color("Color",Color)=(1,1,1,1)}
 SubShader {Tags {"RenderType"="Opaque"} Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;float4 _Color;
 struct v2f {float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
 v2f vert(appdata_base v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
 fixed4 frag(v2f i):SV_Target {return fixed4(tex2D(_MainTex,i.uv).rgb*_Color.rgb*2.5,1);}
 ENDCG}} }
