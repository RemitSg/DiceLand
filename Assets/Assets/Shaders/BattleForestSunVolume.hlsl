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

float3 ForestSunMomentTap(float u, float weight, float offset, float rate)
{
    float x = offset + rate * u;
    return weight * exp(-x * x) * float3(1.0, u, u * u);
}

// Mass and first/second moments in normalized ray coordinates u=[-1,1].
// Normalizing before integration avoids large world-distance cancellation.
float3 ForestSunGaussianMoments(float origin, float rate, float begin, float end, float centre, float width)
{
    float halfLength = max(0.0, end - begin) * 0.5;
    if (halfLength < 0.000001)
        return 0;
    width = max(0.001, width);
    float middle = (begin + end) * 0.5;
    float offset = (origin + rate * middle - centre) / width;
    float speed = rate * halfLength / width;
    if (abs(offset) > abs(speed) + 6.0)
        return 0;
    float3 moments = 0;
    if (abs(speed) < 0.5)
    {
        // Smooth, almost-parallel rays: stable five-point Gaussian quadrature.
        moments = ForestSunMomentTap(0.0, 0.568888889, offset, speed);
        moments += ForestSunMomentTap(-0.538469310, 0.478628670, offset, speed);
        moments += ForestSunMomentTap(0.538469310, 0.478628670, offset, speed);
        moments += ForestSunMomentTap(-0.906179846, 0.236926885, offset, speed);
        moments += ForestSunMomentTap(0.906179846, 0.236926885, offset, speed);
    }
    else
    {
        float mass = ForestSunGaussianIntegral(origin, rate, begin, end, centre, width) / halfLength;
        float mean = -offset / speed, varianceScale = rcp(speed * speed);
        float nearValue = exp(-(offset - speed) * (offset - speed));
        float farValue = exp(-(offset + speed) * (offset + speed));
        moments.x = mass;
        moments.y = mean * mass - 0.5 * varianceScale * (farValue - nearValue);
        moments.z = (mean * mean + 0.5 * varianceScale) * mass
            - 0.5 * varianceScale * ((1.0 + mean) * farValue - (mean - 1.0) * nearValue);
    }
    if (moments.x < 0.0000001)
        return 0;
    // Roundoff in negligible tails must not move samples outside the clipped ray.
    moments.y = clamp(moments.y, -moments.x, moments.x);
    moments.z = clamp(moments.z, moments.y * moments.y / moments.x, moments.x);
    return moments * halfLength;
}

float3 ForestSunSoftGaussianMoments(float origin, float rate, float begin, float end,
    float centre, float width, float softness)
{
    float filteredWidth = sqrt(width * width + softness * softness);
    // Gaussian convolution spreads each ribbon without adding cross-sectional energy.
    return ForestSunGaussianMoments(origin, rate, begin, end, centre, filteredWidth) * (width / filteredWidth);
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
