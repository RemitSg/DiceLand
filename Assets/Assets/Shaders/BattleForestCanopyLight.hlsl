#ifndef DICELAND_FOREST_CANOPY_LIGHT_INCLUDED
#define DICELAND_FOREST_CANOPY_LIGHT_INCLUDED

half3 ForestCanopyTransport(half3 pigment, half4 response, float3 normalWS, float3 world,
    float weight, float barkGain, float leafGain)
{
    float3 sun = DiceLandForestSunDirection();
    float3 key = float3(sun.xy,-abs(sun.z));
    float incidence = smoothstep(0.12,0.86,saturate(dot(normalWS,key)));
    float visible = 0.20 + 0.80 * DiceLandForestSunVisibility(world);
    float light = saturate(_DiceLandForestLighting) * saturate(weight) * response.a;
    // Deep painted pockets stay dark; the transport supplements the original painting.
    float pigmentGate = smoothstep(0.035,0.16,dot(pigment,half3(0.2126,0.7152,0.0722)));
    float diffuse = incidence * barkGain * lerp(1.0,0.60,response.b);
    float3 view = SafeNormalize(GetCameraPositionWS()-world);
    float forward = saturate(dot(view,-sun));
    float backlit = saturate(dot(-normalWS,sun));
    float transmission = response.b * leafGain * backlit * (0.40 + 0.60 * forward * forward);
    half3 reflected = pigment * half3(1.0,0.93,0.66) * diffuse;
    half3 transmitted = pigment * half3(1.1,1.0,0.40) * transmission;
    return (reflected+transmitted) * DiceLandForestSunColor() * (light * visible * pigmentGate);
}

half3 ForestCanopyLeafGloss(half3 pigment, half4 response, float3 normalWS, float3 world,
    float strength, float windStrength)
{
    float weight = saturate(strength) * saturate(_DiceLandForestLighting) * response.a * response.b;
    if (weight <= 0.001)
        return 0;
    float3 sun = DiceLandForestSunDirection();
    // Match the front-facing painted key used by the diffuse canopy response.
    float3 key = float3(sun.xy, -abs(sun.z));
    float2 bend = DiceLandForestCanopyBend(DiceLandForestProjectedGround(world), DiceLandForestCanopyBreeze());
    float motion = saturate(windStrength) * saturate(_DiceLandForestOpenings.w);
    float3 normal = SafeNormalize(normalWS + float3(bend.x * 0.75, bend.y * 0.5, 0) * motion);
    float3 view = SafeNormalize(GetCameraPositionWS() - world);
    float3 halfway = SafeNormalize(key + view);
    float incidence = saturate(dot(normal, key));
    float lobe = pow(saturate(dot(normal, halfway)), 16.0);
    float fresnel = 0.08 + 0.92 * pow(1.0 - saturate(dot(view, halfway)), 5.0);
    float pigmentGate = smoothstep(0.06, 0.32, dot(pigment, half3(0.2126, 0.7152, 0.0722)));
    return DiceLandForestSunColor() * (lobe * fresnel * incidence * 10.0 * weight
        * pigmentGate * DiceLandForestSunVisibility(world));
}

#endif
