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
	output.position = mul( worldViewProjectionMatrix, input.position );
	output.color = input.color.rgb;
	return output;
}
