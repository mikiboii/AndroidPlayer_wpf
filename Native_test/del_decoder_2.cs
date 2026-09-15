// // del_decoder_2_fixed.cs
// using System;
// using System.IO;
// using System.Runtime.InteropServices;
// using FFmpeg.AutoGen;
// using SharpDX;
// using SharpDX.Direct3D11;
// using SharpDX.DXGI;
// using Device = SharpDX.Direct3D11.Device;
//
// namespace Android_player_2.Native_test
// {
//     public unsafe class del_decoder_2 : IDisposable
//     {
//         private bool _disposed = false;
//
//         // FFmpeg components
//         private AVCodec* _codec;
//         private AVCodecContext* _codec_ctx;
//         private AVCodecParserContext* _parser;
//
//         // DirectX NV12 texture
//         private Device _device;
//         private Texture2D _nv12Texture;
//         private ShaderResourceView _nv12SRV;
//         private int _texWidth = 0;
//         private int _texHeight = 0;
//
//         // Frame counters
//         public int Width { get; private set; }
//         public int Height { get; private set; }
//         public long FrameCount { get; private set; }
//
//         public del_decoder_2(Device device)
//         {
//             _device = device ?? throw new ArgumentNullException(nameof(device));
//
//             ffmpeg.RootPath = Environment.Is64BitProcess ? @"c:\deps\x64" : @"c:\deps\x32";
//
//             _codec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
//             if (_codec == null) throw new Exception("Failed to find H.264 decoder");
//
//             _codec_ctx = ffmpeg.avcodec_alloc_context3(_codec);
//             if (_codec_ctx == null) throw new Exception("Failed to allocate codec context");
//
//             if (ffmpeg.avcodec_open2(_codec_ctx, _codec, null) < 0)
//                 throw new Exception("Failed to open codec");
//
//             _parser = ffmpeg.av_parser_init((int)AVCodecID.AV_CODEC_ID_H264);
//             if (_parser == null) throw new Exception("Failed to initialize parser");
//
//             Console.WriteLine("[Decoder] Initialized successfully");
//         }
//
//         /// <summary>
//         /// Feed raw H.264 bytes directly to the decoder
//         /// </summary>
//         public Texture2D Decode(byte[] h264Data)
//         {
//             if (_disposed || h264Data == null || h264Data.Length == 0)
//                 return null;
//
//             fixed (byte* pData = h264Data)
//             {
//                 byte* ptr = pData;
//                 int size = h264Data.Length;
//                 Texture2D lastTexture = null;
//
//                 while (size > 0)
//                 {
//                     byte* outData = null;
//                     int outSize = 0;
//
//                     int consumed = ffmpeg.av_parser_parse2(
//                         _parser,
//                         _codec_ctx,
//                         &outData,
//                         &outSize,
//                         ptr,
//                         size,
//                         ffmpeg.AV_NOPTS_VALUE,
//                         ffmpeg.AV_NOPTS_VALUE,
//                         0
//                     );
//
//                     if (consumed < 0)
//                     {
//                         Console.WriteLine("[Decoder] Parser error");
//                         break;
//                     }
//
//                     if (outSize > 0)
//                     {
//                         Texture2D tex = ProcessPacket(outData, outSize);
//                         if (tex != null)
//                         {
//                             lastTexture = tex;
//                             FrameCount++;
//                             Width = _codec_ctx->width;
//                             Height = _codec_ctx->height;
//                             // Console.WriteLine($"[Decoder] Decoded frame {FrameCount}: {Width}x{Height}");
//                         }
//                     }
//
//                     ptr += consumed;
//                     size -= consumed;
//                 }
//
//                 return lastTexture;
//             }
//         }
//
//         private Texture2D ProcessPacket(byte* outData, int outSize)
//         {
//             AVPacket* packet = ffmpeg.av_packet_alloc();
//             if (packet == null) return null;
//
//             try
//             {
//                 packet->data = (byte*)ffmpeg.av_malloc((ulong)outSize);
//                 System.Buffer.MemoryCopy(outData, packet->data, outSize, outSize);
//                 packet->size = outSize;
//
//                 int ret = ffmpeg.avcodec_send_packet(_codec_ctx, packet);
//                 if (ret < 0)
//                 {
//                     Console.WriteLine($"[Decoder] Error sending packet: {ret}");
//                     return null;
//                 }
//
//                 AVFrame* frame = ffmpeg.av_frame_alloc();
//                 if (frame == null) return null;
//
//                 try
//                 {
//                     ret = ffmpeg.avcodec_receive_frame(_codec_ctx, frame);
//                     if (ret == 0)
//                         return UploadFrameToNV12Texture(frame);
//                     else if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN))
//                         return null; // need more data
//                     else
//                         Console.WriteLine($"[Decoder] Error receiving frame: {ret}");
//                 }
//                 finally
//                 {
//                     ffmpeg.av_frame_free(&frame);
//                 }
//             }
//             finally
//             {
//                 if (packet->data != null)
//                     ffmpeg.av_free(packet->data);
//                 ffmpeg.av_packet_free(&packet);
//             }
//
//             return null;
//         }
//
//         private Texture2D UploadFrameToNV12Texture(AVFrame* frame)
//         {
//             if (frame == null) return null;
//
//             const AVPixelFormat wantedNV12 = AVPixelFormat.AV_PIX_FMT_NV12;
//             AVFrame* nv12Frame = frame;
//
//             // Convert to NV12 if needed
//             if ((AVPixelFormat)frame->format != wantedNV12)
//             {
//                 nv12Frame = ffmpeg.av_frame_alloc();
//                 nv12Frame->format = (int)wantedNV12;
//                 nv12Frame->width = frame->width;
//                 nv12Frame->height = frame->height;
//                 if (ffmpeg.av_frame_get_buffer(nv12Frame, 0) < 0)
//                 {
//                     ffmpeg.av_frame_free(&nv12Frame);
//                     return null;
//                 }
//
//                 SwsContext* sc = ffmpeg.sws_getContext(
//                     frame->width, frame->height, (AVPixelFormat)frame->format,
//                     frame->width, frame->height, wantedNV12,
//                     ffmpeg.SWS_BILINEAR, null, null, null
//                 );
//
//                 ffmpeg.sws_scale(sc, frame->data, frame->linesize, 0, frame->height, nv12Frame->data, nv12Frame->linesize);
//                 ffmpeg.sws_freeContext(sc);
//             }
//
//             EnsureNv12Texture(nv12Frame->width, nv12Frame->height);
//
//             var context = _device.ImmediateContext;
//             context.UpdateSubresource(new SharpDX.DataBox((IntPtr)nv12Frame->data[0], nv12Frame->linesize[0], 0), _nv12Texture, 0);
//             context.UpdateSubresource(new SharpDX.DataBox((IntPtr)nv12Frame->data[1], nv12Frame->linesize[1], 0), _nv12Texture, 1);
//
//             if ((AVPixelFormat)frame->format != wantedNV12)
//             {
//                 ffmpeg.av_frame_unref(nv12Frame);
//                 ffmpeg.av_frame_free(&nv12Frame);
//             }
//
//             return _nv12Texture;
//         }
//
//         private void EnsureNv12Texture(int width, int height)
//         {
//             if (_nv12Texture != null && _texWidth == width && _texHeight == height)
//                 return;
//
//             Utilities.Dispose(ref _nv12SRV);
//             Utilities.Dispose(ref _nv12Texture);
//
//             var desc = new Texture2DDescription
//             {
//                 Width = width,
//                 Height = height,
//                 ArraySize = 1,
//                 MipLevels = 1,
//                 Format = Format.NV12,
//                 SampleDescription = new SampleDescription(1, 0),
//                 Usage = ResourceUsage.Default,
//                 BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
//                 CpuAccessFlags = CpuAccessFlags.None,
//                 OptionFlags = ResourceOptionFlags.None
//             };
//
//             _nv12Texture = new Texture2D(_device, desc);
//             _texWidth = width;
//             _texHeight = height;
//         }
//
//         public void Dispose()
//         {
//             if (!_disposed)
//             {
//                 Utilities.Dispose(ref _nv12SRV);
//                 Utilities.Dispose(ref _nv12Texture);
//
//                 if (_parser != null)
//                 {
//                     ffmpeg.av_parser_close(_parser);
//                     _parser = null;
//                 }
//
//                 if (_codec_ctx != null)
//                 {
//                     ffmpeg.avcodec_close(_codec_ctx);
//                     AVCodecContext* ctx = _codec_ctx;
//                     ffmpeg.avcodec_free_context(&ctx);
//                     _codec_ctx = null;
//                 }
//
//                 _disposed = true;
//                 Console.WriteLine("[Decoder] Disposed");
//             }
//         }
//     }
// }


