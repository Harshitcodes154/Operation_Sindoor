Shader "Sindoor/TheatreTerrain" {
    Properties { _Rock("Rock",2D)="white"{} _Grass("Ground cover",2D)="white"{} _Normal("Rock normal",2D)="bump"{} }
    SubShader {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        Pass {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_Rock);SAMPLER(sampler_Rock);TEXTURE2D(_Grass);SAMPLER(sampler_Grass);TEXTURE2D(_Normal);SAMPLER(sampler_Normal);
            struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;half fog:TEXCOORD2;};
            V Vert(A i){V o;o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);o.fog=ComputeFogFactor(o.p.z);return o;}
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);}
            half4 Frag(V i):SV_Target{
                half3 n=normalize(i.n);half3 weights=pow(abs(n),4);weights/=max(dot(weights,half3(1,1,1)),.001);
                float2 uv=i.w.xz*.07;half3 rock=SAMPLE_TEXTURE2D(_Rock,sampler_Rock,i.w.zy*.07).rgb*weights.x+SAMPLE_TEXTURE2D(_Rock,sampler_Rock,uv).rgb*weights.y+SAMPLE_TEXTURE2D(_Rock,sampler_Rock,i.w.xy*.07).rgb*weights.z;
                half3 ground=SAMPLE_TEXTURE2D(_Grass,sampler_Grass,uv*.7).rgb;
                float macro=Noise(i.w.xz*.0018)*.6+Noise(i.w.xz*.008)*.25+Noise(i.w.xz*.04)*.15;
                half slope=saturate((1-n.y)*4+i.w.y*.0006);half3 albedo=lerp(ground,rock,saturate(slope+macro*.52))*(.68+macro*.5);
                half3 bump=UnpackNormal(SAMPLE_TEXTURE2D(_Normal,sampler_Normal,uv));n=normalize(n+half3(bump.x,0,bump.y)*.23);
                Light light=GetMainLight(TransformWorldToShadowCoord(i.w));half diffuse=saturate(dot(n,light.direction));
                half3 color=albedo*(SampleSH(n)+light.color*diffuse*light.shadowAttenuation);
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}
