// FluidCommon.hlsl
// Shared functions and constants for fluid simulation shaders

#ifndef FLUID_COMMON_INCLUDED
#define FLUID_COMMON_INCLUDED

// ============================================================================
// Constants
// ============================================================================

#define MAX_OBSTACLES 16
#define THREAD_GROUP_SIZE 8

// ============================================================================
// Structures
// ============================================================================

// Obstacle data structure (must match C# struct layout)
struct ObstacleData
{
    float2 position;      // World position
    float2 velocity;      // World velocity
    float2 halfExtents;   // Ellipse semi-axes
    float rotation;       // Rotation in radians
    float velocityScale;  // Influence multiplier
};

// ============================================================================
// Coordinate Conversion
// ============================================================================

// Convert cell index to UV coordinates (0-1)
float2 CellToUV(uint2 cell, float2 resolution)
{
    return (cell + 0.5) / resolution;
}

// Convert UV to world position
float2 UVToWorld(float2 uv, float2 simulationCenter, float2 simulationSize)
{
    return simulationCenter + (uv - 0.5) * simulationSize;
}

// Convert world position to UV
float2 WorldToUV(float2 worldPos, float2 simulationCenter, float2 simulationSize)
{
    return (worldPos - simulationCenter) / simulationSize + 0.5;
}

// ============================================================================
// Geometry Functions
// ============================================================================

// Rotate a 2D point around origin
float2 RotatePoint(float2 point, float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return float2(
        point.x * c - point.y * s,
        point.x * s + point.y * c
    );
}

// Check if a point is inside an ellipse
bool IsInsideEllipse(float2 worldPos, float2 ellipsePos, float2 halfExtents, float rotation)
{
    float2 localPos = RotatePoint(worldPos - ellipsePos, -rotation);
    float2 normalized = localPos / halfExtents;
    return dot(normalized, normalized) < 1.0;
}

// Compute approximate signed distance to ellipse
// Negative inside, positive outside
float EllipseDistance(float2 worldPos, float2 ellipsePos, float2 halfExtents, float rotation)
{
    float2 localPos = RotatePoint(worldPos - ellipsePos, -rotation);
    float2 normalized = localPos / halfExtents;
    float distSq = dot(normalized, normalized);

    // Approximate signed distance
    float dist = sqrt(distSq) - 1.0;
    float scale = min(halfExtents.x, halfExtents.y);
    return dist * scale;
}

// Compute distance from obstacle using ObstacleData struct
float ObstacleDistance(float2 worldPos, ObstacleData obs)
{
    return EllipseDistance(worldPos, obs.position, obs.halfExtents, obs.rotation);
}

// ============================================================================
// Sampling Helpers
// ============================================================================

// Bilinear sample with boundary clamping
float2 BilinearSampleVelocity(Texture2D<float2> tex, SamplerState samp, float2 uv)
{
    uv = clamp(uv, 0.001, 0.999);
    return tex.SampleLevel(samp, uv, 0);
}

float BilinearSampleScalar(Texture2D<float> tex, SamplerState samp, float2 uv)
{
    uv = clamp(uv, 0.001, 0.999);
    return tex.SampleLevel(samp, uv, 0);
}

// ============================================================================
// Noise Functions (for procedural density patterns)
// ============================================================================

// Hash function for procedural noise
float Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// Value noise
float ValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);

    // Quintic interpolation
    f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

    float a = Hash21(i);
    float b = Hash21(i + float2(1, 0));
    float c = Hash21(i + float2(0, 1));
    float d = Hash21(i + float2(1, 1));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// Fractal Brownian Motion
float FBM(float2 p, int octaves, float persistence, float lacunarity)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    float maxValue = 0.0;

    for (int i = 0; i < octaves; i++)
    {
        value += amplitude * ValueNoise(p * frequency);
        maxValue += amplitude;
        amplitude *= persistence;
        frequency *= lacunarity;
    }

    return value / maxValue;
}

// ============================================================================
// Color Utilities
// ============================================================================

// Three-color gradient (used for density visualization)
float3 ThreeColorGradient(float t, float3 color1, float3 color2, float3 color3)
{
    if (t < 0.5)
    {
        return lerp(color1, color2, t * 2.0);
    }
    else
    {
        return lerp(color2, color3, (t - 0.5) * 2.0);
    }
}

// Four-color gradient
float3 FourColorGradient(float t, float3 color1, float3 color2, float3 color3, float3 color4)
{
    if (t < 0.333)
    {
        return lerp(color1, color2, t * 3.0);
    }
    else if (t < 0.666)
    {
        return lerp(color2, color3, (t - 0.333) * 3.0);
    }
    else
    {
        return lerp(color3, color4, (t - 0.666) * 3.0);
    }
}

#endif // FLUID_COMMON_INCLUDED
