#ifndef DICELAND_FOREST_LIGHTING_INCLUDED
#define DICELAND_FOREST_LIGHTING_INCLUDED

// Enabled only by the forest stage; shared sprite materials stay neutral in other scenes.
float _DiceLandForestLighting;
float4 _DiceLandForestOpenings;

float DiceLandForestNoise(float2 p)
{
    float2 cell = floor(p), f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float4 h = frac(sin(float4(dot(cell, float2(127.1, 311.7)),
        dot(cell + float2(1, 0), float2(127.1, 311.7)),
        dot(cell + float2(0, 1), float2(127.1, 311.7)),
        dot(cell + 1, float2(127.1, 311.7)))) * 43758.5453);
    return lerp(lerp(h.x, h.y, f.x), lerp(h.z, h.w, f.x), f.y);
}

half3 DiceLandForestLightTint(float3 positionWS)
{
    if (_DiceLandForestLighting <= 0.001)
        return half3(1, 1, 1);
    // Points along the actual sun ray share one canopy opening, on actors and floor alike.
    float3 sunDirection = GetMainLight().direction;
    float2 p = positionWS.xz - sunDirection.xz / max(0.2, sunDirection.y) * positionWS.y;
    float shaft = p.x + p.y * 0.42;
    float openCanopy = max(1.0 - smoothstep(1.7, 5.0, abs(shaft - _DiceLandForestOpenings.x)),
                          1.0 - smoothstep(1.0, 3.6, abs(shaft - _DiceLandForestOpenings.y)));
    float2 breeze = float2(sin(_Time.y * 0.24), cos(_Time.y * 0.19)) * 0.16;
    float leaves = smoothstep(0.24, 0.74, DiceLandForestNoise(p * 0.52 + breeze));
    float sunlight = saturate(openCanopy * (0.60 + leaves * 0.40) + (1.0 - openCanopy) * leaves * 0.13);
    half3 tint = lerp(half3(0.30, 0.43, 0.61), half3(1.65, 1.32, 0.86), sunlight);
    return lerp(half3(1, 1, 1), tint, saturate(_DiceLandForestLighting));
}

#endif
