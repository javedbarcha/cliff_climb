Shader "CanyonClimber/Toon"
{
    Properties
    {
        _BaseColor("Sunlit color", Color) = (1,.55,.25,1)
        _ShadeColor("Shade color", Color) = (.55,.22,.19,1)
        _RimColor("Rim tint", Color) = (1,.85,.55,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ToonForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadeColor;
                half4 _RimColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; half fog : TEXCOORD2; };
            Varyings Vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.fog = ComputeFogFactor(p.positionCS.z); return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                Light light = GetMainLight();
                half diffuse = dot(n, light.direction) * .5h + .5h;
                half band = diffuse > .72h ? 1.0h : diffuse > .38h ? .65h : .18h;
                half3 color = lerp(_ShadeColor.rgb, _BaseColor.rgb, band);
                half rim = pow(1.0h - saturate(dot(n, GetWorldSpaceNormalizeViewDir(i.positionWS))), 3.0h);
                color += _RimColor.rgb * rim * .09h;
                return half4(MixFog(color, i.fog), 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 p : POSITION; };
            float4 DepthVert(A i) : SV_POSITION { return TransformObjectToHClip(i.p.xyz); }
            half4 DepthFrag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
