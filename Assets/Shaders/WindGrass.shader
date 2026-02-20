Shader "RuneRealm/WindGrass"
{
    // Grass shader with wind animation and distance fade.
    // URP-compatible with alpha cutout for natural-looking grass.

    Properties
    {
        _MainTex ("Grass Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (0.3, 0.5, 0.2, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _WindStrength ("Wind Strength", Range(0, 2)) = 0.5
        _WindSpeed ("Wind Speed", Range(0, 5)) = 1.5
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0.5, 0)
        _SwayAmount ("Sway Amount", Range(0, 1)) = 0.3
        _FadeStart ("Fade Start Distance", Float) = 40
        _FadeEnd ("Fade End Distance", Float) = 60
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Cull Off

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float _Cutoff;
                float _WindStrength;
                float _WindSpeed;
                float4 _WindDirection;
                float _SwayAmount;
                float _FadeStart;
                float _FadeEnd;
            CBUFFER_END

            // Global wind from WeatherSystem
            float4 _GlobalWindDirection;
            float _GlobalWindStrength;

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
                float3 normalWS : TEXCOORD1;
                float distToCamera : TEXCOORD2;
                float fogCoord : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                // Use global wind if available, otherwise use local
                float3 wind = _GlobalWindStrength > 0
                    ? _GlobalWindDirection.xyz * _GlobalWindStrength
                    : _WindDirection.xyz * _WindStrength;

                // Only move vertices above ground (UV.y as height mask)
                float heightMask = input.uv.y;

                // Wind animation
                float time = _Time.y * _WindSpeed;
                float windWave = sin(time + worldPos.x * 0.5 + worldPos.z * 0.3) * 0.5 + 0.5;
                float windGust = sin(time * 0.7 + worldPos.x * 0.3) * 0.3;

                float3 windOffset = wind * (windWave + windGust) * _SwayAmount * heightMask;
                worldPos += windOffset;

                output.positionCS = TransformWorldToHClip(worldPos);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.distToCamera = length(worldPos - _WorldSpaceCameraPos);
                output.fogCoord = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

                // Distance fade
                float fade = 1.0 - saturate((input.distToCamera - _FadeStart) / (_FadeEnd - _FadeStart));
                texColor.a *= fade;

                // Alpha cutout
                clip(texColor.a - _Cutoff);

                // Simple lighting
                Light mainLight = GetMainLight();
                half3 diffuse = LightingLambert(mainLight.color * mainLight.distanceAttenuation, mainLight.direction, input.normalWS);
                half3 litColor = texColor.rgb * (diffuse + unity_AmbientSky.rgb);

                litColor = MixFog(litColor, input.fogCoord);

                return half4(litColor, texColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
