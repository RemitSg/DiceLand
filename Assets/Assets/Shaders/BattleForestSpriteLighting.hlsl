#ifndef DICELAND_FOREST_SPRITE_LIGHTING_INCLUDED
#define DICELAND_FOREST_SPRITE_LIGHTING_INCLUDED

// A broad painted surface, not a reconstructed anatomical normal map.
// World-space bounds keep lighting stable when the source sprite is mirrored or atlas packed.
float3 DiceLandForestSculptedNormal(float3 positionWS, float4 bounds, float weight)
{
    float2 uv = saturate((positionWS.xy - bounds.xy) / max(bounds.zw, float2(0.001, 0.001)));
    float2 shape = float2((uv.x - 0.5) * 2.4, (uv.y - 0.43) * 0.95);
    return normalize(float3(shape * weight, -0.85));
}

float3 DiceLandForestPaintedKey(float3 sunDirection)
{
    // The illustration faces the viewer. Wrap the canopy key onto its front-facing surface,
    // retaining the actual sun's horizontal/vertical direction used by floor light patches.
    return float3(sunDirection.xy, -abs(sunDirection.z));
}

half3 DiceLandForestSurfaceTint(float keyDiffuse)
{
    return lerp(half3(0.78, 0.88, 1.06), half3(1.07, 1.00, 0.88), saturate(keyDiffuse * 1.1));
}

half3 DiceLandForestIndirectFill(half3 albedo, float3 normalWS)
{
    // Painted dark folds need indirect illumination outside the direct canopy mask.
    // A saturating scalar response keeps black at zero and luminance monotonic:
    // L + k * toe * L / (toe + L), rather than a flat grey minimum or a hard dark mask.
    float luminance = dot(albedo, float3(0.2126, 0.7152, 0.0722));
    float relativeFill = 2.4 * 0.035 / (0.035 + max(0.0, luminance));
    half3 bounce = lerp(half3(0.68, 0.61, 0.45), half3(0.62, 0.76, 1.00),
        saturate(normalWS.y * 0.8 + 0.65));
    return albedo * bounce * relativeFill;
}

half3 DiceLandForestPigmentAlbedo(half3 albedo)
{
    // Lift dark painted pigments before illumination without flattening their folds.
    // Scalar gain preserves hue, zero remains zero, and bright pigments keep headroom.
    float luminance = max(0.0, dot(albedo, float3(0.2126, 0.7152, 0.0722)));
    float gain = 1.0 + 0.055 / (0.025 + luminance)
        * (1.0 - smoothstep(0.22, 0.65, luminance));
    float peak = max(albedo.r, max(albedo.g, albedo.b));
    gain = min(gain, max(1.0, rcp(max(0.0001, peak))));
    return albedo * gain;
}

float3 DiceLandForestHighlightShoulder(float3 lighting)
{
    // Keep painted silver/gold planes apart before the scene's display tonemapper.
    // A shared RGB scale preserves hue; the curve and its slope meet at 0.9.
    float peak = max(lighting.r, max(lighting.g, lighting.b));
    float excess = max(0.0, peak - 0.9);
    float compressedExcess = excess / (1.0 + 0.8 * excess);
    float scale = 1.0 - (excess - compressedExcess) / max(0.0001, peak);
    return lighting * scale;
}

#endif
