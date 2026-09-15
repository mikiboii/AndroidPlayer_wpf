using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using static FFmpeg.AutoGen.ffmpeg;

namespace Androidplayer_wpf.Native_test
{
    public unsafe class del_decoder : IDisposable
    {
        #region Fields (mirror of your FFmpeg class)

        // FFmpeg contexts
        private AVFormatContext* fmtCtx = null;   // not used for raw-packet decode but kept for parity
        private AVCodecContext* vCodecCtx = null;
        private AVCodec* codec = null;

        // HW accel constants (same as your FFmpeg class)
        const int AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX = 0x01;
        const AVHWDeviceType HW_DEVICE = AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA;
        const AVPixelFormat HW_PIX_FMT = AVPixelFormat.AV_PIX_FMT_D3D11;

        // D3D/SharpDX resources
        private Device device;
        private AVBufferRef* hw_device_ctx = null;
        private Texture2DDescription textDescHW;
        private Texture2D textureHW = null;
        private Texture2D textureFFmpeg = null;

        // registering ffmpeg binaries
        public static bool alreadyRegister = false;

        // software fallback flag
        private bool hwInitialized = false;
        private bool useSoftwareFallback = false;

        #endregion

        #region Constructor / Init

        public del_decoder(Device device)
        {
            this.device = device ?? throw new ArgumentNullException(nameof(device));
            RegisterFFmpegBinaries();
            av_log_set_level(AV_LOG_ERROR);

            // Prepare codec (we expect raw H264 packets)
            codec = avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
            if (codec == null)
                throw new Exception("H264 codec not found in FFmpeg.");

            // allocate codec context
            vCodecCtx = avcodec_alloc_context3(null);
            if (vCodecCtx == null)
                throw new Exception("Failed to allocate AVCodecContext.");

            // attempt hardware initialization (D3D11VA)
            try
            {
                hw_device_ctx = av_hwdevice_ctx_alloc(HW_DEVICE);
                if (hw_device_ctx == null)
                    throw new Exception("av_hwdevice_ctx_alloc returned null.");

                // set the D3D11 device pointer inside hwctx's hwctx structure
                // This matches your original pattern:
                AVHWDeviceContext* device_ctx = (AVHWDeviceContext*)hw_device_ctx->data;
                // hwctx->hwctx points to driver-specific struct (AVD3D11VADeviceContext)
                AVD3D11VADeviceContext* d3d11va_device_ctx = (AVD3D11VADeviceContext*)device_ctx->hwctx;
                d3d11va_device_ctx->device = (ID3D11Device*)device.NativePointer;

                // initialize hardware device context
                int ret = av_hwdevice_ctx_init(hw_device_ctx);
                if (ret != 0)
                {
                    // cleanup and switch to software fallback
                    // av_buffer_unref(&hw_device_ctx);
                    hw_device_ctx = null;
                    useSoftwareFallback = true;
                    Console.WriteLine($"[del_decoder] av_hwdevice_ctx_init failed: {ErrorCodeToMsg(ret)} ({ret}). Falling back to software decode.");
                }
                else
                {
                    hwInitialized = true;
                }
            }
            catch (Exception ex)
            {
                // catch native access violations, etc.
                Console.WriteLine($"[del_decoder] HW init exception: {ex.Message}\n{ex.StackTrace}");
                if (hw_device_ctx != null)
                {
                    // av_buffer_unref(&hw_device_ctx);
                    hw_device_ctx = null;
                }
                useSoftwareFallback = true;
            }

            // finalize codec context and open codec
            if (!PrepareCodecContext())
            {
                Dispose();
                throw new Exception("Failed to prepare codec context.");
            }
        }

