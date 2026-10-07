Shader "SpaceMiner/PlanetRing" {
 Properties { _Color("Ring color",Color)=(.7,.6,.4,1) }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Color;float3 _SpaceMinerSunPosition,_SpaceMinerCompanionPosition;
 struct v2f {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float3 normal:TEXCOORD2;};
 v2f vert(appdata_base v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);return o;}
 fixed4 frag(v2f i):SV_Target {float band=.6+.2*sin(i.uv.y*145)+.15*sin(i.uv.y*43);float gap=smoothstep(.005,.014,abs(i.uv.y-.58));float edge=smoothstep(0,.05,i.uv.y)*smoothstep(0,.05,1-i.uv.y);float light=.16+2.4*abs(dot(normalize(i.normal),normalize(_SpaceMinerSunPosition-i.world)))+1.1*abs(dot(normalize(i.normal),normalize(_SpaceMinerCompanionPosition-i.world)));return fixed4(_Color.rgb*band*light,edge*gap*.8);}
 ENDCG }
 }
}
