Shader "RuneRealm/SkyrimFog"
{
    // Atmospheric fog shader inspired by Skyrim's distance fog.
    // Applies exponential height fog with color blending.

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
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _FogColor;
        float _FogDensity;
        float _FogHeight;
        float _FogFalloff;
        float4 _SunDirection;
        fixed4 _SunFogColor;
        float _SunFogIntensity;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float fogFactor;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.worldPos = worldPos;

            // Calculate distance fog
            float dist = length(worldPos - _WorldSpaceCameraPos);
            float distFog = 1.0 - exp(-dist * _FogDensity * _FogDensity);

            // Height fog
            float heightFog = exp(-(worldPos.y - _FogHeight) * _FogFalloff);
            heightFog = saturate(heightFog);

            o.fogFactor = saturate(distFog * heightFog);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);

            // Sun scattering in fog
            float3 viewDir = normalize(IN.worldPos - _WorldSpaceCameraPos);
            float sunAmount = max(dot(viewDir, normalize(_SunDirection.xyz)), 0.0);
            sunAmount = pow(sunAmount, 8.0);

            fixed3 fogColor = lerp(_FogColor.rgb, _SunFogColor.rgb, sunAmount * _SunFogIntensity);

            // Apply fog
            o.Albedo = lerp(c.rgb, fogColor, IN.fogFactor);
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
