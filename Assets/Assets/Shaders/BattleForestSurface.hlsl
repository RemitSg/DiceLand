#ifndef DICELAND_FOREST_SURFACE_INCLUDED
#define DICELAND_FOREST_SURFACE_INCLUDED

// Both meshes sample the same world-space surface through the terrace/woodland margin.
// A material change at the mesh boundary must not reveal a straight cut in the scenery.
#include "Assets/Assets/Shaders/BattleForestLighting.hlsl"
CBUFFER_START(UnityPerMaterial)
half4 _SoilColor, _MossColor, _StoneColor;
float _TextureScale, _TextureWeight, _StoneTextureWeight, _PavingScale, _PavingGrowth, _LowerStepGain;
float _PavingReliefStrength, _PavingGlintStrength;
CBUFFER_END
TEXTURE2D(_GroundMap); SAMPLER(sampler_GroundMap);
TEXTURE2D(_StoneMap); SAMPLER(sampler_StoneMap);
TEXTURE2D(_PavingReliefMap); SAMPLER(sampler_PavingReliefMap);

half3 ForestEarthAlbedo(float2 p)
{
    float clearing = 1.0 - smoothstep(3.0, 14.0, abs(p.y) + DiceLandForestNoise(p * 0.19) * 3.0);
    half3 fallback = lerp(_MossColor.rgb, _SoilColor.rgb, clearing * 0.9);
    half3 painted = SAMPLE_TEXTURE2D(_GroundMap, sampler_GroundMap, p * _TextureScale).rgb;
    painted *= lerp(half3(0.63, 0.82, 0.61), half3(1.02, 1.00, 0.91), clearing);
    return lerp(fallback, painted, _TextureWeight);
}

half3 ForestPavingAlbedo(float2 p)
{
    half3 painted = SAMPLE_TEXTURE2D(_StoneMap, sampler_StoneMap, p * _PavingScale).rgb;
    return lerp(_StoneColor.rgb, painted, _StoneTextureWeight);
}

float ForestPavingCoverage(float2 p)
{
    float edge = 9.0 + (DiceLandForestNoise(float2(p.x * 0.27, 12.4)) - 0.5) * 6.0;
    edge += sin(p.x * 0.31) * 0.8;
    float breakup = (DiceLandForestNoise(p * 0.85) - 0.5) * 1.8;
    return 1.0 - smoothstep(edge - 3.0, edge + 3.0, p.y + breakup);
}

float ForestSurfacePaving(float2 p)
{
    // Broad, repeatable earthen islands break up the paving carpet without adding
    // texture fetches or using the battle RNG. Both adjoining meshes share this mask.
    float growth = DiceLandForestNoise(p * float2(0.19, 0.28) + float2(7.3, 21.6));
    growth += (DiceLandForestNoise(p * 0.73) - 0.5) * 0.16;
    float edge = 1.02 - saturate(_PavingGrowth) * 0.65;
    float paving = ForestPavingCoverage(p) * (1.0 - smoothstep(edge, edge + 0.07, saturate(growth)));
    return paving;
}

half3 ForestSurfaceAlbedo(float2 p, float paving)
{
    return lerp(ForestEarthAlbedo(p), ForestPavingAlbedo(p), paving);
}

half3 ForestSurfaceLighting(float3 world, half3 normal)
{
    Light light = GetMainLight(TransformWorldToShadowCoord(world));
    half top = saturate(normal.y);
    half3 fill = max(SampleSH(normal), lerp(half3(0.19, 0.21, 0.24), half3(0.07, 0.09, 0.13), top));
    half3 illumination = fill + light.color * saturate(dot(normal, light.direction)) * lerp(0.4, 1.0, light.shadowAttenuation);
    return illumination * lerp(half3(0.85, 0.92, 1.0), DiceLandForestLightTint(world), top);
}

half3 ForestSurfaceShading(float3 world, half3 geometricNormal, half3 albedo, float paving)
{
    float top = saturate(geometricNormal.y);
    float surfaceWeight = saturate(_PavingReliefStrength) * top * saturate(_DiceLandForestLighting);
    float weight = surfaceWeight * paving * saturate(_StoneTextureWeight);
    half3 result = half3(0,0,0);
    if (surfaceWeight > 0.001)
    {
        half4 relief = SAMPLE_TEXTURE2D(_PavingReliefMap, sampler_PavingReliefMap, world.xz * _PavingScale);
        float2 xz = relief.rg * 2.0 - 1.0;
        float3 detailNormal = float3(xz.x, sqrt(max(0.02, 1.0 - dot(xz,xz))), xz.y);
        float3 normal = normalize(lerp(geometricNormal, detailNormal, weight));
        float cavity = lerp(1.0, relief.b, weight);
        Light light = GetMainLight(TransformWorldToShadowCoord(world));
        // The lowest tiers disable URP's main light; the stage already supplies its
        // authored sun to actor materials and air scattering on those tiers.
        float fallback = 1.0 - step(0.0001, dot(light.color,half3(1,1,1)));
        light.direction = lerp(light.direction,DiceLandForestSunDirection(),fallback);
        // Sunlight also reaches earth between stones, so growth islands do not become black holes.
        light.color = lerp(light.color,DiceLandForestSunColor(),fallback * surfaceWeight);
        light.shadowAttenuation = lerp(light.shadowAttenuation,1.0,fallback);
        half3 fill = max(SampleSH(normal), lerp(half3(0.19,0.21,0.24), half3(0.07,0.09,0.13), top));
        half3 illumination = fill * cavity + light.color * saturate(dot(normal,light.direction))
            * lerp(0.4,1.0,light.shadowAttenuation);
        half3 tint = lerp(half3(0.85,0.92,1.0), DiceLandForestLightTint(world), top);
        float3 view = normalize(GetWorldSpaceViewDir(world));
        float3 halfDirection = normalize(light.direction + view + float3(0,0.0001,0));
        float glint = pow(saturate(dot(normal,halfDirection)),28.0) * relief.a;
        half3 specular = light.color * glint * (_PavingGlintStrength * weight)
            * light.shadowAttenuation * DiceLandForestSunVisibility(world);
        result = (albedo * illumination + specular) * tint;
    }
    else
        result = albedo * ForestSurfaceLighting(world, geometricNormal);
    return result;
}
#endif
