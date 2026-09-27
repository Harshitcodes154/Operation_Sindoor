Shader "Sindoor/Particle" {
    Properties { _BaseMap("Density",2D)="white"{} _Additive("Emission",Float)=0 }
    SubShader {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float _Additive;
            CBUFFER_END
            struct A{float4 p:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;half fog:TEXCOORD1;};
            V Vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.color=i.color;o.fog=ComputeFogFactor(o.p.z);return o;}
            half4 Frag(V i):SV_Target{half4 t=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);half3 c=i.color.rgb*lerp(t.rgb,half3(1,1,1),_Additive);return half4(MixFog(c,i.fog),t.a*i.color.a);}
            ENDHLSL
        }
    }
}
