namespace Demo1
{
    using System.Diagnostics;

    using SharpDX;

    using static System.Math;
    using static CoreMath;

    public class SoftwareRasterizer : SoftwareRasterizerCore
    {
        public SoftwareRasterizer( Renderer renderer ) : base( renderer ) {}

        override protected void Rasterize( Vector3[] vertices, Viewport viewport, ConstantBuffer constantBuffer )
        { 
            Vector3 v1 = VertexShader( vertices[0], constantBuffer );
            Vector3 v2 = VertexShader( vertices[1], constantBuffer );
            Vector3 v3 = VertexShader( vertices[2], constantBuffer );

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
                FillTopTriangle( v1, v2, v4, constantBuffer.color );

            if ( rasterizerMode == RasterizerMode.Bottom || rasterizerMode == RasterizerMode.Both)
                FillBottomTriangle( v3, v2, v4, constantBuffer.color );

        }

        private Vector3 VertexShader( Vector3 vertex, ConstantBuffer constantBuffer )
        {
            vertex.X *= constantBuffer.offsetScale.W;
            vertex.Y *= constantBuffer.offsetScale.W;

            vertex.X += constantBuffer.offsetScale.X;
            vertex.Y += constantBuffer.offsetScale.Y;
            vertex.Z += constantBuffer.offsetScale.Z;

            return vertex;
        }

        private Vector3 ViewportTransform( Viewport viewport, Vector3 position )
        {
            Vector3 positionViewport = position;
            positionViewport.X = Lerp( viewport.X, viewport.X + viewport.Width,  Remap( -1.0f,  1.0f, position.X ) );
            positionViewport.Y = Lerp( viewport.Y, viewport.Y + viewport.Height, Remap(  1.0f, -1.0f, position.Y ) );
            return positionViewport;
        }


        private void FillTopTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 color)
        {
            Debug.Assert(v2.Y == v3.Y);
            Debug.Assert(v1.Y <= v3.Y && v1.Y <= v2.Y);

            for (float y = (float)Round(v1.Y) + 0.5f; y < (float)Round(v2.Y) + 0.5f; y++)
            {
                FillScanLine(v1, v2, v3, color, y);
            }
        }

        private void FillBottomTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 color)
        {
            Debug.Assert(v2.Y == v3.Y);
            Debug.Assert(v1.Y >= v3.Y && v1.Y >= v2.Y);

            for (float y = (float)Round(v2.Y) + 0.5f; y < (float)Round(v1.Y) + 0.5f; y++)
            {
                FillScanLine(v1, v2, v3, color, y);
            }
        }

        private void FillScanLine( Vector3 v1, Vector3 v2, Vector3 v3, Vector3 color, float y )
        {
            float rmp = Remap(v1.Y, v3.Y, y);
            float x0 = Lerp(v1.X, v2.X, rmp); // ?
            float x1 = Lerp(v1.X, v3.X, rmp);  // ?

            float d0 = Lerp(v1.Z, v2.Z, rmp);
            float d1 = Lerp(v1.Z, v3.Z, rmp);

            DrawLineDepthTest( y, x0, x1, d0, d1, color );
        }

        

        private void DrawLineDepthTest( float y, float x0, float x1, float depth0, float depth1, Vector3 color )
        {
            float t0 = Min( x0, x1 );
            float t1 = Max( x0, x1 );

            for ( float x = (float) Round( t0 ) + 0.5f; x < (float) Round( t1 ) + 0.5f; x++ )
            {
                float depth = 0.0f;

                depth = Lerp(depth0, depth1, Remap(x0, x1, x));

                if((255 * Min(depth, 1.0f)) <= depthbufferBitmap.GetPixel((int)x, (int)y).B)
                {
                    depthbufferBitmap.SetPixel((int)x, (int)y, System.Drawing.Color.FromArgb(0, 0, (int)(255 * Min(depth, 1.0f))));
                    backbufferBitmap.SetPixel((int)x, (int)y, System.Drawing.Color.FromArgb((int)(255 * color.X), (int)(255 * color.Y), (int)(255 * color.Z)));
                }
                
            }
        }
    }
}