        private bool PrepareCodecContext()
        {
            int ret;

            // If hw initialized, let codec context use it
            if (hwInitialized && hw_device_ctx != null)
            {
                vCodecCtx->hw_device_ctx = av_buffer_ref(hw_device_ctx);
            }

            // Open context with codec
            ret = avcodec_open2(vCodecCtx, codec, null);
            if (ret < 0)
            {
                Console.WriteLine($"[del_decoder] avcodec_open2 failed: {ErrorCodeToMsg(ret)} ({ret})");
                return false;
            }

            // If hw, prepare texture description (we'll set width/height when we get first frame)
            textDescHW = new Texture2DDescription()
            {
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.Decoder,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                ArraySize = 1,
                MipLevels = 1,
                Width = 0,
                Height = 0
            };

            return true;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Feed a single raw H.264 packet (as received from scrcpy) and attempt to decode.
        /// Returns a SharpDX Texture2D (a copy on your Direct3D device) or null if no frame produced.
        /// Caller must Dispose the returned Texture2D when done.
        /// </summary>
        /// 
        
        
        
       
        
        
        
        
        public Texture2D DecodePacketToTexture(byte[] h264Packet)
        {
            if (h264Packet == null || h264Packet.Length == 0) return null;
            fixed (byte* p = h264Packet)
            {
                AVPacket* pkt = av_packet_alloc();
                av_init_packet(pkt);
                pkt->data = p;
                pkt->size = h264Packet.Length;
        
                int sendRet = avcodec_send_packet(vCodecCtx, pkt);
                if (sendRet < 0)
                {
                    // packet could not be sent (EAGAIN or other)
                    av_packet_unref(pkt);
                    return null;
                }
        
                AVFrame* frame = av_frame_alloc();
                int ret = avcodec_receive_frame(vCodecCtx, frame);
                
                if (ret == AVERROR_EOF || ret == AVERROR(EAGAIN)) { av_packet_unref(pkt); ret = 0; return null; }
                if (ret != 0) { if (frame != null) av_frame_free(&frame);
                    av_packet_unref(pkt); ret = 0; return null; }
        
                // if (recvRet == AVERROR_EAGAIN || recvRet == AVERROR_EOF || recvRet < 0)
                // {
                //     // no frame this time
                //     av_frame_free(&frame);
                //     av_packet_unref(pkt);
                //     return null;
                // }
        
                // At this point, we have a decoded frame.
                // If using HW D3D11VA, frame->data[0] contains pointer to ID3D11Texture2D and data[1] contains index
                Texture2D userTexture = null;
        
                try
                {
                    if (hwInitialized && frame->data[0] != null)
                    {
                        // Create SharpDX Texture2D wrapper around native pointer (FFmpeg's texture)
                        IntPtr ffTexPtr = (IntPtr)frame->data[0];
                        // wrapped texture (this does not increase refcount in SharpDX wrapper for the underlying resource)
                        textureFFmpeg = new Texture2D(ffTexPtr);
        
                        // ensure our textDescHW.Format and width/height set
                        textDescHW.Format = textureFFmpeg.Description.Format;
                        textDescHW.Width = textureFFmpeg.Description.Width;
                        textDescHW.Height = textureFFmpeg.Description.Height;
        
                        // create or recreate the device-side texture that we will return (copy target)
                        textureHW?.Dispose();
                        textureHW = new Texture2D(device, textDescHW);
        
                        // Copy subresource from FFmpeg's array texture to our texture
                        int arrayIndex = (int)frame->data[1];
                        device.ImmediateContext.CopySubresourceRegion(
                            textureFFmpeg,
                            arrayIndex,
                            new ResourceRegion(0, 0, 0, textureHW.Description.Width, textureHW.Description.Height, 1),
                            textureHW,
                            0);
        
                        // return a copy of textureHW (caller expects a Texture2D they own)
                        userTexture = new Texture2D(device, textureHW.Description);
                        device.ImmediateContext.CopyResource(textureHW, userTexture);
        
                        // cleanup intermediate wrappers
                        textureFFmpeg.Dispose();
                        textureFFmpeg = null;
                    }
                    else
                    {
                        // software fallback: convert AVFrame to RGB and upload to texture
                        // We'll convert using sws_scale to RGBA and create a SharpDX texture from raw bytes
                        userTexture = SoftwareFrameToTexture(frame);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[del_decoder] Error converting frame to texture: {ex.Message}");
                }
                finally
                {
                    av_frame_free(&frame);
                    av_packet_unref(pkt);
                }
        
                return userTexture;
            }
        }

        #endregion

        #region Software decode helper (fallback)

        private Texture2D SoftwareFrameToTexture(AVFrame* frame)
        {
            if (frame == null) return null;

            // Setup a software scaler: dest format RGBA (AV_PIX_FMT_RGBA)
            SwsContext* swsCtx = sws_getContext(
                frame->width,
                frame->height,
                (AVPixelFormat)frame->format,
                frame->width,
                frame->height,
                AVPixelFormat.AV_PIX_FMT_RGBA,
                ffmpeg.SWS_BILINEAR,
                null, null, null);

            if (swsCtx == null) return null;

            int dstStride = frame->width * 4;
            byte_ptrArray4 dstData = new byte_ptrArray4();
            int_array4 dstLinesize = new int_array4();
            byte[] managedBuffer = new byte[dstStride * frame->height];

            fixed (byte* dstBufPtr = managedBuffer)
            {
                dstData[0] = dstBufPtr;
                dstLinesize[0] = dstStride;

                sws_scale(swsCtx, frame->data, frame->linesize, 0, frame->height, dstData, dstLinesize);

                // create SharpDX texture description for RGBA
                var desc = new Texture2DDescription
                {
                    Width = frame->width,
                    Height = frame->height,
                    ArraySize = 1,
                    BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = Format.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default
                };

                // create staging texture and upload using UpdateSubresource
                var tex = new Texture2D(device, desc);

                // create DataBox and call UpdateSubresource
                DataBox db = new DataBox((IntPtr)dstBufPtr, dstStride, 0);
                device.ImmediateContext.UpdateSubresource(db, tex, 0);

                sws_freeContext(swsCtx);
                return tex;
            }
        }

        #endregion

        #region RegisterFFmpegBinaries / Helpers (copied & adapted)

        private void RegisterFFmpegBinaries()
        {
            if (alreadyRegister) return;
            alreadyRegister = true;

            // Attempt to find deps folder in ancestor paths (same logic as your FFmpeg class)
            var current = @"c:\";
            var probe = Path.Combine("deps", Environment.Is64BitProcess ? "x64" : "x32");
            
            string ffmpegPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "deps",
                Environment.Is64BitProcess ? "x64" : "x32");

            // ffmpeg.RootPath = ffmpegPath;

            RootPath = ffmpegPath;
            // while (current != null)
            // {
            //     var ffmpegBinaryPath = Path.Combine(current, probe);
            //     if (Directory.Exists(ffmpegBinaryPath))
            //     {
            //         RootPath = ffmpegBinaryPath;
            //         Console.WriteLine(RootPath);
            //         Console.WriteLine(Environment.Is64BitProcess);
            //         uint ver = ffmpeg.avformat_version();
            //         Console.WriteLine($"[del_decoder] FFmpeg Version: {ver >> 16}.{ver >> 8 & 255}.{ver & 255}  Location: {ffmpegBinaryPath}");
            //         return;
            //     }
            //
            //     current = Directory.GetParent(current)?.FullName;
            // }

            Console.WriteLine("[del_decoder] Could not locate FFmpeg binaries folder. Make sure deps/x64 or deps/x32 exists.");
        }

        private static string ErrorCodeToMsg(int error)
        {
            byte* buffer = stackalloc byte[1024];
            ffmpeg.av_strerror(error, buffer, 1024);
            return Marshal.PtrToStringAnsi((IntPtr)buffer);
        }

        private void Log(string msg) => Console.WriteLine($"[del_decoder] {msg}");

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (vCodecCtx != null)
            {
                AVCodecContext* tmp = vCodecCtx;
                avcodec_free_context(&tmp);
                vCodecCtx = null;
            }

            // if (hw_device_ctx != null)
            // {
            //     av_buffer_unref(&hw_device_ctx);
            //     hw_device_ctx = null;
            // }

            textureHW?.Dispose();
            textureFFmpeg?.Dispose();
        }

        #endregion
    }
}
