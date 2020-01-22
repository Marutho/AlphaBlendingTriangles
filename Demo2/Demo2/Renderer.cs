#define RENDER_CUBE
#define RENDER_CUBE_PERSPECTIVE
#define MULTIPLE_PYRAMIDS
//#define ALPHABLENDING
#define ADDITIVEBLENDING

//#define TRIANGLE_3D
#define PYRAMID_3D

namespace Demo
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using SharpDX;

    using D3D11 = SharpDX.Direct3D11;
    using DXGI = SharpDX.DXGI;

    using static System.Math;
    using static CoreMath;
    using SharpDX.D3DCompiler;

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

        //BLEND STATE
        private D3D11.BlendState blendState;



        private D3D11.Buffer trianglePositionVertexBuffer;
        private D3D11.Buffer triangleColorVertexBuffer;
        #if RENDER_CUBE
        private D3D11.Buffer triangleIndexBuffer;
#endif


        public bool[] CameraSwitch = new bool[4];
        public bool[] AlphaSwitch = new bool[5];
        private const float SPEED_CAMERA = 0.01f;

        [StructLayout( LayoutKind.Sequential )]
        private struct ConstantBufferCPU
        {
            public Matrix worldViewProjectionMatrix;
            public float time;
            public Vector3 padding;
            public Vector4 alpha;
        };
        private D3D11.Buffer constantBuffer;

        private D3D11.VertexShader vertexShader;
        private D3D11.PixelShader pixelShader;

        private D3D11.InputElement[] inputElements = new D3D11.InputElement[]
        {
            new D3D11.InputElement( "POSITION", 0, DXGI.Format.R32G32B32_Float, 0 ),
            new D3D11.InputElement( "COLOR",    0, DXGI.Format.R32G32B32_Float, 1 )
        };

#if RENDER_CUBE

#if TRIANGLE_3D
        Vector3[] vertexPositions = new[]
        {
            // Base
            new Vector3( -0.5f, 0.0f, -0.0f ),
            new Vector3( 0.5f, 0.0f, -0.0f ),
        
            //Top
            new Vector3( 0.0f, 1.0f, 0.0f )
        };
#elif PYRAMID_3D
        Vector3[] vertexPositions = new[]
        {
            // Base
            new Vector3( -0.5f, 0.0f, 0.5f ),
            new Vector3( 0.5f, 0.0f, 0.5f ),
            new Vector3( -0.5f, 0.0f, -0.5f ),
            new Vector3( 0.5f, 0.0f, -0.5f ),
        
            //Top
            new Vector3( 0.0f, 1.0f, 0.0f )
        };

#endif

        #if ALPHABLENDING
        Vector3[] vertexColors = new[]
        {
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            //TOP
            new Vector3(1.0f, 0.0f, 0.0f)
        };
#endif

    #if ADDITIVEBLENDING
        Vector3[] vertexColors = new[]
        {
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 1.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            //TOP
            new Vector3(1.0f, 1.0f, 1.0f)
        };
#endif

        int[] vertexIndices = new int[]
        {
             0, 2, 1,
             1, 2, 3,
             0, 1, 4,
             1, 3, 4,
             3, 2, 4,
             2, 0, 4,
        };

#else
        Vector3[] vertexPositions = new Vector3[] { new Vector3( 0.5f, -0.5f, 0.0f ), new Vector3( -0.5f, -0.5f, 0.0f ), new Vector3( 0.0f, 0.5f, 0.0f ) };
        Vector3[] vertexColors    = new Vector3[] { new Vector3( 1.0f, 0.0f, 0.0f ),  new Vector3( 0.0f, 1.0f,  0.0f ),  new Vector3( 0.0f , 0.0f, 1.0f ) };
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
                IsDepthClipEnabled = false,
                FillMode = D3D11.FillMode.Solid,
                IsFrontCounterClockwise = false,
                IsMultisampleEnabled = false,
                IsScissorEnabled = false,
                SlopeScaledDepthBias = 0.0f
            };

            /*var blendDesc = new D3D11.BlendStateDescription();
            blendDesc.RenderTarget[0].IsBlendEnabled = true;
            blendDesc.RenderTarget[0].SourceBlend = D3D11.BlendOption.SourceAlpha;
            blendDesc.RenderTarget[0].DestinationBlend = D3D11.BlendOption.InverseSourceAlpha;
            blendDesc.RenderTarget[0].BlendOperation = D3D11.BlendOperation.Add;
            blendDesc.RenderTarget[0].SourceAlphaBlend = D3D11.BlendOption.Zero;
            blendDesc.RenderTarget[0].DestinationAlphaBlend = D3D11.BlendOption.Zero;
            blendDesc.RenderTarget[0].AlphaBlendOperation = D3D11.BlendOperation.Add;
            blendDesc.RenderTarget[0].RenderTargetWriteMask = D3D11.ColorWriteMaskFlags.All;*/

