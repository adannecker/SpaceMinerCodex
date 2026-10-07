Shader "SpaceMiner/RememberedPaper" {
 Properties { _MainTex("Drawing",2D)="white"{} _PreviousTex("Previous drawing",2D)="white"{} _Blend("Cross dissolve",Float)=1 _EdgeMask("Torn edges",2D)="white"{} _Fade("Fade",Float)=1 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex,_PreviousTex,_EdgeMask;float4 _MainTex_ST,_PreviousUV;float _Fade,_Blend;
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 edge:TEXCOORD1;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.texcoord,_MainTex);o.edge=v.texcoord;return o;}
 fixed4 frag(v2f i):SV_Target {fixed4 c=lerp(tex2D(_PreviousTex,i.edge*_PreviousUV.xy+_PreviousUV.zw),tex2D(_MainTex,i.uv),_Blend);float e=min(min(i.edge.x,1-i.edge.x),min(i.edge.y,1-i.edge.y));c.rgb*=lerp(.67,1,smoothstep(0,.06,e));c.a=tex2D(_EdgeMask,i.edge).a*_Fade;return c;}
 ENDCG }
 }
}
