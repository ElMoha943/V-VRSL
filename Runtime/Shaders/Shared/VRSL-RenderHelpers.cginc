#ifndef VRSL_RENDER_HELPERS_INCLUDED
#define VRSL_RENDER_HELPERS_INCLUDED

// Mirror-compatible projection correction originally provided by DJ Lukis.
inline float4 VRSL_CalculateFrustumCorrection()
{
    float x1 = -UNITY_MATRIX_P._31 / (UNITY_MATRIX_P._11 * UNITY_MATRIX_P._34);
    float x2 = -UNITY_MATRIX_P._32 / (UNITY_MATRIX_P._22 * UNITY_MATRIX_P._34);
    return float4(x1, x2, 0.0, UNITY_MATRIX_P._33 / UNITY_MATRIX_P._34 + x1 * UNITY_MATRIX_P._13 + x2 * UNITY_MATRIX_P._23);
}

inline float VRSL_CorrectedLinearEyeDepth(float z, float correction)
{
    #if UNITY_REVERSED_Z
        if (z == 0.0)
            return LinearEyeDepth(z);
    #endif
    return 1.0 / (z / UNITY_MATRIX_P._34 + correction);
}

inline float VRSL_GetDitherThreshold(float4 screenPosition)
{
    float2 pixelPosition = screenPosition.xy / screenPosition.w;
    pixelPosition *= _ScreenParams.xy;

    float thresholds[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    int index = (int)((uint(pixelPosition.x) % 4) * 4 + uint(pixelPosition.y) % 4);
    return thresholds[index];
}

#endif
