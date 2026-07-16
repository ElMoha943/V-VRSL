#ifndef VRSL_PROJECTION_HELPERS_INCLUDED
#define VRSL_PROJECTION_HELPERS_INCLUDED

inline float VRSL_SampleProjectionDepth(float2 screenUV)
{
#if defined(_MULTISAMPLEDEPTH)
    float2 texelSize = _CameraDepthTexture_TexelSize.xy;
    float horizontalDepth = min(
        SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV + float2(texelSize.x, 0.0)),
        SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV - float2(texelSize.x, 0.0)));
    float verticalDepth = min(
        SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV + float2(0.0, texelSize.y)),
        SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV - float2(0.0, texelSize.y)));
    return min(horizontalDepth, verticalDepth);
#else
    return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);
#endif
}

inline float VRSL_ProjectionLinear01Depth(float rawDepth, float correctionW)
{
    float eyeDepth = VRSL_CorrectedLinearEyeDepth(rawDepth, correctionW);
    eyeDepth = (1.0 - (eyeDepth * _ZBufferParams.w)) / (eyeDepth * _ZBufferParams.z);
    return Linear01Depth(eyeDepth);
}

inline float3 VRSL_ProjectionWorldPosition(float depth, float3 ray)
{
    return mul(unity_CameraToWorld, float4(ray * depth, 1.0)).xyz;
}

inline float3 VRSL_ProjectionObjectPosition(float3 worldPosition)
{
    return mul(unity_WorldToObject, float4(worldPosition, 1.0)).xyz;
}

inline half3 VRSL_RotateProjectionPosition(half3 position, half3 origin, half4 rotationSinCos)
{
    position -= origin;

    half panSin = rotationSinCos.x;
    half panCos = rotationSinCos.y;
    half tiltSin = rotationSinCos.z;
    half tiltCos = rotationSinCos.w;

    half rotatedX = panCos * position.x + panSin * position.y;
    half rotatedY = -panSin * position.x + panCos * position.y;
    half rotatedZ = position.z;

    position.x = rotatedX;
    position.y = tiltCos * rotatedY + tiltSin * rotatedZ;
    position.z = -tiltSin * rotatedY + tiltCos * rotatedZ;
    return position + origin;
}

inline half2 VRSL_RotateProjectionUV(half2 uv, half2 rotationSinCos)
{
    uv -= half2(0.5, 0.5);
    uv = mul(uv, half2x2(
        rotationSinCos.y, -rotationSinCos.x,
        rotationSinCos.x, rotationSinCos.y));
    return uv + half2(0.5, 0.5);
}

inline float VRSL_ProjectionFadeMultiplier(float uvDistance, float fadeScale, float fadeCurve)
{
    return 1.0 - saturate(pow(uvDistance * fadeScale, fadeCurve));
}

inline float VRSL_ProjectionReciprocalFalloff(float distanceFromOrigin, float constantTerm, float linearTerm, float quadraticTerm)
{
    return rcp(constantTerm + linearTerm * distanceFromOrigin + quadraticTerm * distanceFromOrigin * distanceFromOrigin);
}

inline float VRSL_ProjectionDistanceFadeMultiplier(float distanceFromOrigin, float fadePosition)
{
    return 1.0 - smoothstep(distanceFromOrigin, 0.0, fadePosition);
}

#endif
