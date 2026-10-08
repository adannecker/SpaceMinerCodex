Shader "SpaceMiner/MiraHair"
{
    Properties { _MainTex("Albedo",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Cutoff("Cutoff",Range(0,1))=.3 }
    SubShader {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alphatest:_Cutoff
        #pragma target 3.0
        sampler2D _MainTex; fixed4 _Color;
        struct Input { float2 uv_MainTex; };
        void surf(Input IN, inout SurfaceOutputStandard o) {
            fixed4 c=tex2D(_MainTex,IN.uv_MainTex)*_Color; o.Albedo=c.rgb; o.Alpha=c.a; o.Smoothness=.2;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
