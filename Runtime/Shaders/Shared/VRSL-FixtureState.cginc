#ifndef VRSL_FIXTURE_STATE_INCLUDED
#define VRSL_FIXTURE_STATE_INCLUDED

// A per-invocation snapshot of fixture inputs. Values still originate from
// the existing instanced material properties and source-specific adapters.
struct VRSLFixtureState
{
    uint channel;
    half pan;
    half tilt;
    half coneWidth;
    half intensity;
    half strobe;
    half goboSelection;
    half goboSpinSpeed;
    half audioAmplitude;
    half globalIntensity;
    half finalIntensity;
    half4 color;
    half4 emissionColor;
};

VRSLFixtureState VRSL_CreateFixtureState()
{
    VRSLFixtureState state;
    state.channel = 0;
    state.pan = 0.0;
    state.tilt = 0.0;
    state.coneWidth = 0.0;
    state.intensity = 1.0;
    state.strobe = 1.0;
    state.goboSelection = 0.0;
    state.goboSpinSpeed = 0.0;
    state.audioAmplitude = 1.0;
    state.globalIntensity = 1.0;
    state.finalIntensity = 1.0;
    state.color = half4(1.0, 1.0, 1.0, 1.0);
    state.emissionColor = half4(1.0, 1.0, 1.0, 1.0);
    return state;
}

#ifdef VRSL_DMX
VRSLFixtureState VRSL_LoadFixtureTransformState(VRSLFixtureState state)
{
    state.channel = getDMXChannel();
    state.coneWidth = getDMXConeWidth(state.channel);
    state.pan = GetPanValue(state.channel);
    state.tilt = GetTiltValue(state.channel);
    return state;
}

VRSLFixtureState VRSL_LoadFixtureLightState(VRSLFixtureState state)
{
    state.intensity = GetDMXIntensity(state.channel, 1.0);
    state.strobe = GetStrobeOutput(state.channel);
    state.color = GetDMXColor(state.channel);
    return state;
}

VRSLFixtureState VRSL_LoadFixtureGoboSelection(VRSLFixtureState state)
{
    state.goboSelection = getDMXGoboSelection(state.channel);
    return state;
}

VRSLFixtureState VRSL_LoadFixtureGoboSpin(VRSLFixtureState state)
{
    state.goboSpinSpeed = getGoboSpinSpeed(state.channel);
    return state;
}
#endif

#ifdef VRSL_AUDIOLINK
VRSLFixtureState VRSL_LoadFixtureTransformState(VRSLFixtureState state)
{
    state.coneWidth = getConeWidth();
    return state;
}

#ifndef RAW
VRSLFixtureState VRSL_LoadFixtureAudioState(VRSLFixtureState state)
{
    state.audioAmplitude = GetAudioReactAmplitude();
    return state;
}
#endif
#endif

VRSLFixtureState VRSL_LoadFixtureIntensityControls(VRSLFixtureState state)
{
    state.globalIntensity = getGlobalIntensity();
    state.finalIntensity = getFinalIntensity();
    return state;
}

VRSLFixtureState VRSL_LoadFixtureEmission(VRSLFixtureState state)
{
    state.emissionColor = getEmissionColor();
    return state;
}

#endif
