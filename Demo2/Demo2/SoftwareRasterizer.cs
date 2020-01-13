#define RASTERIZER_SOLUTION
#define DEPTH_TEST_SOLUTION

namespace Demo
{
    using System.Diagnostics;

    using SharpDX;

    using static System.Math;
    using static CoreMath;

    public class SoftwareRasterizer : SoftwareRasterizerCore
    {
        public SoftwareRasterizer( Renderer renderer ) : base( renderer ) {}

        override public void Draw( Vector3[] vertices, Viewport viewport, Matrix worldViewProjectionMatrix )
        { 
            Vector3 color = new Vector3( 1.0f, 1.0f, 1.0f );
            Vector3 v1 = VertexShader( vertices[0], worldViewProjectionMatrix );
            Vector3 v2 = VertexShader( vertices[1], worldViewProjectionMatrix );
            Vector3 v3 = VertexShader( vertices[2], worldViewProjectionMatrix );

            v1 = ViewportTransform( viewport, v1 );
            v2 = ViewportTransform( viewport, v2 );
            v3 = ViewportTransform( viewport, v3 );

            SortVerticesAscendingByY( ref v1, ref v2, ref v3 ); // v1.Y <= v2.Y <= v3.Y

            // v4 splits the triangle into two simpler ones (with one edge horizontal):
            Vector3 v4 = v2; 
            float s = Remap( v1.Y, v3.Y, v2.Y );
            v4.X = Lerp( v1.X, v3.X, s );
            v4.Z = Lerp( v1.Z, v3.Z, s );

            if ( rasterizerMode == RasterizerMode.Top || rasterizerMode == RasterizerMode.Both)
                FillTopTriangle( v1, v2, v4, color );

            if ( rasterizerMode == RasterizerMode.Bottom || rasterizerMode == RasterizerMode.Both)
                FillBottomTriangle( v3, v2, v4, color );
        }

        private Vector3 VertexShader( Vector3 vertex, Matrix worldViewProjectionMatrix )
        {
            Vector4 vector4 = new Vector4( vertex.X, vertex.Y, vertex.Z, 1.0f );
            return (Vector3) Vector4.Transform( vector4, worldViewProjectionMatrix );
        }

        private Vector3 ViewportTransform( Viewport viewport, Vector3 position )
        {
            Vector3 positionViewport = position;
            positionViewport.X = Lerp( viewport.X, viewport.X + viewport.Width,  Remap( -1.0f,  1.0f, position.X ) );
            positionViewport.Y = Lerp( viewport.Y, viewport.Y + viewport.Height, Remap(  1.0f, -1.0f, position.Y ) );
            return positionViewport;
        }

        private void FillScanLine( Vector3 v1, Vector3 v2, Vector3 v3, Vector3 color, float y )
        {
            #if RASTERIZER_SOLUTION
            float s = Remap( v1.Y, v2.Y, y );
            float x0 = Lerp( v1.X, v2.X, s );
            float x1 = Lerp( v1.X, v3.X, s );

            #if DEPTH_TEST_SOLUTION
            float d0 = Lerp( v1.Z, v2.Z, s );
            float d1 = Lerp( v1.Z, v3.Z, s );
            DrawLineDepthTest( y, x0, x1, d0, d1, color );
            #else
            DrawLine( y, x0, x1, color );
            #endif
            #endif
        }

        private void FillTopTriangle( Vector3 v1, Vector3 v2, Vector3 v3, Vector3 color )
        {
            Debug.Assert( v2.Y == v3.Y );
            Debug.Assert( v1.Y <= v3.Y && v1.Y <= v2.Y );

            for ( float y = (float) Round( v1.Y ) + 0.5f; y < (float) Round( v2.Y ) + 0.5f; y++ )
            {
                FillScanLine( v1, v2, v3, color, y );
            }
        }

        private void FillBottomTriangle( Vector3 v1, Vector3 v2, Vector3 v3, Vector3 color )
        {
            Debug.Assert( v2.Y == v3.Y );
            Debug.Assert( v1.Y >= v3.Y && v1.Y >= v2.Y );

            for ( float y = (float) Round( v2.Y ) + 0.5f; y < (float) Round( v1.Y ) + 0.5f; y++ )
            {
                FillScanLine( v1, v2, v3, color, y );
            }
        }

        private void DrawLineDepthTest( float y, float x0, float x1, float depth0, float depth1, Vector3 color )
        {
            float t0 = Min( x0, x1 );
            float t1 = Max( x0, x1 );

            for ( float x = (float) Round( t0 ) + 0.5f; x < (float) Round( t1 ) + 0.5f; x++ )
            {
                #if DEPTH_TEST_SOLUTION
                float s = Remap( x0, x1, x );
                float depth = Lerp( depth0, depth1, s );

                if ( depthbufferBitmap.GetPixel( (int) x, (int) y ).B > (int) ( 255 * depth ) )
                {
                    depthbufferBitmap.SetPixel( (int) x, (int) y, System.Drawing.Color.FromArgb( 0, 0, (int) ( 255 * Clamp( depth, 0.0f, 1.0f ) ) ) );

                    backbufferBitmap.SetPixel( (int) x, (int) y, System.Drawing.Color.FromArgb( (int) ( 255 * color.X ), (int) ( 255 * color.Y ), (int) ( 255 * color.Z ) ) );
                }
                #else
                backbufferBitmap.SetPixel( (int) x, (int) y, System.Drawing.Color.FromArgb( (int) ( 255 * color.X ), (int) ( 255 * color.Y ), (int) ( 255 * color.Z ) ) );
                #endif
            }
        }
    }
}
