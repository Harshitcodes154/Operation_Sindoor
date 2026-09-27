Shader "Sindoor/Water" {
    Properties {_SunDirection("Sun",Vector)=(0,1,0,0)}
    SubShader{
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Pass{
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _SunDirection;
            CBUFFER_END
            struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;half fog:TEXCOORD1;};
            V Vert(A i){V o;o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);o.fog=ComputeFogFactor(o.p.z);return o;}
            half4 Frag(V i):SV_Target{
                float t=_Time.y;half3 n=normalize(half3(sin(i.w.x*.037+t*.8)*.1+sin(i.w.z*.12+t)*.045,1,cos(i.w.z*.052-t*.5)*.1));half3 v=GetWorldSpaceNormalizeViewDir(i.w);half fresnel=pow(1-saturate(dot(v,n)),4);
                half spec=pow(saturate(dot(reflect(-normalize(_SunDirection.xyz),n),v)),100);
                half3 c=lerp(half3(.024,.085,.11),half3(.27,.39,.44),fresnel)+spec*half3(1,.82,.57)*2;
                return half4(MixFog(c,i.fog),1);
            }
            ENDHLSL
        }
    }
}
