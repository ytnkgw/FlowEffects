﻿#include "Common.cginc"

sampler2D _SourcePositionBuffer0;
sampler2D _SourcePositionBuffer1;

sampler2D _PositionBuffer;
float4 _PositionBuffer_TexelSize;

sampler2D _VelocityBuffer;
float4 _VelocityBuffer_TexelSize;

sampler2D _OrthnormBuffer;
float4 _OrthnormBuffer_TexelSize;

float _SpeedLimit;
float _Drag;

float4 InitializePositionFragment(v2f_img i) : SV_Target
{
	return tex2D(_SourcePositionBuffer1, i.uv.xy);
}

float4 InitializeVelocityFragment(v2f_img i) : SV_Target
{
	return 0;
}

float4 InitializeOrthnormFragment(v2f_img i) : SV_Target
{
	return 0;
}

float4 UpdatePositionFragment(v2f_img i) : SV_Target
{
	// Memo about "_TexelSize" : https://docs.unity3d.com/ja/2017.3/Manual/SL-PropertiesInPrograms.html
	const float texelHeight = _PositionBuffer_TexelSize.y;

	float2 uv = i.uv.xy;

	if (uv.y < texelHeight) 
	{
		// First row : just copy the source position to first row.
		return tex2D(_SourcePositionBuffer1, uv);
	}
	else 
	{
		// Fetch the position and velocity from the previous row.
		uv.y -= texelHeight;
		float3 p = tex2D(_PositionBuffer, uv).xyz;
		float3 v = tex2D(_VelocityBuffer, uv).xyz;

		// 味付け : ポジションを速度の向きに少しずつ移動させている
		// Apply the velocity cap.
		float lv = max(length(v), 0.001);
		v = v * min(lv, _SpeedLimit) / lv;
		// Update position with velocity.
		p += v * unity_DeltaTime.x;

		return half4(p, 0);
	}
}

float4 UpdateVelocityFragment(v2f_img i) : SV_Target
{
	const float texelHeight = _VelocityBuffer_TexelSize.y;
	
	// DONE :: Q :: what is this uv of. maybe target renderTexture.
	// → 描画先じゃないとUV取ってきても意味ないので、つまり描画先のuv.
	float2 uv = i.uv.xy;

	if (uv.y < texelHeight)
	{
		// First row : calculate the vertex velocity.
		// Get the average with previous frams for low-pass filtering.
		float3 p0 = tex2D(_SourcePositionBuffer0, uv).xyz; // pos in one frame before.
		float3 p1 = tex2D(_SourcePositionBuffer1, uv).xyz; // current pos.
		float3 v0 = tex2D(_VelocityBuffer, uv).xyz; // velocity in one frame before.
		float3 v1 = (p1 - p0) * unity_DeltaTime.y; // current velocity from pos.
		return float4((v0 + v1) * 0.5, 0);
	}
	else 
	{
		// Retrieve the velocity from the previous row and dampen it.
		uv.y -= texelHeight;
		float3 v = tex2D(_VelocityBuffer, uv).xyz;
		return float4(v * _Drag, 0); // ここの味付けは何のため? スピードの値を大きくしている
		//return float4(v, 0);
	}
}

// Q :: I have no idea at all here.
// ここ丸々味付けの可能性あり
float4 UpdateOrthnormFragment(v2f_img i) : SV_Target
{
	const float texelHeight = _OrthnormBuffer_TexelSize.y;

	float2 uv = i.uv.xy;

	float2 uv0 = float2(uv.x, uv.y - texelHeight * 2);  // 2個後のフレームを取得するためのuv
	float2 uv1 = float2(uv.x, uv.y - texelHeight); // 1個後のフレームを取得するためのuv
	float2 uv2 = float2(uv.x, uv.y + texelHeight * 2); // 2個前のフレームを取得するためのuv

	// Use the parent normal vector from the previous frame.
	half4 b1 = tex2D(_OrthnormBuffer, uv1);
	half3 ax = StereoInverseProjection(b1.zw);

	// Tangent vector
	float3 p0 = tex2D(_PositionBuffer, uv0);
	float3 p1 = tex2D(_PositionBuffer, uv2);
	half3 az = p1 - p0 + float3(1e-6, 0, 0); // 1e-6は1のマイナス6乗

	// Reconstruct the orthnormal basis.
	half3 ay = normalize(cross(az, ax));
	ax = normalize(cross(ay, az));

	// Twisting
	half tw = frac(uv.x * 327.7289) * (1 - uv.y) * 0.2;
	ax = normalize(ax + ay * tw);
	return half4(StereoProjection(ay), StereoProjection(ax));
	//return half4(0, 0, 0, 0);
}