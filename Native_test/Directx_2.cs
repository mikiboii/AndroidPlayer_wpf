// using System;
// using SharpDX;
// using SharpDX.Direct3D;
// using SharpDX.Direct3D11;
// using SharpDX.DXGI;
// using Device    = SharpDX.Direct3D11.Device;
// using Resource  = SharpDX.Direct3D11.Resource;
//
//
//
//
// namespace Android_player_2.Native_test
// {
//     public class Directx_2 : IDisposable
//     {
//         public Device _device;
//         public DeviceContext _context;
//         public SwapChain _swapChain;
//
//         private Texture2D _backBuffer;
//         private RenderTargetView _renderView;
//
//         private int _width;
//         private int _height;
//
//         public Directx_2(IntPtr hwnd, int width = 1920, int height = 1080)
//         {
//             _width = width;
//             _height = height;
//
//             // Create swap chain description
//             var desc = new SwapChainDescription()
//             {
//                 BufferCount = 2,
//                 ModeDescription = new ModeDescription(
//                     width, height,
//                     new Rational(60, 1),
//                     Format.B8G8R8A8_UNorm),
//                 Usage = Usage.RenderTargetOutput,
//                 OutputHandle = hwnd,
//                 IsWindowed = true,
//                 SampleDescription = new SampleDescription(1, 0),
//                 SwapEffect = SwapEffect.FlipSequential,
//                 Flags = SwapChainFlags.AllowModeSwitch
//             };
//
//             // Create device + swap chain
//             Device.CreateWithSwapChain(
//                 DriverType.Hardware,
//                 DeviceCreationFlags.BgraSupport,
//                 desc,
//                 out _device,
//                 out _swapChain);
//
//             _context = _device.ImmediateContext;
//
//             // Create frame buffer
//             _backBuffer = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);
//             _renderView = new RenderTargetView(_device, _backBuffer);
//         }
//
//         // Present a GPU texture (NV12 or BGRA)
//         public void PresentFrame(Texture2D gpuTexture)
//         {
//             if (gpuTexture == null)
//                 return;
//
//             // Copy decoded texture to swap chain buffer
//             _context.CopyResource(_backBuffer, gpuTexture);
//
//             // Commit to screen
//             _swapChain.Present(1, PresentFlags.None);
//         }
//
//         // Create an empty texture (NV12, BGRA etc.)
//         public Texture2D CreateTexture(int width, int height, Format format)
//         {
//             Texture2DDescription desc = new Texture2DDescription()
//             {
//                 Width = width,
//                 Height = height,
//                 ArraySize = 1,
//                 MipLevels = 1,
//                 Format = format,
//                 BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
//                 Usage = ResourceUsage.Default,
//                 CpuAccessFlags = CpuAccessFlags.None,
//                 SampleDescription = new SampleDescription(1, 0),
//             };
//
//             return new Texture2D(_device, desc);
//         }
//
//         public void Dispose()
//         {
//             _renderView?.Dispose();
//             _backBuffer?.Dispose();
//             _swapChain?.Dispose();
//             _context?.Dispose();
//             _device?.Dispose();
//         }
//     }
// }


using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D11.Device;

namespace Androidplayer_wpf.Native_test
{
    public class Directx_2 : IDisposable
    {
        public Device _device;
        public DeviceContext _context;
        public SwapChain _swapChain;

        private Texture2D _backBuffer;
        private VideoDevice1 _videoDevice1;
        private VideoContext1 _videoContext1;
        private VideoProcessorEnumerator _vpe;
        private VideoProcessor _videoProcessor;
        private VideoProcessorInputViewDescription _vpivd;
        private VideoProcessorOutputViewDescription _vpovd;
        private VideoProcessorInputView _vpiv;
        private VideoProcessorOutputView _vpov;
        private VideoProcessorStream[] _vpsa;

        private int _width;
        private int _height;

        public Directx_2(IntPtr hwnd, int width = 1080, int height = 488)
        {
            _width = width;
            _height = height;

            try
            {
                var desc = new SwapChainDescription
                {
                    BufferCount = 2,
                    ModeDescription = new ModeDescription(width, height, new Rational(60, 1), Format.B8G8R8A8_UNorm),
                    Usage = Usage.RenderTargetOutput,
                    OutputHandle = hwnd,
                    IsWindowed = true,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.FlipSequential,
                    Flags = SwapChainFlags.AllowModeSwitch
                };

                Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.BgraSupport, desc, out _device, out _swapChain);
                _context = _device.ImmediateContext;

                _backBuffer = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);

                var factory = _swapChain.GetParent<Factory>();
                factory.MakeWindowAssociation(hwnd, WindowAssociationFlags.IgnoreAll);

                // VideoProcessor initialization
                _videoDevice1 = _device.QueryInterface<VideoDevice1>();
                _videoContext1 = _device.ImmediateContext.QueryInterface<VideoContext1>();

                var vpcd = new VideoProcessorContentDescription
                {
                    Usage = VideoUsage.PlaybackNormal,
                    InputFrameFormat = VideoFrameFormat.Progressive,
                    InputFrameRate = new Rational(1, 1),
                    OutputFrameRate = new Rational(1, 1),
                    InputWidth = width,
                    OutputWidth = width,
                    InputHeight = height,
                    OutputHeight = height
                };

