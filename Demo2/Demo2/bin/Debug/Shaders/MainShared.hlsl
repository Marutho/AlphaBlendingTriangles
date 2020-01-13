cbuffer ConstantBuffer : register(b0)
{
	float4x4 worldViewProjectionMatrix;
	float time; // in seconds
}

struct OutputVS
{
	float4 position : SV_POSITION;
	float3 color : COLOR;
};
