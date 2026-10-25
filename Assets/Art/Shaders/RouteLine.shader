Shader "CanyonClimber/RouteLine"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 p : POSITION; half4 c : COLOR; };
            struct V { float4 p : SV_POSITION; half4 c : COLOR; };
            V Vert(A i) { V o; o.p = TransformObjectToHClip(i.p.xyz); o.c = i.c; return o; }
            half4 Frag(V i) : SV_Target { return i.c; }
            ENDHLSL
        }
    }
}
