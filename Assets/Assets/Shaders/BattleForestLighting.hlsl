#ifndef DICELAND_FOREST_LIGHTING_INCLUDED
#define DICELAND_FOREST_LIGHTING_INCLUDED

// Enabled only by the forest stage; shared sprite materials stay neutral in other scenes.
float _DiceLandForestLighting;
// xy: light-shaft openings, z: canopy occlusion, w: shared sunlight motion.
float4 _DiceLandForestOpenings;
// Authored sun fallback when a low URP tier disables main-light shading.
float4 _DiceLandForestSunDirection;
half4 _DiceLandForestSunColor;

float3 DiceLandForestSunDirection()
{
    Light sun = GetMainLight();
    return dot(sun.color, half3(1, 1, 1)) > 0.0001 || _DiceLandForestSunDirection.w < 0.5
        ? sun.direction : _DiceLandForestSunDirection.xyz;
}

half3 DiceLandForestSunColor()
{
    half3 color = GetMainLight().color;
    return dot(color, half3(1, 1, 1)) > 0.0001 ? color : _DiceLandForestSunColor.rgb;
}

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

float DiceLandForestSunSweep(float opening)
{
    float drift = sin(_Time.y * 0.36 + opening * 0.17) * 0.44;
    drift += sin(_Time.y * 0.73 + opening * 0.29) * 0.15;
    return drift * saturate(_DiceLandForestOpenings.w);
}

float2 DiceLandForestCanopyBreeze()
{
    float2 legacy = float2(sin(_Time.y * 0.24), cos(_Time.y * 0.19)) * 0.16;
    float2 breeze = float2(sin(_Time.y * 0.42) * 0.55 + sin(_Time.y * 0.93 + 2.1) * 0.09,
        cos(_Time.y * 0.31) * 0.44 + sin(_Time.y * 0.67) * 0.08);
    return lerp(legacy, breeze, saturate(_DiceLandForestOpenings.w));
}

float2 DiceLandForestProjectedGround(float3 positionWS)
{
    float3 sunDirection = DiceLandForestSunDirection();
    return positionWS.xz - sunDirection.xz / max(0.2, sunDirection.y) * positionWS.y;
}

float DiceLandForestCanopyTransmission(float2 p, float2 breeze)
{
    // Large leaf clusters establish connected shade; smaller leaves break their edges.
    // Projected world coordinates keep the same pattern on the floor, plants and actors.
    float2 q = p + breeze;
    float clusters = DiceLandForestNoise(q * float2(0.34, 0.46) + float2(13.7, 4.1)) * 0.72;
    clusters += DiceLandForestNoise(q * 1.65 + float2(2.6, 19.3)) * 0.28;
    float gaps = smoothstep(0.38, 0.62, clusters);

    // Branch silhouettes stay rooted while only the leaves drift. Two unequal limbs
    // prevent the opening from reading as a row of identical circular spotlights.
    float limb = p.y + p.x * 0.24 + sin(p.x * 0.31) * 0.42;
    float branch = 1.0 - smoothstep(0.17, 0.42, abs(limb + 1.4));
    float fork = p.y - p.x * 0.46 - 2.2 + sin(p.x * 0.23) * 0.28;
    branch = max(branch, (1.0 - smoothstep(0.10, 0.29, abs(fork))) * 0.78);
    return lerp(0.18, 1.0, gaps) * (1.0 - branch * 0.78);
}

float DiceLandForestSunVisibility(float3 positionWS)
{
    if (_DiceLandForestLighting <= 0.001)
        return 0.0;
    // Points along the actual sun ray share one canopy opening, on actors and floor alike.
    float2 p = DiceLandForestProjectedGround(positionWS);
    float shaft = p.x + p.y * 0.42;
    float openCanopy = max(1.0 - smoothstep(1.7, 5.0, abs(shaft - _DiceLandForestOpenings.x - DiceLandForestSunSweep(_DiceLandForestOpenings.x))),
                          1.0 - smoothstep(1.0, 3.6, abs(shaft - _DiceLandForestOpenings.y - DiceLandForestSunSweep(_DiceLandForestOpenings.y))));
    float2 breeze = DiceLandForestCanopyBreeze();
    float leaves = smoothstep(0.24, 0.74, DiceLandForestNoise(p * 0.52 + breeze));
    float sunlight = saturate(openCanopy * (0.60 + leaves * 0.40) + (1.0 - openCanopy) * leaves * 0.13);
    if (_DiceLandForestOpenings.z > 0.001)
        sunlight *= lerp(1.0, DiceLandForestCanopyTransmission(p, breeze), saturate(_DiceLandForestOpenings.z));
    return sunlight;
}

half3 DiceLandForestLightTint(float3 positionWS)
{
    if (_DiceLandForestLighting <= 0.001)
        return half3(1, 1, 1);
    float sunlight = DiceLandForestSunVisibility(positionWS);
    half3 tint = lerp(half3(0.21, 0.32, 0.48), half3(1.65, 1.32, 0.86), sunlight);
    return lerp(half3(1, 1, 1), tint, saturate(_DiceLandForestLighting));
}

#endif
