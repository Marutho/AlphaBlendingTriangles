#include "MainShared.hlsl"
#include "Utils.hlsl"

float4 main( OutputVS input  ) : SV_TARGET
{
	return float4( input.color, alpha.a );
}