                _videoDevice1.CreateVideoProcessorEnumerator(ref vpcd, out _vpe);
                _videoDevice1.CreateVideoProcessor(_vpe, 0, out _videoProcessor);

                _vpivd = new VideoProcessorInputViewDescription
                {
                    Dimension = VpivDimension.Texture2D,
                    Texture2D = new Texture2DVpiv { MipSlice = 0, ArraySlice = 0 }
                };

                _vpovd = new VideoProcessorOutputViewDescription
                {
                    Dimension = VpovDimension.Texture2D,
                    Texture2D = new Texture2DVpov { MipSlice = 0 }
                };

                _videoDevice1.CreateVideoProcessorOutputView(_backBuffer, _vpe, _vpovd, out _vpov);
                _vpsa = new VideoProcessorStream[1];

                Console.WriteLine($"[DirectX] Initialized Directx_2 with backbuffer {width}x{height}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DirectX] Initialization failed: {ex.Message}");
                throw;
            }
        }

        // Present NV12 GPU texture using VideoProcessorBlt
        // public void PresentFrame(Texture2D nv12Texture)
        // {
        //     if (nv12Texture == null) return;
        //
        //     try
        //     {
        //         // Create VideoProcessorInputView for the NV12 texture
        //         _videoDevice1.CreateVideoProcessorInputView(nv12Texture, _vpe, _vpivd, out _vpiv);
        //
        //         var vps = new VideoProcessorStream
        //         {
        //             PInputSurface = _vpiv,
        //             Enable = new RawBool(true)
        //         };
        //         _vpsa[0] = vps;
        //
        //         // Clear backbuffer to black before presenting
        //         _context.ClearRenderTargetView(new RenderTargetView(_device, _backBuffer), new RawColor4(0, 0, 0, 1));
        //
        //         _videoContext1.VideoProcessorBlt(_videoProcessor, _vpov, 0, 1, _vpsa);
        //         _swapChain.Present(1, PresentFlags.None);
        //
        //         // Console.WriteLine($"[DirectX] PresentFrame called. Texture: {nv12Texture.Description.Width}x{nv12Texture.Description.Height}, Format: {nv12Texture.Description.Format}");
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"[DirectX] PresentFrame error: {ex.Message}");
        //     }
        //     finally
        //     {
        //         Utilities.Dispose(ref _vpiv);
        //     }
        // }
        
        
        private VideoProcessorInputView _vpivCached;
        private int _vpivWidth = 0;
        private int _vpivHeight = 0;

        public void PresentFrame(Texture2D nv12Texture)
        {
            if (nv12Texture == null) return;

            // Only recreate input view if resolution changes
            if (_vpivCached == null || _vpivWidth != nv12Texture.Description.Width || _vpivHeight != nv12Texture.Description.Height)
            {
                Utilities.Dispose(ref _vpivCached);

                _vpivd.Texture2D.MipSlice = 0;
                _vpivd.Texture2D.ArraySlice = 0;

                _videoDevice1.CreateVideoProcessorInputView(nv12Texture, _vpe, _vpivd, out _vpivCached);

                _vpivWidth = nv12Texture.Description.Width;
                _vpivHeight = nv12Texture.Description.Height;
            }

            var vps = new VideoProcessorStream
            {
                PInputSurface = _vpivCached,
                Enable = new RawBool(true)
            };
            _vpsa[0] = vps;

            _videoContext1.VideoProcessorBlt(_videoProcessor, _vpov, 0, 1, _vpsa);
            // _swapChain.Present(1, PresentFlags.None);
            
            
            _swapChain.Present(0, PresentFlags.DoNotWait);
        }


        // Resize swapchain and video processor
        public void Resize(int width, int height)
        {
            if (_swapChain == null || _backBuffer == null) return;
            try
            {
                width = Math.Max(width, 1);
                height = Math.Max(height, 1);

                Utilities.Dispose(ref _backBuffer);
                Utilities.Dispose(ref _vpov);

                _swapChain.ResizeBuffers(2, width, height, Format.B8G8R8A8_UNorm, SwapChainFlags.AllowModeSwitch);
                _backBuffer = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);

                var vpcd = new VideoProcessorContentDescription
                {
                    Usage = VideoUsage.PlaybackNormal,
                    InputFrameFormat = VideoFrameFormat.Progressive,
                    InputFrameRate = new Rational(1, 1),
                    OutputFrameRate = new Rational(1, 1),
                    InputWidth = width,
                    OutputWidth = width,
                    InputHeight = height,
                    OutputHeight = height
                };

                Utilities.Dispose(ref _vpe);
                Utilities.Dispose(ref _videoProcessor);

                _videoDevice1.CreateVideoProcessorEnumerator(ref vpcd, out _vpe);
                _videoDevice1.CreateVideoProcessor(_vpe, 0, out _videoProcessor);

                _videoDevice1.CreateVideoProcessorOutputView(_backBuffer, _vpe, _vpovd, out _vpov);

                Console.WriteLine($"[DirectX] Resize: {width}x{height}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DirectX] Resize failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Utilities.Dispose(ref _vpov);
            Utilities.Dispose(ref _videoProcessor);
            Utilities.Dispose(ref _vpe);
            Utilities.Dispose(ref _videoContext1);
            Utilities.Dispose(ref _videoDevice1);
            Utilities.Dispose(ref _backBuffer);
            Utilities.Dispose(ref _swapChain);
            Utilities.Dispose(ref _device);
        }
    }
}
