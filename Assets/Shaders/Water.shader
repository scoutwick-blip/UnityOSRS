Shader "RuneRealm/Water"
{
    // Stylized water shader with wave animation, reflections, and transparency.
    // Combines OSRS-clean aesthetic with Skyrim-style depth.

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
        _DepthFade ("Depth Fade Distance", Float) = 5
        _FoamThreshold ("Foam Threshold", Range(0, 2)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        CGPROGRAM
        #pragma surface surf StandardSpecular alpha:fade vertex:vert
        #pragma target 3.0

        fixed4 _ShallowColor;
        fixed4 _DeepColor;
        fixed4 _FoamColor;
        float _WaveSpeed;
        float _WaveAmplitude;
        float _WaveFrequency;
        sampler2D _NormalMap;
        float _NormalStrength;
        float _SpecularPower;
        float _SpecularIntensity;
        float _FresnelPower;
        float _DepthFade;
        float _FoamThreshold;

        sampler2D _CameraDepthTexture;

        struct Input
        {
            float2 uv_NormalMap;
            float3 worldPos;
            float3 viewDir;
            float4 screenPos;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

            // Vertex wave animation
            float wave1 = sin(_Time.y * _WaveSpeed + worldPos.x * _WaveFrequency) * _WaveAmplitude;
            float wave2 = sin(_Time.y * _WaveSpeed * 0.7 + worldPos.z * _WaveFrequency * 0.8) * _WaveAmplitude * 0.5;
            float wave3 = cos(_Time.y * _WaveSpeed * 0.5 + (worldPos.x + worldPos.z) * _WaveFrequency * 0.5) * _WaveAmplitude * 0.3;

            v.vertex.y += wave1 + wave2 + wave3;
        }

        void surf(Input IN, inout SurfaceOutputStandardSpecular o)
        {
            // Animated UV for normal map
            float2 uv1 = IN.uv_NormalMap + _Time.y * _WaveSpeed * 0.05 * float2(1, 0.5);
            float2 uv2 = IN.uv_NormalMap * 1.3 + _Time.y * _WaveSpeed * 0.03 * float2(-0.5, 1);

            float3 normal1 = UnpackNormal(tex2D(_NormalMap, uv1));
            float3 normal2 = UnpackNormal(tex2D(_NormalMap, uv2));
            float3 combinedNormal = normalize(float3(
                (normal1.xy + normal2.xy) * _NormalStrength,
                1
            ));

            // Fresnel effect
            float fresnel = pow(1.0 - saturate(dot(normalize(IN.viewDir), float3(0, 1, 0))), _FresnelPower);

            // Color blending based on depth
            fixed4 waterColor = lerp(_ShallowColor, _DeepColor, fresnel);

            // Foam at edges (simplified without depth texture)
            float foam = 0;
            float edgeDist = frac(IN.worldPos.x * 0.1 + _Time.y * 0.5);
            if (edgeDist < _FoamThreshold * 0.1)
                foam = 1.0 - (edgeDist / (_FoamThreshold * 0.1));

            fixed4 finalColor = lerp(waterColor, _FoamColor, foam * 0.3);

            o.Albedo = finalColor.rgb;
            o.Specular = float3(1, 1, 1) * 0.3;
            o.Smoothness = 0.9;
            o.Normal = combinedNormal;
            o.Alpha = lerp(_ShallowColor.a, _DeepColor.a, fresnel);
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
