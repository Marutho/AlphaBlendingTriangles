#include "MainShared.hlsl"
#include "Utils.hlsl"

struct InputVS
{
	float4 position : POSITION;
	float4 color : COLOR;
};

OutputVS main( InputVS input )
{
	OutputVS output;

	float4 newPosition = input.position;
	newPosition.xyz += padding.xyz * 0.1;
	output.position = mul(worldViewProjectionMatrix, newPosition);

	output.color = input.color.rgb;
	return output;
}
