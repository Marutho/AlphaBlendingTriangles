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

	float r = sqrt((output.position.x) * (output.position.x) + (output.position.y) * (output.position.y));
	float angle = atan2((output.position.x), (output.position.y));
	float newAngle = angle + time;
	output.position.x = r * (float)sin(newAngle);
	output.position.y = r * (float)cos(newAngle);

	output.position.xyz += offset;

	



	#ifdef VERTEX_COLOR
	output.color = input.color;
	#endif
	return output;
}
