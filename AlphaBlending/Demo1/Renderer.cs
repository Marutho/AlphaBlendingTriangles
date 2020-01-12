#define MULTIPLE_TRIANGLES // Render one or a few triangles
#define SHUFFLE_TRIANGLES // If commented out, we're using the Painter's algorithm
// #define NON_PLANAR_TRIANGLES // For checking that depth interpolation is being done right

namespace Demo1
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;

    using SharpDX;
    using SharpDX.D3DCompiler;

    using D3D11 = SharpDX.Direct3D11;
    using DXGI = SharpDX.DXGI;

    public class Renderer : IDisposable
    {
        public int width { get; private set; }
        public int height { get; private set; }

        public D3D11.Device device { get; }
        public D3D11.DeviceContext deviceContext { get; private set; }

        public D3D11.Texture2D backbufferTexture { get; private set; }
        public D3D11.RenderTargetView backbufferRTV { get; private set; }
        public D3D11.DepthStencilView depthDSV { get; private set; }

        public SoftwareRasterizer softwareRasterizer { get; private set; }

        private D3D11.DepthStencilState depthStencilState;
        private D3D11.RasterizerState rasterizerState;

        private D3D11.Buffer trianglePositionVertexBuffer;
        private D3D11.Buffer triangleColorVertexBuffer;

        [StructLayout( LayoutKind.Sequential )]
        private struct ConstantBufferCPU
        {
            public Vector4 offsetScale;
            public Vector3 color;
            public float time;
        };
        private D3D11.Buffer constantBuffer;


        private D3D11.VertexShader vertexShader;
        private D3D11.PixelShader pixelShader;

        private D3D11.InputElement[] inputElements = new D3D11.InputElement[]
        {
            new D3D11.InputElement( "POSITION", 0, DXGI.Format.R32G32B32_Float, 0 ),
            new D3D11.InputElement( "COLOR", 0, DXGI.Format.R32G32B32_Float, 1),
        };

        #if NON_PLANAR_TRIANGLES
        Vector3[] vertexPositions = new Vector3[] { new Vector3( 0.5f, -0.5f, 1.0f ), new Vector3( -0.5f, -0.5f, 1.0f ), new Vector3( 0.0f, 0.5f, 0.0f ) };
        Vector3[] vertexColor = new Vector3[] {
            new Vector3(1.0f, 0.0f, 0.0f), //COLOR
            new Vector3(0.0f, 1.0f, 0.0f),  //COLOR
            new Vector3(0.0f, 0.0f, 1.0f) }; //COLOR
#else
        Vector3[] vertexPositions = new Vector3[] {
            new Vector3( 0.5f, -0.5f, 0.0f ),  //POSITION
            new Vector3( -0.5f, -0.25f, 0.0f ), //POSITION
            new Vector3( 0.0f, 0.5f, 0.0f ) }; //POSITION

        Vector3[] vertexColor = new Vector3[] { 
            new Vector3(1.0f, 0.0f, 0.0f), //COLOR
            new Vector3(0.0f, 1.0f, 0.0f),  //COLOR
            new Vector3(0.0f, 0.0f, 1.0f) }; //COLOR
#endif

        private D3D11.InputLayout inputLayout;

        private Stopwatch stopWatch;

        public enum RenderMode
        {
            RenderModeSoftware,
            RenderModeHardware,
        };
        public RenderMode mode { get; private set; }

        public Renderer( D3D11.Device device, DXGI.SwapChain swapChain )
        {
            this.device = device;

            deviceContext = device.ImmediateContext;

            InitializeDeviceResources( swapChain );
            InitializeShaders();
            InitializeTriangle();

            softwareRasterizer = new SoftwareRasterizer( this );

            mode = RenderMode.RenderModeHardware;

            stopWatch = Stopwatch.StartNew();
        }

        private void InitializeDeviceResources( DXGI.SwapChain swapChain )
        {
            backbufferTexture = swapChain.GetBackBuffer<D3D11.Texture2D>( 0 );
            backbufferRTV = new D3D11.RenderTargetView( device, backbufferTexture );

            width  = backbufferTexture.Description.Width;
            height = backbufferTexture.Description.Height;

            var depthBufferDesc = new D3D11.Texture2DDescription()
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = DXGI.Format.D24_UNorm_S8_UInt,
                SampleDescription = new DXGI.SampleDescription( 1, 0 ),
                Usage = D3D11.ResourceUsage.Default,
                BindFlags = D3D11.BindFlags.DepthStencil,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                OptionFlags = D3D11.ResourceOptionFlags.None
            };

            using ( var depthStencilBufferTexture = new D3D11.Texture2D( device, depthBufferDesc ) )
            {
                var depthStencilViewDesc = new D3D11.DepthStencilViewDescription()
                {
                    Format = DXGI.Format.D24_UNorm_S8_UInt,
                    Dimension = D3D11.DepthStencilViewDimension.Texture2D,
                    Texture2D = new D3D11.DepthStencilViewDescription.Texture2DResource()
                    {
                        MipSlice = 0
                    }
                };

                depthDSV = new D3D11.DepthStencilView( device, depthStencilBufferTexture, depthStencilViewDesc );
            }

            var depthStencilDesc = new D3D11.DepthStencilStateDescription()
            {
                IsDepthEnabled = true,
                DepthWriteMask = D3D11.DepthWriteMask.All,
                DepthComparison = D3D11.Comparison.Less,
                IsStencilEnabled = false,
                StencilReadMask = 0xFF,
                StencilWriteMask = 0xFF,

                FrontFace = new D3D11.DepthStencilOperationDescription()
                {
                    FailOperation = D3D11.StencilOperation.Keep,
                    DepthFailOperation = D3D11.StencilOperation.Keep,
                    PassOperation = D3D11.StencilOperation.Keep,
                    Comparison = D3D11.Comparison.Always
                },

                BackFace = new D3D11.DepthStencilOperationDescription()
                {
                    FailOperation = D3D11.StencilOperation.Keep,
                    DepthFailOperation = D3D11.StencilOperation.Keep,
                    PassOperation = D3D11.StencilOperation.Keep,
                    Comparison = D3D11.Comparison.Always
                }
            };

            depthStencilState = new D3D11.DepthStencilState( device, depthStencilDesc );

            var rasterDesc = new D3D11.RasterizerStateDescription()
            {
                IsAntialiasedLineEnabled = false,
                CullMode = D3D11.CullMode.Back,
                DepthBias = 0,
                DepthBiasClamp = 0.0f,
                IsDepthClipEnabled = true,
                FillMode = D3D11.FillMode.Solid,
                IsFrontCounterClockwise = false,
                IsMultisampleEnabled = false,
                IsScissorEnabled = false,
                SlopeScaledDepthBias = 0.0f
            };

            rasterizerState = new D3D11.RasterizerState( device, rasterDesc );
        }

        private void InitializeShaders()
        {
            ConstantBufferCPU data = new ConstantBufferCPU();
            data.offsetScale = Vector4.Zero;
            data.color = Vector3.One;
            data.time = 0.0f;
            constantBuffer = D3D11.Buffer.Create( device, D3D11.BindFlags.ConstantBuffer, ref data );

            using ( var vertexShaderByteCode = ShaderBytecode.CompileFromFile("Shaders\\MainVS.hlsl", "main", "vs_4_0", ShaderFlags.Debug | ShaderFlags.WarningsAreErrors, include : new StreamInclude() ) )
            {
                Debug.Assert( vertexShaderByteCode.Bytecode != null, vertexShaderByteCode.Message );

                vertexShader = new D3D11.VertexShader( device, vertexShaderByteCode );

                using( var inputSignature = ShaderSignature.GetInputSignature( vertexShaderByteCode ) )
                    inputLayout = new D3D11.InputLayout( device, inputSignature, inputElements );
            }

            using ( var pixelShaderByteCode = ShaderBytecode.CompileFromFile("Shaders\\MainPS.hlsl", "main", "ps_4_0", ShaderFlags.Debug | ShaderFlags.WarningsAreErrors, include : new StreamInclude() ) )
            {
                Debug.Assert( pixelShaderByteCode.Bytecode != null, pixelShaderByteCode.Message );

                pixelShader = new D3D11.PixelShader( device, pixelShaderByteCode );
            }
        }

        private void InitializeTriangle()
        {
            trianglePositionVertexBuffer = D3D11.Buffer.Create<Vector3>( device, D3D11.BindFlags.VertexBuffer, vertexPositions );
            triangleColorVertexBuffer = D3D11.Buffer.Create<Vector3>(device, D3D11.BindFlags.VertexBuffer, vertexColor);
        }

        public void Dispose()
        {
            backbufferTexture.Dispose();
            backbufferRTV.Dispose();
            depthDSV.Dispose();

            depthStencilState.Dispose();
            rasterizerState.Dispose();

            constantBuffer.Dispose();
            vertexShader.Dispose();
            pixelShader.Dispose();

            trianglePositionVertexBuffer.Dispose();

            softwareRasterizer.Dispose();

            inputLayout.Dispose();
        }

        public void SwitchMode()
        {
            mode = mode == RenderMode.RenderModeHardware ? RenderMode.RenderModeSoftware : RenderMode.RenderModeHardware;
        }

        public void Render()
        {
            Viewport viewport = new Viewport( 0, 0, width, height );

            int elementSize = Utilities.SizeOf<Vector3>();
            int vertexCount = trianglePositionVertexBuffer.Description.SizeInBytes / elementSize;

            if ( mode == RenderMode.RenderModeHardware )
            {
                deviceContext.InputAssembler.SetVertexBuffers( 0, new D3D11.VertexBufferBinding( trianglePositionVertexBuffer, elementSize, 0 ) );
                deviceContext.InputAssembler.SetVertexBuffers(1, new D3D11.VertexBufferBinding( triangleColorVertexBuffer, elementSize, 0));

                deviceContext.InputAssembler.InputLayout = inputLayout;
                deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;

                deviceContext.VertexShader.Set( vertexShader );
                deviceContext.PixelShader.Set( pixelShader );

                deviceContext.Rasterizer.SetViewport( viewport );
                deviceContext.Rasterizer.State = rasterizerState;

                deviceContext.OutputMerger.SetDepthStencilState( depthStencilState, 1 );
                deviceContext.OutputMerger.SetRenderTargets( depthDSV, backbufferRTV );

                deviceContext.ClearDepthStencilView( depthDSV, D3D11.DepthStencilClearFlags.Depth, 1.0f, 0 );
                deviceContext.ClearRenderTargetView( backbufferRTV, new SharpDX.Color( 32, 103, 178 ) );
            }
            else
            {
                softwareRasterizer.Clear( new Color( 32, 103, 178 ) );
            }

            #if MULTIPLE_TRIANGLES
            Random random = new Random( 0 );

            var triangleCount = 32;
            var shuffle = Enumerable.Range( 1, triangleCount ).OrderBy( r => random.Next() ).ToArray();

            for ( int i = 0; i < triangleCount; i++ )
            {
                int ii = i;
                #if SHUFFLE_TRIANGLES
                ii = shuffle[i];
                #endif

                float s = 1.0f - (float) ii / triangleCount;
                float angle = 2.0f * (float) Math.PI * s;
                float radius = 0.5f;

                Vector4 offsetScale = Vector4.Zero;
                offsetScale.X = radius * (float) Math.Sin( angle) ;
                offsetScale.Y = radius * (float) Math.Cos( angle ); 
                offsetScale.Y = radius * (float) Math.Cos( angle );

                //Rotate triangle
                /*for(int ix=0;ix<vertexPositions.Length;ix++)
                {

                    float r = (float)Math.Sqrt((vertexPositions[ix].X) * (vertexPositions[ix].X) + (vertexPositions[ix].Y) * (vertexPositions[ix].Y));
                    float newAngle = (float)Math.Atan2((vertexPositions[ix].Y), (vertexPositions[ix].X)) + stopWatch.ElapsedMilliseconds / 1000.0f; //+ stopWatch.ElapsedMilliseconds / 1000.0f;
                    vertexPositions[ix].X = r * (float)Math.Sin(newAngle);
                    vertexPositions[ix].Y = r * (float)Math.Cos(newAngle);

                }*/


#if NON_PLANAR_TRIANGLES
                offsetScale.Z = 0.0f;
#else
                offsetScale.Z = 0.5f * s;
                #endif
                offsetScale.W = 0.35f;

                Vector3 color = new Vector3( s * random.NextFloat( 0.0f, 1.0f ), s * random.NextFloat( 0.0f, 1.0f ), s * random.NextFloat( 0.0f, 1.0f ) );

                if ( mode == RenderMode.RenderModeHardware )
                {
                    UpdateConstantBuffer( offsetScale, color );
                    deviceContext.Draw( vertexCount, 0 );
                }
                else
                {
                    SoftwareRasterizer.ConstantBuffer softwareRasterizerConstantBuffer = new SoftwareRasterizer.ConstantBuffer();
                    softwareRasterizerConstantBuffer.offsetScale = offsetScale;
                    softwareRasterizerConstantBuffer.color = color;
                    softwareRasterizer.Draw( vertexPositions, viewport, softwareRasterizerConstantBuffer );
                }
            }
            #else
            Vector4 offsetScale = new Vector4( 0.0f, 0.0f, 0.0f, 1.0f );
            Vector3 offsetColor = Vector3.One;

            if ( mode == RenderMode.RenderModeHardware )
            {
                UpdateConstantBuffer( offsetScale, offsetColor);
                deviceContext.Draw( vertexCount, 0 );
            }
            else
            {
                SoftwareRasterizer.ConstantBuffer softwareRasterizerConstantBuffer = new SoftwareRasterizer.ConstantBuffer();
                softwareRasterizerConstantBuffer.offsetScale = offsetScale;
                softwareRasterizerConstantBuffer.color = offsetColor;
                softwareRasterizer.Draw( vertexPositions, viewport, softwareRasterizerConstantBuffer );
            }
            #endif
        }

        private void UpdateConstantBuffer( Vector4 offsetScale, Vector3 color )
        {
            ConstantBufferCPU data = new ConstantBufferCPU();
            data.offsetScale = offsetScale;
            data.color = color;
            data.time = stopWatch.ElapsedMilliseconds / 1000.0f;
            
            deviceContext.UpdateSubresource( ref data, constantBuffer );

            deviceContext.VertexShader.SetConstantBuffer( 0, constantBuffer );
            deviceContext.PixelShader.SetConstantBuffer( 0, constantBuffer );
        }
    }
}
