namespace Demo
{
    using static System.Math;

    public class CoreMath
    {
        public static float Clamp( float x, float x0, float x1 )
        {
            return Min( Max( x, x0 ), x1 );
        }

        public static float Saturate( float x )
        {
            return Clamp( x, 0.0f, 1.0f );
        }

        public static float Remap( float x0, float x1, float x )
        {
            return Saturate( ( x - x0 ) / ( x1 - x0 ) );
        }

        public static float Lerp( float x0, float x1, float s )
        {
            return x0 + s * ( x1 - x0 );
        }
    }
}
