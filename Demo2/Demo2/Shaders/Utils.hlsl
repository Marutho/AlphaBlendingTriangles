#ifndef UTILS
#define UTILS

float remap( float x0, float x1, float x )
{
	return saturate( ( x - x0 ) / ( x1 - x0 ) );
}

float2 remap( float2 x0, float2 x1, float2 x )
{
	return saturate( ( x - x0 ) / ( x1 - x0 ) );
}

float3 remap( float3 x0, float3 x1, float3 x )
{
	return saturate( ( x - x0 ) / ( x1 - x0 ) );
}

float4 remap( float4 x0, float4 x1, float4 x )
{
	return saturate( ( x - x0 ) / ( x1 - x0 ) );
}

#endif
