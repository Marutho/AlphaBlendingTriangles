﻿namespace Demo
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    
    using SharpDX.Direct3D;
    using SharpDX.Windows;

    using D3D11 = SharpDX.Direct3D11;
    using DXGI = SharpDX.DXGI;

    public class Program : IDisposable
    {
        private const string Title = "Demo2";

        private const int Width = 1280;
        private const int Height = 720;

        private RenderForm renderForm;
        private DXGI.SwapChain swapChain;
        private D3D11.Device device;
        private Renderer renderer;

        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]
        static extern int LoadLibrary( [MarshalAs( UnmanagedType.LPStr )] string lpLibFileName );

        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
        static extern bool FreeLibrary( int hModule );

        public static void Main( string[] args )
        {
            int hmod = Environment.Is64BitProcess ? LoadLibrary( "x64\\d3dcompiler_47.dll" ) : LoadLibrary( "x86\\d3dcompiler_47.dll" );
            Debug.Assert( hmod != 0 );

            using ( Program program = new Program() )
            {
                program.renderForm.KeyDown += ( s, e ) => program.KeyDownCallback( e.KeyCode );

                program.renderForm.KeyUp += (s, e) => program.KeyUpCallback(e.KeyCode);

                RenderLoop.Run( program.renderForm, program.RenderCallback );
            }

            FreeLibrary( hmod );
        }

        private Program()
        {
            SharpDX.Configuration.EnableObjectTracking = true;

            renderForm = new RenderForm( Title );
            renderForm.ClientSize = new Size( Width, Height );
            renderForm.AllowUserResizing = false;

            InitializeSwapChain();

            renderer = new Renderer( device, swapChain );

            SetTitle();
        }

        private void InitializeSwapChain()
        {
            DXGI.ModeDescription backBufferDesc = new DXGI.ModeDescription( Width, Height, new DXGI.Rational( 60, 1 ), DXGI.Format.R8G8B8A8_UNorm );

            DXGI.SwapChainDescription swapChainDesc = new DXGI.SwapChainDescription()
            {
                ModeDescription = backBufferDesc,
                SampleDescription = new DXGI.SampleDescription( 1, 0 ),
                Usage = DXGI.Usage.RenderTargetOutput,
                BufferCount = 1,
                OutputHandle = renderForm.Handle,
                IsWindowed = true
            };

            D3D11.Device.CreateWithSwapChain( DriverType.Hardware, D3D11.DeviceCreationFlags.None, swapChainDesc, out device, out swapChain );
        }

        private void SetTitle()
        {
            renderForm.Text = Title + " : ";

            renderForm.Text += renderer.mode == Renderer.RenderMode.RenderModeSoftware ? "(F1) Software" : "(F1) Hardware";

            if ( renderer.mode == Renderer.RenderMode.RenderModeSoftware )
            {
                switch ( renderer.softwareRasterizer.rasterizerMode )
                {
                    case SoftwareRasterizer.RasterizerMode.Top:
                        renderForm.Text += " : (F5) Top Rasterization";
                        break;
                    case SoftwareRasterizer.RasterizerMode.Bottom:
                        renderForm.Text += " : (F5) Bottom Rasterization";
                        break;
                    case SoftwareRasterizer.RasterizerMode.Both:
                        renderForm.Text += " : (F5) Both Rasterization";
                        break;
                }

                renderForm.Text += renderer.softwareRasterizer.outputMode == SoftwareRasterizer.OutputMode.Color ? " : (F6) Color" : " : (F6) Depth";
            }
        }

        public void Dispose()
        {
            renderForm.Dispose();
            swapChain.Dispose();
            device.Dispose();
            renderer.Dispose();

            Console.Write( SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects() );
        }

        private void KeyDownCallback( Keys key )
        {
            if ( key == Keys.Escape )
                renderForm.Dispose();

            if ( key == Keys.F1 )
                renderer.SwitchMode();

            if ( key == Keys.F5 )
                renderer.softwareRasterizer.SwitchMode();

            if ( key == Keys.F6 )
                renderer.softwareRasterizer.SwitchOutput();

            if (key == Keys.A)
                renderer.SetCameraSwitch(0, true);

            if (key == Keys.D)
                renderer.SetCameraSwitch(1, true);

            if (key == Keys.W)
                renderer.SetCameraSwitch(2, true);

            if (key == Keys.S)
                renderer.SetCameraSwitch(3, true);

            if (key == Keys.D1)
            {
                renderer.SetAlphaSwitch(0, true);
                renderer.SetAlphaSwitch(1, false);
                renderer.SetAlphaSwitch(2, false);
                renderer.SetAlphaSwitch(3, false);
                renderer.SetAlphaSwitch(4, false);
            }

            if (key == Keys.D2)
            {
                renderer.SetAlphaSwitch(0, false);
                renderer.SetAlphaSwitch(1, true);
                renderer.SetAlphaSwitch(2, false);
                renderer.SetAlphaSwitch(3, false);
                renderer.SetAlphaSwitch(4, false);
            }

            if (key == Keys.D3)
            {
                renderer.SetAlphaSwitch(0, false);
                renderer.SetAlphaSwitch(1, false);
                renderer.SetAlphaSwitch(2, true);
                renderer.SetAlphaSwitch(3, false);
                renderer.SetAlphaSwitch(4, false);
            }

            if (key == Keys.D4)
            {
                renderer.SetAlphaSwitch(0, false);
                renderer.SetAlphaSwitch(1, false);
                renderer.SetAlphaSwitch(2, false);
                renderer.SetAlphaSwitch(3, true);
                renderer.SetAlphaSwitch(4, false);
            }

            if (key == Keys.D5)
            {
                renderer.SetAlphaSwitch(0, false);
                renderer.SetAlphaSwitch(1, false);
                renderer.SetAlphaSwitch(2, false);
                renderer.SetAlphaSwitch(3, false);
                renderer.SetAlphaSwitch(4, true);
            }



            SetTitle();
        }


        private void KeyUpCallback(Keys key)
        {
            if (key == Keys.A)
                renderer.SetCameraSwitch(0, false);

            if (key == Keys.D)
                renderer.SetCameraSwitch(1, false);

            if (key == Keys.W)
                renderer.SetCameraSwitch(2, false);

            if (key == Keys.S)
                renderer.SetCameraSwitch(3, false);




        }

        private void RenderCallback()
        {
            renderer.Render();
            swapChain.Present( 1, DXGI.PresentFlags.None );
        }
    }
}
