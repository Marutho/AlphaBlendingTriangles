#include "MainShared.hlsl"
#include "Utils.hlsl"

float4 main( OutputVS input  ) : SV_TARGET
{
<<<<<<< HEAD
	return float4( input.color, alpha.a );
=======
	return float4( input.color, 0.1 );
>>>>>>> f79dc965213f439735564e331f0c9e24cbd024c1
}
