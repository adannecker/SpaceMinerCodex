Shader "SpaceMiner/Asteroid Surface"
{
    Properties
    {
        _CrustColor ("Crust", Color) = (0.13,0.12,0.11,1)
        _ExposureColor ("Exposure", Color) = (0.65,0.67,0.69,1)
        _AccentColor ("Accent", Color) = (0.2,0.18,0.16,1)
        _CrustMetallic ("Crust metallic", Range(0,1)) = 0
        _ExposureMetallic ("Exposure metallic", Range(0,1)) = 0
        _AccentMetallic ("Accent metallic", Range(0,1)) = 0
        _CrustSmoothness ("Crust smoothness", Range(0,1)) = 0.12
        _ExposureSmoothness ("Exposure smoothness", Range(0,1)) = 0.25
        _AccentSmoothness ("Accent smoothness", Range(0,1)) = 0.15
        _DetailTex ("Shared triplanar detail", 2D) = "gray" {}
        _DetailStrength ("Detail contrast", Range(0,1)) = 0.25
        _DetailSize ("Detail size in metres", Float) = 8
        _BumpStrength ("Detail relief", Range(0,1)) = 0.3
        _Faceting ("Geometric facets", Range(0,1)) = 0.4
        _BodySize ("Body extent in metres", Float) = 100
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
        #include "UnityCG.cginc"
        sampler2D _DetailTex;
        float4 _DetailTex_TexelSize;
        fixed4 _CrustColor, _ExposureColor, _AccentColor;
        half _CrustMetallic, _ExposureMetallic, _AccentMetallic;
        half _CrustSmoothness, _ExposureSmoothness, _AccentSmoothness;
        float _DetailStrength, _DetailSize, _BumpStrength, _BodySize, _Faceting;
        struct Input
        {
            float3 localPosition;
            float3 localNormal;
            float3 localTangent;
            float3 worldPos;
            float4 weights;
        };
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.localPosition = v.vertex.xyz;
            o.localNormal = v.normal;
            o.localTangent = v.tangent.xyz;
            o.weights = v.color;
        }
        // R/G contain centred derivatives of the seamless height stored in B.
        // Triplanar projection has no polar stretching or longitudinal seam.
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 n = normalize(IN.localNormal);
            float3 projection = pow(abs(n), 4);
            projection /= max(dot(projection, float3(1,1,1)), 0.0001);
            float3 p = IN.localPosition * (_BodySize / max(_DetailSize,0.25));
            float4 x = tex2D(_DetailTex, p.yz);
            float4 y = tex2D(_DetailTex, p.xz);
            float4 z = tex2D(_DetailTex, p.xy);
            float3 broad = IN.localPosition * 2.5;
            float4 bx = tex2D(_DetailTex,broad.yz);
            float4 by = tex2D(_DetailTex,broad.xz);
            float4 bz = tex2D(_DetailTex,broad.xy);
            float detail = dot(float3(x.b,y.b,z.b)*0.45 + float3(bx.b,by.b,bz.b)*0.55,projection);
            float exposure = saturate(IN.weights.r);
            float accent = min(saturate(IN.weights.g), 1-exposure);
            float crust = 1-exposure-accent;
            float shade = lerp(0.82,1.08,saturate(IN.weights.b));
            o.Albedo = (_CrustColor.rgb*crust + _ExposureColor.rgb*exposure + _AccentColor.rgb*accent)
                * shade * lerp(1,0.35+detail*1.3,_DetailStrength);
            o.Metallic = _CrustMetallic*crust + _ExposureMetallic*exposure + _AccentMetallic*accent;
            o.Smoothness = saturate((_CrustSmoothness*crust + _ExposureSmoothness*exposure + _AccentSmoothness*accent)
                + (detail-0.5)*0.12);
            float3 dx = float3(0,x.r*2-1,x.g*2-1);
            float3 dy = float3(y.r*2-1,0,y.g*2-1);
            float3 dz = float3(z.r*2-1,z.g*2-1,0);
            float3 broadGradient = float3(0,bx.r*2-1,bx.g*2-1)*projection.x
                + float3(by.r*2-1,0,by.g*2-1)*projection.y + float3(bz.r*2-1,bz.g*2-1,0)*projection.z;
            float3 gradient = (dx*projection.x + dy*projection.y + dz*projection.z + broadGradient)*2.5;
            float3 tangent = normalize(IN.localTangent - n*dot(IN.localTangent,n));
            float3 bitangent = cross(n,tangent);
            float3 faceWorld = normalize(cross(ddx(IN.worldPos),ddy(IN.worldPos)));
            float3 face = normalize(mul((float3x3)unity_WorldToObject,faceWorld));
            face *= dot(face,n)<0 ? -1 : 1;
            float3 geometric = normalize(lerp(n,face,_Faceting));
            o.Normal = normalize(float3(dot(geometric,tangent)-dot(gradient,tangent)*_BumpStrength,
                dot(geometric,bitangent)-dot(gradient,bitangent)*_BumpStrength,dot(geometric,n)));
            o.Occlusion = lerp(0.6,1,detail);
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
