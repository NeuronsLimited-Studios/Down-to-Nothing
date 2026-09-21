Shader "Hidden/BlackHoleLens"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "BlackHoleLens"

            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _LensParams;   // xy = center of the black hole (uv), z = how far the distortion reaches (the screen height is 1), w = strength from 0 to 1
            float4 _LensExtra;    // x = screen aspect, y = how much everything is pushed away from the hole

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float4 original = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float reach = _LensParams.z;
                float strength = _LensParams.w;

                if (strength <= 0.0001 || reach <= 0.00001)
                    return original;

                float2 aspect = float2(_LensExtra.x, 1.0);
                float2 offset = (uv - _LensParams.xy) * aspect;   // distance from the hole, in screen heights
                float radius = length(offset);

                if (radius >= reach)
                    return original;

                // Everything near the hole is looked at from a bit closer to it, so it seems pushed away.
                // The push is strongest close to the hole and fades smoothly to nothing at the reach,
                // and it never squeezes a whole area into a single point, so no artifacts can appear
                float falloff = 1.0 - radius / reach;
                float sourceRadius = radius * (1.0 - _LensExtra.y * strength * falloff * falloff);
                float2 direction = offset / max(radius, 0.00001);
                float2 sourceUV = _LensParams.xy + direction * sourceRadius / aspect;

                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sourceUV);
            }
            ENDHLSL
        }
    }
}
