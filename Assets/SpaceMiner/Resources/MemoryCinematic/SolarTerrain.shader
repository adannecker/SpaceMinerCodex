Shader "SpaceMiner/SolarTerrain" {
 Properties {_MainTex("Surface",2D)="white"{} _Color("Color",Color)=(1,1,1,1)}
 SubShader {Tags {"RenderType"="Opaque"} Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;float4 _Color;float3 _SpaceMinerSunPosition,_SpaceMinerCompanionPosition;
 struct v2f {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;float3 world:TEXCOORD2;};
 v2f vert(appdata_base v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 fixed4 frag(v2f i):SV_Target {float3 n=normalize(i.normal);float a=max(0,dot(n,normalize(_SpaceMinerSunPosition-i.world)));float b=max(0,dot(n,normalize(_SpaceMinerCompanionPosition-i.world)));return fixed4(tex2D(_MainTex,i.uv).rgb*_Color.rgb*(.16+a*2.4+b*float3(.78,.87,1)*1.1),1);}
 ENDCG}} }
