Shader "RuneRealm/SkyrimFog"
{
    // Atmospheric fog shader inspired by Skyrim's distance fog.
    // URP-compatible with exponential height fog and sun scattering.

    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (0.65, 0.7, 0.75, 1)
        _FogDensity ("Fog Density", Range(0, 0.1)) = 0.004
        _FogHeight ("Fog Height", Float) = 30
        _FogFalloff ("Fog Height Falloff", Range(0.001, 2)) = 0.5
        _SunDirection ("Sun Direction", Vector) = (0.3, 0.8, 0.5, 0)
        _SunFogColor ("Sun Fog Color", Color) = (1, 0.9, 0.7, 1)
        _SunFogIntensity ("Sun Fog Intensity", Range(0, 2)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _FogColor;
                float _FogDensity;
                float _FogHeight;
                float _FogFalloff;
                float4 _SunDirection;
                half4 _SunFogColor;
                float _SunFogIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                float fogCoord : TEXCOORD4;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogCoord = ComputeFogFactor(posInputs.positionCS.z);

                // Custom atmospheric fog
                float dist = length(output.positionWS - _WorldSpaceCameraPos);
                float distFog = 1.0 - exp(-dist * _FogDensity * _FogDensity);
                float heightFog = exp(-(output.positionWS.y - _FogHeight) * _FogFalloff);
                heightFog = saturate(heightFog);
                output.fogFactor = saturate(distFog * heightFog);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // URP main light
                Light mainLight = GetMainLight();
                half3 diffuse = LightingLambert(mainLight.color * mainLight.distanceAttenuation, mainLight.direction, input.normalWS);
                half3 litColor = texColor.rgb * (diffuse + unity_AmbientSky.rgb);

                // Sun scattering in fog
                float3 viewDir = normalize(input.positionWS - _WorldSpaceCameraPos);
                float sunAmount = max(dot(viewDir, normalize(_SunDirection.xyz)), 0.0);
                sunAmount = pow(sunAmount, 8.0);

                half3 fogColor = lerp(_FogColor.rgb, _SunFogColor.rgb, sunAmount * _SunFogIntensity);

                // Apply custom atmospheric fog
                half3 finalColor = lerp(litColor, fogColor, input.fogFactor);

                // Also apply Unity's built-in fog
                finalColor = MixFog(finalColor, input.fogCoord);

                return half4(finalColor, texColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