using System.IO;
using FFmpeg.AutoGen;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using static FFmpeg.AutoGen.ffmpeg;

namespace Androidplayer_wpf.Native_test
{
    public unsafe class del_decoder_2 : IDisposable
    {
        private AVCodec* codec;
        private AVCodecContext* codecCtx;
        private AVBufferRef* hwDeviceCtx = null;
        private AVCodecParserContext* parser;

        private Device device;

        private Texture2D hwTexture = null;      // Device-side HW texture
        private Texture2D ffmpegTexture = null;  // Temporary FFmpeg wrapper

        private bool hwInitialized = false;
        private bool useSoftwareFallback = false;

        public long FrameCount { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public del_decoder_2(Device device)
        {
            this.device = device ?? throw new ArgumentNullException(nameof(device));
            // ffmpeg.RootPath = Environment.Is64BitProcess ? @"c:\deps\x64" : @"c:\deps\x32";
            
            string ffmpegPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "deps",
                Environment.Is64BitProcess ? "x64" : "x32");

            ffmpeg.RootPath = ffmpegPath;

            codec = avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
            if (codec == null) throw new Exception("H264 codec not found");

            codecCtx = avcodec_alloc_context3(codec);
            if (codecCtx == null) throw new Exception("Failed to allocate codec context");
            
            
            codecCtx->flags |= AV_CODEC_FLAG_LOW_DELAY;    // Critical for low latency
            codecCtx->flags2 |= AV_CODEC_FLAG2_FAST;       // Enable fast decoding
            codecCtx->skip_frame = AVDiscard.AVDISCARD_DEFAULT;
            codecCtx->skip_loop_filter = AVDiscard.AVDISCARD_DEFAULT;

            // Reduce reference frames
            codecCtx->refs = 1;
            
            // Try D3D11VA HW acceleration
            try
            {
                hwDeviceCtx = av_hwdevice_ctx_alloc(AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA);
                AVHWDeviceContext* devCtx = (AVHWDeviceContext*)hwDeviceCtx->data;
                AVD3D11VADeviceContext* d3d11 = (AVD3D11VADeviceContext*)devCtx->hwctx;
                d3d11->device = (ID3D11Device*)device.NativePointer;

                int ret = av_hwdevice_ctx_init(hwDeviceCtx);
                if (ret < 0) { useSoftwareFallback = true; hwDeviceCtx = null; }
                else { hwInitialized = true; }
            }
            catch { useSoftwareFallback = true; hwDeviceCtx = null; }

            if (hwInitialized && hwDeviceCtx != null)
                codecCtx->hw_device_ctx = av_buffer_ref(hwDeviceCtx);

            if (avcodec_open2(codecCtx, codec, null) < 0)
                throw new Exception("Failed to open codec");

            parser = av_parser_init((int)AVCodecID.AV_CODEC_ID_H264);
            if (parser == null)
                throw new Exception("Failed to initialize H264 parser");
        }

