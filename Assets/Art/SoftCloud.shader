Shader "Sindoor/SoftCloud" {
    Properties { _BaseMap("Cloud opacity",2D)="white"{} }
    SubShader {
        Tags {"RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; half fog:TEXCOORD1; };
            V Vert(A i){ V o; o.p=TransformObjectToHClip(i.p.xyz); o.uv=i.uv; o.fog=ComputeFogFactor(o.p.z); return o; }
            half4 Frag(V i):SV_Target {half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);Light sun=GetMainLight();half illumination=clamp(dot(sun.color,half3(.2126,.7152,.0722)),.025,1);c.rgb*=lerp(half3(.42,.48,.56),sun.color*.65+.3,smoothstep(.1,.8,i.uv.y))*illumination;c.rgb=MixFog(c.rgb,i.fog);return c;}
            ENDHLSL
        }
    }
}
