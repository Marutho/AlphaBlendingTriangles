#include "MainShared.hlsl"
#include "Utils.hlsl"

float4 main( OutputVS input  ) : SV_TARGET
{
#ifdef VERTEX_COLOR
	return float4( input.color);
#else
	return float4( color, 1.0 );
#endif
}
