#ifndef DICELAND_FOREST_LOCAL_LIGHTS_INCLUDED
#define DICELAND_FOREST_LOCAL_LIGHTS_INCLUDED

#if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
bool DiceLandForestIsPunctualLight(uint index)
{
    // Low tiers disable the main-light slot. URP may then put the authored sun
    // in the additional list; the forest already supplies that sun separately.
    #if USE_CLUSTERED_LIGHTING
        int lightIndex = index;
    #else
        int lightIndex = GetPerObjectLightIndex(index);
    #endif
    #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
        return _AdditionalLightsBuffer[lightIndex].position.w > 0.5;
    #else
        return _AdditionalLightsPosition[lightIndex].w > 0.5;
    #endif
}
#endif

#endif
