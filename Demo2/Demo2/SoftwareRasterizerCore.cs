namespace Demo
{
    using System;
    using System.Drawing;

    using SharpDX;

    using D3D11 = SharpDX.Direct3D11;
    using DXGI = SharpDX.DXGI;

    using static System.Math;

    public class SoftwareRasterizerCore : IDisposable
    {
        public enum RasterizerMode
        {
            Top,
            Bottom,
            Both
        };
        public RasterizerMode rasterizerMode { get; private set; }

        public enum OutputMode
        {
            Color,
            Depth,
        };
        public OutputMode outputMode { get; private set; }

        protected Renderer renderer;
        protected Bitmap backbufferBitmap;
        protected Bitmap depthbufferBitmap;

        private D3D11.Texture2D screenTexture;

        public SoftwareRasterizerCore( Renderer renderer )
        {
            rasterizerMode = RasterizerMode.Both;
            outputMode = OutputMode.Color;

            this.renderer = renderer;

            D3D11.Texture2DDescription texture2DDescription = new D3D11.Texture2DDescription
            {
                CpuAccessFlags = D3D11.CpuAccessFlags.Write,
                BindFlags = D3D11.BindFlags.None,
                Format = DXGI.Format.R8G8B8A8_UNorm,
                Width = renderer.width,
                Height = renderer.height,
                OptionFlags = D3D11.ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = D3D11.ResourceUsage.Staging
            };

            backbufferBitmap = new Bitmap( renderer.width, renderer.height, System.Drawing.Imaging.PixelFormat.Format32bppArgb );

            depthbufferBitmap = new Bitmap( renderer.width, renderer.height, System.Drawing.Imaging.PixelFormat.Format32bppArgb );

            screenTexture = new D3D11.Texture2D( renderer.device, texture2DDescription );
        }

        public void Dispose()
        {
            backbufferBitmap.Dispose();
            depthbufferBitmap.Dispose();
            screenTexture.Dispose();
        }

        public void SwitchMode()
        {
            rasterizerMode = ( RasterizerMode ) ( ( (int) rasterizerMode + 1 ) % 3 );
        }

        public void SwitchOutput()
        {
            outputMode = ( OutputMode ) ( ( (int) outputMode + 1 ) % 2 );
        }

        public void Clear( SharpDX.Color color )
        {
            using ( Graphics gfx = Graphics.FromImage( backbufferBitmap ) )
            using ( SolidBrush brush = new SolidBrush( System.Drawing.Color.FromArgb( color.B, color.G, color.R ) ) )
                gfx.FillRectangle( brush, 0, 0, renderer.width, renderer.height );

            using ( Graphics gfx = Graphics.FromImage( depthbufferBitmap ) )
            using ( SolidBrush brush = new SolidBrush( System.Drawing.Color.FromArgb( 0, 0, 255 ) ) )
                gfx.FillRectangle( brush, 0, 0, renderer.width, renderer.height );
        }

        virtual public void Draw( Vector3[] vertices, Viewport viewport, Matrix worldViewProjectionMatrix ) { }

        public void EndFrame()
        {
            Bitmap bitmap = outputMode == OutputMode.Color ? backbufferBitmap : depthbufferBitmap;

            var mapSource = renderer.deviceContext.MapSubresource( screenTexture, 0, D3D11.MapMode.Write, SharpDX.Direct3D11.MapFlags.None );
            var boundsRect = new System.Drawing.Rectangle( 0, 0, renderer.width, renderer.height );
            var bitmapData = bitmap.LockBits( boundsRect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat );

            var sourcePtr = bitmapData.Scan0;
            var destinationPtr = mapSource.DataPointer;

            for ( int y = 0; y < renderer.height; y++ )
            {
                Utilities.CopyMemory( destinationPtr, sourcePtr, renderer.width * 4 );

                sourcePtr = IntPtr.Add( sourcePtr, mapSource.RowPitch );
                destinationPtr = IntPtr.Add( destinationPtr, bitmapData.Stride );
            }

            bitmap.UnlockBits( bitmapData );
            renderer.deviceContext.UnmapSubresource( screenTexture, 0 );

            renderer.deviceContext.CopyResource( screenTexture, renderer.backbufferTexture );
        }

        protected void SortVerticesAscendingByY( ref Vector3 v1, ref Vector3 v2, ref Vector3 v3 )
        {
            Vector3 vTmp;
        
            if ( v1.Y > v2.Y )
            {
                vTmp = v1;
                v1 = v2;
                v2 = vTmp;
            }

            if ( v1.Y > v3.Y )
            {
                vTmp = v1;
                v1 = v3;
                v3 = vTmp;
            }

            if ( v2.Y > v3.Y )
            {
                vTmp = v2;
                v2 = v3;
                v3 = vTmp;
            }
        }

        protected void DrawLine( float y, float x0, float x1, Vector3 color )
        {
            float t0 = Min( x0, x1 );
            float t1 = Max( x0, x1 );

            for ( float x = (float) Round( t0 ) + 0.5f; x < (float) Round( t1 ) + 0.5f; x++ )
                backbufferBitmap.SetPixel( (int) x, (int) y, System.Drawing.Color.FromArgb( (int) ( 255 * color.X ), (int) ( 255 * color.Y ), (int) ( 255 * color.Z ) ) );
        }
    }
}
