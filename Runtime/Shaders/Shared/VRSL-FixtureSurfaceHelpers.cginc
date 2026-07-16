#ifndef VRSL_FIXTURE_SURFACE_HELPERS_INCLUDED
#define VRSL_FIXTURE_SURFACE_HELPERS_INCLUDED

#if defined(VRSL_AUDIOLINK)
inline float VRSL_GetAudioFixtureIntensity()
{
    return getGlobalIntensity() * getFinalIntensity() * _UniversalIntensity;
}

inline half3 VRSL_ApplyAudioFixtureControls(half3 emission)
{
    return emission * GetAudioReactAmplitude() * VRSL_GetAudioFixtureIntensity();
}
#endif

#if defined(FIXTURE_EMIT)
inline float3 VRSL_GetDecorativeFixtureEmission(float2 uv)
{
    return tex2D(_DecorativeEmissiveMap, uv).rgb * _DecorativeEmissiveMapStrength;
}

#if defined(VRSL_DMX)
inline float3 VRSL_GetDMXFixtureEmission(float4 fixtureColor, float fixtureIntensity, float strobeValue)
{
    bool dmxEnabled = isDMX() == 1;
    float strobe = isStrobe() == 1 ? strobeValue : 1.0;
    float4 emission = getEmissionColor() * strobe;

    if(dmxEnabled)
    {
        emission *= fixtureColor;
    }

    emission *= _FixtureMaxIntensity * 1500.0;
    emission = clamp(emission, 0.0, _LensMaxBrightness * 100.0);

    const float minimumIntensity = 0.025;
    if(dmxEnabled)
    {
        if(all(fixtureColor >= float4(minimumIntensity, minimumIntensity, minimumIntensity, 1.0)) || fixtureIntensity >= minimumIntensity)
        {
            emission *= lerp(1.0, _FixutreIntensityMultiplier, pow(fixtureIntensity, 1.9));
        }
        else
        {
            emission = float4(0.0, 0.0, 0.0, 1.0);
        }
    }

    emission.rgb *= getGlobalIntensity() * getFinalIntensity() * _UniversalIntensity;
    return emission.rgb;
}
#endif

#if defined(VRSL_AUDIOLINK)
inline float3 VRSL_GetMovingAudioFixtureEmission(float washMask)
{
    float fixtureIntensity = VRSL_GetAudioFixtureIntensity();
    float4 emission = getEmissionColor() * (_FixtureMaxIntensity * 1500.0);
    emission = clamp(emission, 0.0, _LensMaxBrightness * 100.0 * fixtureIntensity);

#if !defined(RAW)
    emission.rgb *= GetAudioReactAmplitude();
#endif
    emission.rgb *= fixtureIntensity;

#if defined(WASH)
    if(washMask > 0.0)
    {
        emission.rgb = saturate(emission.rgb) - 0.25;
    }
#endif

    float averageLighting = dot(emission.rgb, float3(0.333333, 0.333333, 0.333333));
#if defined(RAW)
    float saturationBlend = _Saturation * _Saturation;
#else
    float saturationBlend = _Saturation;
#endif
    return lerp(emission.rgb, averageLighting.xxx, saturationBlend);
}
#endif
#endif

#endif
