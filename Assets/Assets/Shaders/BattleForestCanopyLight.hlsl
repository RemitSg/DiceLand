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

#endif
