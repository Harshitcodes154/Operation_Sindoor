Shader "Sindoor/Canopy" {
    Properties { _BaseColor("Optical tint",Color)=(.12,.22,.24,.4) }
    SubShader {
        Tags {"RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Pass {
            Tags {"LightMode"="UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            struct A {float4 p:POSITION;float3 n:NORMAL;};
            struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;half fog:TEXCOORD2;};
            V Vert(A i){V o;o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);o.fog=ComputeFogFactor(o.p.z);return o;}
            half4 Frag(V i):SV_Target {
                half3 n=normalize(i.n),v=GetWorldSpaceNormalizeViewDir(i.w),r=reflect(-v,n);
                half fresnel=.04+.96*pow(1-saturate(dot(n,v)),5);
                half3 reflection=GlossyEnvironmentReflection(r,i.w,.08,1);
                Light sun=GetMainLight();half glint=pow(saturate(dot(r,sun.direction)),256);
                half3 color=_BaseColor.rgb*.4+reflection*(.45+fresnel)+sun.color*glint*2;
                return half4(MixFog(color,i.fog),saturate(_BaseColor.a+fresnel*.5));
            }
            ENDHLSL
        }
    }
}