        public Texture2D Decode(byte[] h264Data)
        {
            if (h264Data == null || h264Data.Length == 0) return null;

            fixed (byte* pData = h264Data)
            {
                byte* ptr = pData;
                int size = h264Data.Length;
                Texture2D lastTexture = null;

                while (size > 0)
                {
                    byte* outData = null;
                    int outSize = 0;

                    int consumed = av_parser_parse2(
                        parser,
                        codecCtx,
                        &outData,
                        &outSize,
                        ptr,
                        size,
                        AV_NOPTS_VALUE,
                        AV_NOPTS_VALUE,
                        0
                    );

                    if (consumed < 0)
                        break;

                    if (outSize > 0)
                    {
                        Texture2D tex = DecodeParsedPacket(outData, outSize);
                        if (tex != null) lastTexture = tex;
                    }

                    ptr += consumed;
                    size -= consumed;
                }

                return lastTexture;
            }
        }

        private Texture2D DecodeParsedPacket(byte* data, int size)
        {
            AVPacket* packet = av_packet_alloc();
            av_init_packet(packet);
            packet->data = data;
            packet->size = size;

            int ret = avcodec_send_packet(codecCtx, packet);
            if (ret < 0) { av_packet_unref(packet); return null; }

            AVFrame* frame = av_frame_alloc();
            ret = avcodec_receive_frame(codecCtx, frame);
            if (ret == AVERROR_EOF || ret == AVERROR(EAGAIN)) { av_packet_unref(packet); av_frame_free(&frame); return null; }
            if (ret < 0) { av_packet_unref(packet); av_frame_free(&frame); return null; }

            Texture2D result = null;

            try
            {
                if (hwInitialized && frame->data[0] != null)
                {
                    // HW decoded frame
                    IntPtr ptr = (IntPtr)frame->data[0];
                    ffmpegTexture = new Texture2D(ptr);

                    if (hwTexture == null || hwTexture.Description.Width != ffmpegTexture.Description.Width ||
                        hwTexture.Description.Height != ffmpegTexture.Description.Height)
                    {
                        hwTexture?.Dispose();
                        hwTexture = new Texture2D(device, new Texture2DDescription
                        {
                            Width = ffmpegTexture.Description.Width,
                            Height = ffmpegTexture.Description.Height,
                            MipLevels = 1,
                            ArraySize = 1,
                            Format = ffmpegTexture.Description.Format,
                            SampleDescription = new SampleDescription(1, 0),
                            Usage = ResourceUsage.Default,
                            BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                            CpuAccessFlags = CpuAccessFlags.None,
                            OptionFlags = ResourceOptionFlags.None
                        });
                    }

                    int arrayIndex = (int)frame->data[1];
                    device.ImmediateContext.CopySubresourceRegion(
                        ffmpegTexture,
                        arrayIndex,
                        new ResourceRegion(0, 0, 0, hwTexture.Description.Width, hwTexture.Description.Height, 1),
                        hwTexture,
                        0);

                    result = new Texture2D(device, hwTexture.Description);
                    device.ImmediateContext.CopyResource(hwTexture, result);

                    ffmpegTexture.Dispose();
                    ffmpegTexture = null;
                }
                else
                {
                    // Software fallback
                    result = SoftwareFrameToTexture(frame);
                }

                FrameCount++;
                Width = frame->width;
                Height = frame->height;
            }
            finally
            {
                av_frame_free(&frame);
                av_packet_unref(packet);
            }

            return result;
        }