#if ALPHABLENDING
            var blendDesc = new D3D11.BlendStateDescription();
            blendDesc.RenderTarget[0].IsBlendEnabled = true;
            blendDesc.RenderTarget[0].SourceBlend = D3D11.BlendOption.SourceAlpha;
            blendDesc.RenderTarget[0].DestinationBlend = D3D11.BlendOption.InverseSourceAlpha;
            blendDesc.RenderTarget[0].BlendOperation = D3D11.BlendOperation.Add;
            blendDesc.RenderTarget[0].SourceAlphaBlend = D3D11.BlendOption.Zero;
            blendDesc.RenderTarget[0].DestinationAlphaBlend = D3D11.BlendOption.One;
            blendDesc.RenderTarget[0].AlphaBlendOperation = D3D11.BlendOperation.Add;
            blendDesc.RenderTarget[0].RenderTargetWriteMask = D3D11.ColorWriteMaskFlags.All;
#endif

#if ADDITIVEBLENDING
            var blendDesc = new D3D11.BlendStateDescription();
            blendDesc.RenderTarget[0].IsBlendEnabled = true;
            blendDesc.RenderTarget[0].SourceBlend = D3D11.BlendOption.SourceAlpha;
            blendDesc.RenderTarget[0].DestinationBlend = D3D11.BlendOption.DestinationAlpha;
            blendDesc.RenderTarget[0].BlendOperation = D3D11.BlendOperation.Add;
            blendDesc.RenderTarget[0].SourceAlphaBlend = D3D11.BlendOption.One;
            blendDesc.RenderTarget[0].DestinationAlphaBlend = D3D11.BlendOption.One;
            blendDesc.RenderTarget[0].AlphaBlendOperation = D3D11.BlendOperation.Add;
            blendDesc.RenderTarget[0].RenderTargetWriteMask = D3D11.ColorWriteMaskFlags.All;
