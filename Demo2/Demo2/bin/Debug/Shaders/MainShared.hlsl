cbuffer ConstantBuffer : register(b0)
{
	float4x4 worldViewProjectionMatrix;
	float3 padding;
	float time; // in seconds
	float4 alpha;
}

struct OutputVS
{
	float4 position : SV_POSITION;
	float3 color : COLOR;
};
