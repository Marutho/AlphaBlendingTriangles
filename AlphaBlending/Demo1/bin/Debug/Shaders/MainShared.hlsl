#ifndef VERTEX_COLOR
#define VERTEX_COLOR
#endif

cbuffer ConstantBuffer : register(b0)
{
	float4 offsetScale;
	float3 color;
	float time; // in seconds
}

struct OutputVS
{
	float4 position : SV_POSITION;
	#ifdef VERTEX_COLOR
	float4 color : COLOR;
	#endif
};
