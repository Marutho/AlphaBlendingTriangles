#include "MainShared.hlsl"
#include "Utils.hlsl"


struct InputVS
{
	float4 position : POSITION;
	#ifdef VERTEX_COLOR
	float4 color : COLOR;
	#endif
};

OutputVS main( InputVS input )
{
	float3 offset = offsetScale.xyz;
	float scale = offsetScale.w;

	OutputVS output;
	output.position = input.position;
	output.position.xy *= scale;
	output.position.xyz += offset;
	#ifdef VERTEX_COLOR
	output.color = input.color;
	#endif
	return output;
}
