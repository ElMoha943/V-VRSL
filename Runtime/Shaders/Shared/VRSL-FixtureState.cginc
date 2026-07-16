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

#ifdef VRSL_DMX
void VRSL_LoadFixtureTransformState(inout VRSLFixtureState state)
{
    state.channel = getDMXChannel();
    state.coneWidth = getDMXConeWidth(state.channel);
    state.pan = GetPanValue(state.channel);
    state.tilt = GetTiltValue(state.channel);
}

void VRSL_LoadFixtureLightState(inout VRSLFixtureState state)
{
    state.intensity = GetDMXIntensity(state.channel, 1.0);
    state.strobe = GetStrobeOutput(state.channel);
    state.color = GetDMXColor(state.channel);
}

void VRSL_LoadFixtureGoboSelection(inout VRSLFixtureState state)
{
    state.goboSelection = getDMXGoboSelection(state.channel);
}

void VRSL_LoadFixtureGoboSpin(inout VRSLFixtureState state)
{
    state.goboSpinSpeed = getGoboSpinSpeed(state.channel);
}
#endif

#ifdef VRSL_AUDIOLINK
void VRSL_LoadFixtureTransformState(inout VRSLFixtureState state)
{
    state.coneWidth = getConeWidth();
}

#ifndef RAW
void VRSL_LoadFixtureAudioState(inout VRSLFixtureState state)
{
    state.audioAmplitude = GetAudioReactAmplitude();
}
#endif
#endif

void VRSL_LoadFixtureIntensityControls(inout VRSLFixtureState state)
{
    state.globalIntensity = getGlobalIntensity();
    state.finalIntensity = getFinalIntensity();
}

void VRSL_LoadFixtureEmission(inout VRSLFixtureState state)
{
    state.emissionColor = getEmissionColor();
}

#endif
