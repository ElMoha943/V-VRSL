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

#endif
