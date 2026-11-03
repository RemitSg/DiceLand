#ifndef DICELAND_FOREST_SUN_VOLUME_INCLUDED
#define DICELAND_FOREST_SUN_VOLUME_INCLUDED

float ForestSunErf(float value)
{
    float x = abs(value);
    float t = rcp(1.0 + 0.3275911 * x);
    float polynomial = (((((1.061405429 * t - 1.453152027) * t)
        + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t;
    return sign(value) * (1.0 - polynomial * exp(-x * x));
}

// Integral of exp(-((origin + rate*t - centre)/width)^2), in world metres.
float ForestSunGaussianIntegral(float origin, float rate, float begin, float end, float centre, float width)
{
    float length = max(0.0, end - begin);
    width = max(0.001, width);
    float a = (origin + rate * begin - centre) / width;
    float b = (origin + rate * end - centre) / width;
    if (abs(b - a) < 0.05)
    {
        // Avoid cancellation for a view ray almost parallel to the sunlight band.
        float middle = (a + b) * 0.5;
        return length * (exp(-a * a) + 4.0 * exp(-middle * middle) + exp(-b * b)) / 6.0;
    }
    return max(0.0, 0.886226925 * width / rate * (ForestSunErf(b) - ForestSunErf(a)));
}

float2 ForestSunBoxInterval(float3 origin, float3 direction)
{
    float3 inverse = (step(0.0, direction) * 2.0 - 1.0) / max(abs(direction), 0.000001);
    float3 a = (-0.5 - origin) * inverse;
    float3 b = (0.5 - origin) * inverse;
    float3 near = min(a, b), far = max(a, b);
    return float2(max(near.x, max(near.y, near.z)), min(far.x, min(far.y, far.z)));
}

#endif
