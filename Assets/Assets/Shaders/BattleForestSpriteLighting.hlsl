#ifndef DICELAND_FOREST_SPRITE_LIGHTING_INCLUDED
#define DICELAND_FOREST_SPRITE_LIGHTING_INCLUDED

float2 DiceLandForestSunEdgeOffset(float3 worldDx, float3 worldDy, float2 uvDx, float2 uvDy, float3 sun, float pixels)
{
    // Derivatives follow the rendered silhouette, scale and render target orientation.
    float2 direction = float2(dot(worldDx, sun), dot(worldDy, sun));
    direction *= rsqrt(max(1e-20, dot(direction, direction)));
    return (uvDx * direction.x + uvDy * direction.y) * pixels;
}

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

half3 DiceLandForestMaterialSpecular(half3 albedo, half3 material, float normalHalf)
{
    // Authored R = skin, G = metal, B = roughness. Cloth keeps a very weak broad lobe.
    float skin = saturate(material.r), metal = saturate(material.g);
    float roughness = clamp(material.b, 0.2, 1.0);
    float broad = pow(saturate(normalHalf), lerp(48.0, 12.0, roughness));
    // A continuous lobe keeps broad metal planes below the aligned glint peak.
    // Reuse the roughness lobe instead of a smoothstep with a large flat plateau.
    float polished = broad;
    half3 reflectance = lerp(half3(0.018,0.018,0.018), half3(0.035,0.028,0.023), skin);
    half3 metalColor = lerp(half3(0.48,0.48,0.48), albedo * 0.70 + 0.08, 0.55);
    float pigment = smoothstep(0.008, 0.05, dot(albedo, half3(0.2126,0.7152,0.0722)));
    return lerp(reflectance * broad, metalColor * polished * 1.65, metal) * pigment;
}

half3 DiceLandForestMetalEnvironment(half3 albedo, half3 material, float3 normalWS, float3 viewDirWS)
{
    // Low-frequency sky/ground reflection from the renderer's existing SH lighting.
    // This complements direct glints; it is not a sharp reflection-probe substitute.
    float metal = saturate(material.g) * (1.0 - saturate(material.r));
    float roughness = clamp(material.b, 0.2, 1.0);
    float3 reflected = reflect(-viewDirWS, normalWS);
    float3 lookup = SafeNormalize(lerp(reflected, normalWS, roughness * roughness * 0.6));
    half3 environment = SampleSH(lookup);
    half3 f0 = saturate(lerp(half3(0.48,0.48,0.48), albedo * 0.70 + 0.08, 0.55));
    float grazing = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 5.0);
    half3 fresnel = f0 + (1.0 - f0) * grazing;
    float pigment = smoothstep(0.008, 0.05, dot(albedo, half3(0.2126,0.7152,0.0722)));
    return environment * fresnel * (metal * pigment * (1.0 - 0.65 * roughness));
}

float DiceLandForestMaterialDiffuse(half3 material, float incidence)
{
    // Soft wrapped skin response; matte fabric retains the painted dark folds.
    float wrapped = saturate((incidence + material.r * 0.24) / (1.0 + material.r * 0.24));
    return (0.30 + 0.96 * wrapped) * lerp(0.94, 0.55, material.g);
}

half3 DiceLandForestSkinScatter(half3 albedo, half3 material, float incidence)
{
    // A colored wrap near the terminator, not thickness-based subsurface transport.
    // https://developer.nvidia.com/gpugems/gpugems/part-iii-materials/chapter-16-real-time-approximations-subsurface-scattering
    float skin = saturate(material.r) * (1.0 - saturate(material.g));
    float transition = smoothstep(-0.28, 0.12, incidence)
        * (1.0 - smoothstep(0.30, 0.86, incidence));
    float pigment = smoothstep(0.01, 0.09, dot(albedo, half3(0.2126, 0.7152, 0.0722)));
    return half3(1.0, 0.28, 0.12) * (skin * transition * pigment * 0.55);
}

half3 DiceLandForestFabricSheen(half3 albedo, half3 material, float normalHalf, float normalView, float incidence)
{
    // Charlie distribution and smooth cloth visibility, with a small artistic gain.
    // Model reference: https://google.github.io/filament/main/filament.html#materialsystem/clothmodel
    float cloth = (1.0-saturate(material.r)) * (1.0-saturate(material.g)) * smoothstep(0.92,1.0,material.b);
    float inverseRoughness = rcp(clamp(material.b,0.5,1.0));
    float sineSquared = max(0.0078125,1.0-saturate(normalHalf)*saturate(normalHalf));
    float distribution = (2.0+inverseRoughness) * pow(sineSquared,0.5*inverseRoughness) / (2.0*PI);
    float nl=saturate(incidence),nv=saturate(normalView);
    float visibility = rcp(max(0.04,4.0*(nl+nv-nl*nv)));
    half3 sheenColor=sqrt(max(half3(0,0,0),albedo));
    return sheenColor * (cloth * distribution * visibility * nl * 1.35);
}

float DiceLandForestMaterialRim(half3 material)
{
    return lerp(lerp(0.28, 0.62, material.r), 1.35, material.g);
}

#endif
