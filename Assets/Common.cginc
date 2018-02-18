#include "UnityCG.cginc"


float _RandomSeed;

float UVRandom(float2 uv, float salt)
{
	uv += float2(salt, _RandomSeed);
	return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}


half2 StereoProjection(half3 n)
{
	return n.xy / (1 - n.z);
}

half3 StereoInverseProjection(half2 p)
{
	float d = 2 / (dot(p.xy, p.yx) + 1);
	return float3(p.xy * d, 1 - d);
}

half3 HueToRGB(half h)
{
	h = frac(h);
	half r = abs(h * 6 - 3) - 1;
	half g = 2 - abs(h * 6 - 2);
	half b = 2 - abs(h * 6 - 4);
	half3 rgb = saturate(half3(r, g, b));
#if UNITY_COLORSPACE_GAMMA
	return rgb;
#else 
	return GammaToLinearSpace(rgb);
#endif 
}

// Common color animation
half _BaseHue;
half _HueRandomness;
half _Saturation;
half _Brightness;
half _EmissionProb;
half _HueShift;
half _BrightnessOffs;

half3 ColorAnimation(float id, half intensity)
{
	// Low frequency oscillation with half-weve rectified sinusoid.
	half phase = UVRandom(id, 30) * 32 + _Time.y * 4;
	half lfo = abs(sin(phase * UNITY_PI));

	// Switch LFO
	lfo *= UVRandom(id + floor(phase), 31) < _EmissionProb;

	// Hue animation
	half hue = _BaseHue + UVRandom(id, 32) * _HueRandomness + _HueShift * intensity;

	// Convert to RGB
	half3 rgb = lerp(1, HueToRGB(hue), _Saturation);

	// Apply brightness
	return rgb * (_Brightness * lfo * _BrightnessOffs * intensity);
}