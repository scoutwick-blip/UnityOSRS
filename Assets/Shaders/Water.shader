Shader "RuneRealm/Water"
{
    // Stylized water shader with wave animation, fresnel, and transparency.
    // URP-compatible. Combines OSRS-clean aesthetic with Skyrim-style depth.

    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.2, 0.5, 0.6, 0.6)
        _DeepColor ("Deep Color", Color) = (0.05, 0.15, 0.3, 0.9)
        _FoamColor ("Foam Color", Color) = (0.9, 0.95, 1, 1)
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 1
        _WaveAmplitude ("Wave Amplitude", Range(0, 1)) = 0.1
        _WaveFrequency ("Wave Frequency", Range(0, 10)) = 2
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.5
        _SpecularPower ("Specular Power", Range(1, 128)) = 64
        _SpecularIntensity ("Specular Intensity", Range(0, 3)) = 1
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 3
        _FoamThreshold ("Foam Threshold", Range(0, 2)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                float _WaveSpeed;
                float _WaveAmplitude;
                float _WaveFrequency;
                float4 _NormalMap_ST;
                float _NormalStrength;
                float _SpecularPower;
                float _SpecularIntensity;
                float _FresnelPower;
                float _FoamThreshold;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float3 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
                float fogCoord : TEXCOORD6;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                // Vertex wave animation
                float wave1 = sin(_Time.y * _WaveSpeed + worldPos.x * _WaveFrequency) * _WaveAmplitude;
                float wave2 = sin(_Time.y * _WaveSpeed * 0.7 + worldPos.z * _WaveFrequency * 0.8) * _WaveAmplitude * 0.5;
                float wave3 = cos(_Time.y * _WaveSpeed * 0.5 + (worldPos.x + worldPos.z) * _WaveFrequency * 0.5) * _WaveAmplitude * 0.3;

                input.positionOS.y += wave1 + wave2 + wave3;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = normInputs.tangentWS;
                output.bitangentWS = normInputs.bitangentWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _NormalMap);
                output.fogCoord = ComputeFogFactor(posInputs.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Animated UV for normal map
                float2 uv1 = input.uv + _Time.y * _WaveSpeed * 0.05 * float2(1, 0.5);
                float2 uv2 = input.uv * 1.3 + _Time.y * _WaveSpeed * 0.03 * float2(-0.5, 1);

                half3 normal1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1));
                half3 normal2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2));
                half3 combinedNormal = normalize(half3(
                    (normal1.xy + normal2.xy) * _NormalStrength,
                    1
                ));

                // Transform normal to world space
                half3x3 TBN = half3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                half3 normalWS = normalize(mul(combinedNormal, TBN));

                // Fresnel effect
                float fresnel = pow(1.0 - saturate(dot(normalize(input.viewDirWS), input.normalWS)), _FresnelPower);

                // Color blending
                half4 waterColor = lerp(_ShallowColor, _DeepColor, fresnel);

                // Foam at edges (simplified)
                float edgeDist = frac(input.positionWS.x * 0.1 + _Time.y * 0.5);
                float foam = 0;
                if (edgeDist < _FoamThreshold * 0.1)
                    foam = 1.0 - (edgeDist / (_FoamThreshold * 0.1));

                half4 finalColor = lerp(waterColor, _FoamColor, foam * 0.3);

                // Specular from main light
                Light mainLight = GetMainLight();
                half3 halfDir = normalize(mainLight.direction + input.viewDirWS);
                float spec = pow(max(dot(normalWS, halfDir), 0.0), _SpecularPower) * _SpecularIntensity;
                finalColor.rgb += mainLight.color * spec;

                // Apply fog
                finalColor.rgb = MixFog(finalColor.rgb, input.fogCoord);

                return half4(finalColor.rgb, lerp(_ShallowColor.a, _DeepColor.a, fresnel));
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