        private Texture2D SoftwareFrameToTexture(AVFrame* frame)
        {
            if (frame == null) return null;

            SwsContext* sws = sws_getContext(frame->width, frame->height, (AVPixelFormat)frame->format,
                                             frame->width, frame->height, AVPixelFormat.AV_PIX_FMT_RGBA,
                                             SWS_BILINEAR, null, null, null);
            if (sws == null) return null;

            int stride = frame->width * 4;
            byte[] buffer = new byte[stride * frame->height];

            fixed (byte* pBuffer = buffer)
            {
                byte_ptrArray4 dstData = new byte_ptrArray4();
                int_array4 dstLine = new int_array4();
                dstData[0] = pBuffer;
                dstLine[0] = stride;

                sws_scale(sws, frame->data, frame->linesize, 0, frame->height, dstData, dstLine);

                var desc = new Texture2DDescription
                {
                    Width = frame->width,
                    Height = frame->height,
                    ArraySize = 1,
                    MipLevels = 1,
                    Format = Format.R8G8B8A8_UNorm,
                    BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                    Usage = ResourceUsage.Default,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription = new SampleDescription(1, 0)
                };

                var tex = new Texture2D(device, desc);
                device.ImmediateContext.UpdateSubresource(new DataBox((IntPtr)pBuffer, stride, 0), tex, 0);

                sws_freeContext(sws);
                return tex;
            }
        }

        public void Dispose()
        {
            hwTexture?.Dispose();
            ffmpegTexture?.Dispose();

            if (parser != null)
            {
                av_parser_close(parser);
                parser = null;
            }

            if (codecCtx != null)
            {
                AVCodecContext* tmp = codecCtx;
                avcodec_free_context(&tmp);
                codecCtx = null;
            }

            if (hwDeviceCtx != null)
            {
                // av_buffer_unref(&hwDeviceCtx);
                hwDeviceCtx = null;
            }
        }
    }
}