#endif




            //     RenderTarget[0].BlendOpSharpDX.Direct3D11.BlendOperation.Add RenderTarget[0].SrcBlendAlphaSharpDX.Direct3D11.BlendOption.One
            //     RenderTarget[0].DestBlendAlphaSharpDX.Direct3D11.BlendOption.Zero RenderTarget[0].BlendOpAlphaSharpDX.Direct3D11.BlendOperation.Add
            //     RenderTarget[0].RenderTargetWriteMaskSharpDX.Direct3D11.ColorWriteMaskFlags.All


            blendState = new D3D11.BlendState(device, blendDesc);
            rasterizerState = new D3D11.RasterizerState( device, rasterDesc );
        }

        private void InitializeShaders()
        {
            ConstantBufferCPU data = new ConstantBufferCPU();
            data.worldViewProjectionMatrix = Matrix.Identity;
            data.time = 0.0f;
            data.padding = Vector3.Zero;
            constantBuffer = D3D11.Buffer.Create( device, D3D11.BindFlags.ConstantBuffer, ref data );

            string textVS = System.IO.File.ReadAllText( "Shaders\\MainVS.hlsl" );
            using ( var vertexShaderByteCode = ShaderBytecode.Compile( textVS, "main", "vs_4_0", ShaderFlags.Debug | ShaderFlags.WarningsAreErrors, EffectFlags.None, null, new StreamInclude(), sourceFileName : "Shaders\\MainVS.hlsl" ) )
            {
                if ( vertexShaderByteCode.Bytecode == null )
                {
                    MessageBox.Show( vertexShaderByteCode.Message, "Shader Compilation Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                    System.Environment.Exit( -1 );
                }

                vertexShader = new D3D11.VertexShader( device, vertexShaderByteCode );

                using( var inputSignature = ShaderSignature.GetInputSignature( vertexShaderByteCode ) )
                    inputLayout = new D3D11.InputLayout( device, inputSignature, inputElements );
            }

            string textPS = System.IO.File.ReadAllText( "Shaders\\MainPS.hlsl" );
            using ( var pixelShaderByteCode = ShaderBytecode.Compile( textPS, "main", "ps_4_0", ShaderFlags.Debug | ShaderFlags.WarningsAreErrors, EffectFlags.None, null, new StreamInclude(), sourceFileName : "Shaders\\MainPS.hlsl" ) )
            {
                if ( pixelShaderByteCode.Bytecode == null )
                {
                    MessageBox.Show( pixelShaderByteCode.Message, "Shader Compilation Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                    System.Environment.Exit( -1 );
                }

                pixelShader = new D3D11.PixelShader( device, pixelShaderByteCode );
            }
        }

        private void InitializeTriangle()
        {
            trianglePositionVertexBuffer = D3D11.Buffer.Create<Vector3>( device, D3D11.BindFlags.VertexBuffer, vertexPositions );
            triangleColorVertexBuffer = D3D11.Buffer.Create<Vector3>( device, D3D11.BindFlags.VertexBuffer, vertexColors );
            #if RENDER_CUBE
            triangleIndexBuffer = D3D11.Buffer.Create<int>( device, D3D11.BindFlags.IndexBuffer, vertexIndices );
            #endif
        }

        public void Dispose()
        {
            backbufferTexture.Dispose();
            backbufferRTV.Dispose();
            depthDSV.Dispose();

            depthStencilState.Dispose();
            rasterizerState.Dispose();
            blendState.Dispose();

            constantBuffer.Dispose();
            vertexShader.Dispose();
            pixelShader.Dispose();

            trianglePositionVertexBuffer.Dispose();
            triangleColorVertexBuffer.Dispose();
            #if RENDER_CUBE
            triangleIndexBuffer.Dispose();
            #endif

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

            #if RENDER_CUBE
            ConstantBufferCPU data = GetConstantBufferForCube();
            #else
            ConstantBufferCPU data = GetConstantBufferForTriangle();
            #endif

            int vertexSize = Utilities.SizeOf<Vector3>();
            int vertexCount = trianglePositionVertexBuffer.Description.SizeInBytes / vertexSize;

            if ( mode == RenderMode.RenderModeHardware )
            {
                deviceContext.InputAssembler.SetVertexBuffers( 0, new D3D11.VertexBufferBinding( trianglePositionVertexBuffer, vertexSize, 0 ) );
                deviceContext.InputAssembler.SetVertexBuffers( 1, new D3D11.VertexBufferBinding( triangleColorVertexBuffer,    vertexSize, 0 ) );
                deviceContext.InputAssembler.InputLayout = inputLayout;
                deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;

                deviceContext.VertexShader.Set( vertexShader );
                deviceContext.PixelShader.Set( pixelShader );

                deviceContext.Rasterizer.SetViewport( viewport );
                deviceContext.Rasterizer.State = rasterizerState;

                deviceContext.OutputMerger.SetDepthStencilState( depthStencilState, 1 );
                deviceContext.OutputMerger.SetRenderTargets( depthDSV, backbufferRTV );
                
                //alpha blending
                deviceContext.OutputMerger.SetBlendState(blendState, new SharpDX.Color4(0.0f,0.0f,0.0f,0.0f), 0xFFFFFFFF); 

                deviceContext.ClearDepthStencilView( depthDSV, D3D11.DepthStencilClearFlags.Depth, 1.0f, 0 );
                deviceContext.ClearRenderTargetView( backbufferRTV, new SharpDX.Color( 32, 103, 178 ) );
            }
            else
            {
                softwareRasterizer.Clear( new Color( 32, 103, 178 ) );
            }


            #if MULTIPLE_PYRAMIDS

            if ( mode == RenderMode.RenderModeHardware )
            {
                UpdateConstantBuffer( data );

            #if RENDER_CUBE
                int indexCount = triangleIndexBuffer.Description.SizeInBytes / Utilities.SizeOf<int>();
                deviceContext.InputAssembler.SetIndexBuffer( triangleIndexBuffer, DXGI.Format.R32_UInt, 0 );
                deviceContext.DrawIndexed( indexCount, 0, 0 );
            #else
                deviceContext.Draw( vertexCount, 0 );
            #endif
            }
            else
            {
                #if RENDER_CUBE
                // Software indexed draws not implemented
                #else
                softwareRasterizer.Draw( vertexPositions, viewport, data.worldViewProjectionMatrix );
                #endif

                softwareRasterizer.EndFrame();
            }
            #endif

        }

        public void SetCameraSwitch(int index, bool value)
        {
            CameraSwitch[index] = value;
        }

        public void SetAlphaSwitch(int index, bool value)
        {
            AlphaSwitch[index] = value;
        }

        public Matrix lastRotation = new Matrix(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );

        private ConstantBufferCPU GetConstantBufferForCube()
        {
            ConstantBufferCPU data;

            float time = stopWatch.ElapsedMilliseconds / 1000.0f;

            float aspectRatio = (float) width / height;

            Matrix rotation = new Matrix();

            float alphaValue = 0.0f;


            if (AlphaSwitch[0])
                alphaValue = 0.0f;

            if (AlphaSwitch[1])
                alphaValue = 0.25f;

            if (AlphaSwitch[2])
                alphaValue = 0.5f;

            if (AlphaSwitch[3])
                alphaValue = 0.75f;

            if (AlphaSwitch[4])
                alphaValue = 1.0f;

            if (CameraSwitch[0])
            {

                rotation = new Matrix(
               (float)Math.Cos(-SPEED_CAMERA), 0, (float)-Math.Sin(-SPEED_CAMERA), 0,
               0, 1, 0, 0,
               (float)Math.Sin(-SPEED_CAMERA), 0, (float)Math.Cos(-SPEED_CAMERA), 0,
               0, 0, 0, 1
               ) * lastRotation;
            }

            else if (CameraSwitch[1])
            {
                rotation = new Matrix(
               (float)Math.Cos(SPEED_CAMERA), 0, (float)-Math.Sin(SPEED_CAMERA), 0,
               0, 1, 0, 0,
               (float)Math.Sin(SPEED_CAMERA), 0, (float)Math.Cos(SPEED_CAMERA), 0,
               0, 0, 0, 1
               ) * lastRotation;
            }

            else if (CameraSwitch[2])
            {
                rotation = new Matrix(
               1, 0, 0, 0,
               0, (float)Math.Cos(-SPEED_CAMERA), (float)Math.Sin(-SPEED_CAMERA), 0,
               0, (float)-Math.Sin(-SPEED_CAMERA), (float)Math.Cos(-SPEED_CAMERA), 0,
               0, 0, 0, 1
               ) * lastRotation;
            }

            else if (CameraSwitch[3])
            {
                rotation = new Matrix(
               1, 0, 0, 0,
               0, (float)Math.Cos(SPEED_CAMERA), (float)Math.Sin(SPEED_CAMERA), 0,
               0, (float)-Math.Sin(SPEED_CAMERA), (float)Math.Cos(SPEED_CAMERA), 0,
               0, 0, 0, 1
               ) * lastRotation;
            }

            else
            {
                rotation = lastRotation;
            }

            Matrix rotationX = new Matrix(
               1, 0, 0, 0,
               0, (float)Math.Cos(time / 5), (float)Math.Sin(time / 5), 0,
               0, (float)-Math.Sin(time / 5), (float)Math.Cos(time / 5), 0,
               0, 0, 0, 1
               );
 
            Matrix rotationY = new Matrix(
               (float)Math.Cos(-time / 5), 0, (float)-Math.Sin(-time / 5), 0,
               0, 1, 0, 0,
               (float)Math.Sin(-time / 5), 0, (float)Math.Cos(-time / 5), 0,
               0, 0, 0, 1
               );

            Matrix rotationZ = new Matrix(
                (float)Math.Cos(time / 10), (float)Math.Sin(time / 10), 0, 0,
                (float)-Math.Sin(time / 10), (float)Math.Cos(time / 10), 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
                );


            Matrix worldMatrix = Matrix.Scaling( 1.0f );
            
#if !RENDER_CUBE_PERSPECTIVE
            Matrix projectionMatrix = Matrix.OrthoLH( 3.0f, 3.0f, -2.0f, 2.0f );
            Matrix viewMatrix = Matrix.Identity;
#else
            var cameraPosition = new Vector3( 0.0f, 0.0f, -5.0f);
            var cameraTarget = Vector3.Zero;
            var cameraUp = Vector3.UnitY;

            lastRotation = rotation;
            Matrix viewMatrix = (lastRotation) * Matrix.LookAtLH(cameraPosition, cameraTarget, cameraUp);
            Matrix projectionMatrix = Matrix.PerspectiveFovLH( 2.0f * (float) PI * Remap( 0.0f, 360.0f, 45.0f ), aspectRatio, 0.01f, 1000.0f );
#endif
            data.worldViewProjectionMatrix = worldMatrix * viewMatrix * projectionMatrix;
            data.time = time;
            data.padding = Vector3.Zero;
            data.alpha = new Vector4(Vector3.Zero, alphaValue);
            return data;
        }

        private ConstantBufferCPU GetConstantBufferForTriangle()
        {
            ConstantBufferCPU data;

            float time = stopWatch.ElapsedMilliseconds / 1000.0f;

            //float angle = 15.0f;
            Matrix rotation = new Matrix(
                (float)Math.Cos(time) , (float)Math.Sin(time), 0, 0,
                (float)-Math.Sin(time), (float)Math.Cos(time), 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 1
                );
            data.worldViewProjectionMatrix = Matrix.Identity * Matrix.Scaling(0.5f / time) * rotation * Matrix.Translation(time/10, time/10, 0);

            data.time = time;
            data.padding = Vector3.Zero;
            data.alpha = new Vector4(Vector3.Zero, 0.5f);
            return data;
        }

        private void UpdateConstantBuffer( ConstantBufferCPU data )
        {
            deviceContext.UpdateSubresource( ref data, constantBuffer );

            deviceContext.VertexShader.SetConstantBuffer( 0, constantBuffer );
            deviceContext.PixelShader.SetConstantBuffer( 0, constantBuffer );
        }
    }
}
