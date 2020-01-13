#include "MainShared.hlsl"

float4 main( OutputVS input  ) : SV_TARGET
{
	return float4( input.color, 1.0 );
}
