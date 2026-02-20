Shader "RuneRealm/WindGrass"
{
    // Grass shader with wind animation and distance fade.
    // Creates natural-looking grass movement in Skyrim style.

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
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 100
        Cull Off

        CGPROGRAM
        #pragma surface surf Lambert alphatest:_Cutoff vertex:vert addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _WindStrength;
        float _WindSpeed;
        float4 _WindDirection;
        float _SwayAmount;
        float _FadeStart;
        float _FadeEnd;

        // Global wind from WeatherSystem
        float4 _GlobalWindDirection;
        float _GlobalWindStrength;

        struct Input
        {
            float2 uv_MainTex;
            float distToCamera;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

            // Use global wind if available, otherwise use local
            float3 wind = _GlobalWindStrength > 0
                ? _GlobalWindDirection.xyz * _GlobalWindStrength
                : _WindDirection.xyz * _WindStrength;

            // Only move vertices above ground (using vertex color or UV as height mask)
            float heightMask = v.texcoord.y;

            // Wind animation
            float time = _Time.y * _WindSpeed;
            float windWave = sin(time + worldPos.x * 0.5 + worldPos.z * 0.3) * 0.5 + 0.5;
            float windGust = sin(time * 0.7 + worldPos.x * 0.3) * 0.3;

            float3 windOffset = wind * (windWave + windGust) * _SwayAmount * heightMask;
            v.vertex.xyz += mul(unity_WorldToObject, float4(windOffset, 0)).xyz;

            // Distance to camera for fading
            o.distToCamera = length(worldPos - _WorldSpaceCameraPos);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // Distance fade
            float fade = 1.0 - saturate((IN.distToCamera - _FadeStart) / (_FadeEnd - _FadeStart));

            o.Albedo = c.rgb;
            o.Alpha = c.a * fade;
        }
        ENDCG
    }
    FallBack "Transparent/Cutout/Diffuse"
}
