#include "MainShared.hlsl"
#include "Utils.hlsl"

float4 main(OutputVS input) : SV_TARGET
{
#ifdef VERTEX_COLOR
	//time sera la s, de 10 el porcentaje que esta de leggar a 10 segundos
	
	//frac para no volver
	if (time >= 6)
	{
		float porcentaje = sin(time/2);
		float valorRojo = lerp(0, 1, 1 - porcentaje);
		float valorVerde = lerp(1, 0, 1 - porcentaje);
		input.color.x = valorRojo;
		input.color.y = valorVerde;
	}
else
{
		input.color.x = 0;
		input.color.y = 1;
		input.color.z = 0;
 }
	

	return float4( input.color);
#else
	return float4( color, 1.0 );
#endif
}
