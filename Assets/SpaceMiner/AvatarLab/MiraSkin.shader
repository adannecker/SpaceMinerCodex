Shader "SpaceMiner/MiraSkin"
{
    Properties { _MainTex("Skin",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Glossiness("Smoothness",Range(0,1))=.4 }
    SubShader {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex; fixed4 _Color; half _Glossiness;
        struct Input { float2 uv_MainTex; float3 worldPos; };
        float grain(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
        void surf(Input IN,inout SurfaceOutputStandard o){
            fixed4 c=tex2D(_MainTex,IN.uv_MainTex)*_Color;
            float2 p=IN.uv_MainTex*1500;
            float g=grain(floor(p));
            float neck=smoothstep(1.14,1.18,IN.worldPos.y)*(1-smoothstep(1.22,1.25,IN.worldPos.y));
            float fold=sin(IN.worldPos.y*420+sin(IN.worldPos.x*30)*.4)*neck;
            o.Albedo=c.rgb*(.994+.012*g);
            o.Normal=normalize(float3((g-.5)*.045,(grain(floor(p)+float2(0,1))-.5)*.045+fold*.035,1));
            o.Smoothness=_Glossiness*(.88+.20*g);o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
