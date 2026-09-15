using System;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OpenCvSharp;

using System.IO;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Media;
using SharpDX.Direct3D11;
using Buffer = System.Buffer;
using System.Diagnostics;
using Androidplayer_wpf.Src.Keymap.K_store;
using Androidplayer_wpf.Store;
using Androidplayer_wpf.windows;
using Androidplayer_wpf.Src.Keymap;
using SharpDX;
using SharpDX.XAudio2;
using SharpDX.Multimedia;

// using Serilog;



namespace Androidplayer_wpf.Src
{
    public class Scrcpy_worker : IDisposable
    {



        // XAudio2 fields (replace existing)
        private XAudio2 _xaudio;
        private MasteringVoice _masteringVoice;
        private SourceVoice _sourceVoice;
        private WaveFormat _waveFormat;
        private readonly object _audioLock = new object();

// Track DataStreams so they aren't GC'd while XAudio2 is using them
        private readonly Queue<DataStream> _pendingStreams = new Queue<DataStream>();
        private bool _xaudioStarted = false;



        /// ///////////////////

        private Thread scrcpy_thread;

        private Thread audio_thread;

        private ManualResetEventSlim audioReadyEvent = new ManualResetEventSlim(false);




        private Dispatcher _dispatcher;
        private Mat currentFrame;
        private bool isrunning;

        private bool recive_audio = false;

        private bool isDisposed = false;


        private string host = "127.0.0.1";

        private int port = 1234;
        // private Socket videoSocket;
        // private Socket controlSocket;

        private TcpClient? videoClient;
        private TcpClient? controlClient;

        private TcpClient? audioClient;



        private CancellationTokenSource? cts;

        private bool screenshot = false;

        public string DeviceName { get; private set; } = "";
        public int Width { get; private set; }
        public int Height { get; private set; }


        private int timeoutMs = 15000;

        private int VideoWidth = 0;
        private int VideoHeight = 0;

        private int device_width = 0;

        private int device_height = 0;


        // private my_AV_4? _decoder;
        private my_AV? _decoder;

        private my_audio? _audio_decoder;

        // private my_AV_2? _decoder;

        public Device dx_Device { get; set; }



        private static readonly ArrayPool<byte> pool = ArrayPool<byte>.Shared;

        public event Action Frame_almostready;


        public event Action<Texture2D> FrameReady;

        // public event Action<byte[]> FrameReady;
        public event Action<string> ErrorOccurred;

        public event Action scrcpy_desposed;





        // public event Action<Tuple<int, int>> ResolutionReady;
        public event Action<(int Width, int Height)> DeviceResolutionReady;
        public event Action<(int Width, int Height)> videosizeReady;
        public event Action<TcpClient> ControlSocketReady;



        private Texture2D _previousFrame = null;







        private Stopwatch _frameTimer;
        private long _lastFrameTime;
        private int _framesDropped;
        private const double TARGET_FRAME_TIME_MS = 30; // 60 FPS target


        private const double MAX_FRAME_TIME_MS = 16.67; // Max 60 FPS
        private Texture2D _pendingFrame; // Store frame if we need to delay rendering

        
        
        private sealed class ScrcpyVideoPacket
        {
            public byte[] Data { get; init; }
            public long Pts { get; init; }
            public bool IsConfig { get; init; }
            public bool IsKeyFrame { get; init; }
        }
        
        
        
        public Scrcpy_worker()
        {

            currentFrame = new Mat();
            isrunning = false;

            _audio_decoder = new my_audio();

            InitAudio();


            // Console.WriteLine("Scrcpy_worker initialized");
        }



        public void Start()
        {

            isrunning = true;



            scrcpy_thread = new Thread(Run)
            {
                IsBackground = true,
                Name = "VideoPlaybackThread"
            };

            audio_thread = new Thread(start_audio)
            {
                IsBackground = true,
                Name = "AudioPlaybackThread"
            };


            scrcpy_thread.Start();


            audio_thread.Start();
        }

        public void Stop()
        {
            isrunning = false;


            if (scrcpy_thread != null && scrcpy_thread.IsAlive)
            {
                scrcpy_thread.Join(1000);

                // scrcpy_thread.Abort();

                scrcpy_thread = null;
            }


            audioReadyEvent.Set();

            if (audio_thread != null && audio_thread.IsAlive)
            {
                audio_thread.Join(1000);

                // scrcpy_thread.Abort();

                audio_thread = null;
            }




        }



        public void SetFrameSize(int width, int height)
        {
            // VideoWidth = width;
            // VideoHeight = height;

            // _decoder.VideoWidth = width;
            // _decoder.VideoHeight = height;

            // Console.WriteLine($"resized in scrcpy_worker {_decoder.VideoWidth}, {_decoder.VideoHeight}");
        }

        public void TakeScreenshot(string word)
        {
            if (word == "screenshot")
            {
                screenshot = true;
            }
        }


        #region Audio Player



        private void InitAudio()
        {
            lock (_audioLock)
            {
                if (_xaudio != null) return;

                _xaudio = new XAudio2();
                _masteringVoice = new MasteringVoice(_xaudio);

                // Hardcoded for scrcpy: 48kHz, stereo, 16-bit PCM
                _waveFormat = new WaveFormat(48000, 16, 2);

                _sourceVoice = new SourceVoice(_xaudio, _waveFormat);
                _sourceVoice.Start();

                _pendingStreams.Clear();
                _xaudioStarted = true;

                Console.WriteLine("XAudio2 initialized for real-time playback");
            }
        }




        private const int MAX_QUEUED_BUFFERS = 4; // tune: 4 buffers ≈ how much slack you tolerate

private void SubmitPcmToXAudio(byte[] pcm)
{
    if (pcm == null || pcm.Length == 0) return;

    lock (_audioLock)
    {
        if (_sourceVoice == null || !_xaudioStarted) return;

        // ---------------------------------------------------------
        // Backlog check: if we've queued too many buffers, playback
        // is falling behind real-time. Flush everything queued and
        // resync to "now" instead of playing through a growing delay.
        // ---------------------------------------------------------
        var state = _sourceVoice.State;
        if (state.BuffersQueued >= MAX_QUEUED_BUFFERS)
        {
            Console.WriteLine($"Audio backlog: {state.BuffersQueued} buffers queued, flushing to resync");

            _sourceVoice.Stop();
            _sourceVoice.FlushSourceBuffers();
            _sourceVoice.Start();

            // FlushSourceBuffers triggers BufferEnd for everything that
            // was queued, so your existing _pendingStreams cleanup in
            // that callback should dispose them. If you instead dispose
            // streams somewhere else, drain _pendingStreams here too.
        }

        // Ensure block alignment (channels * bytesPerSample)
        int blockAlign = _waveFormat.BlockAlign;
        if (pcm.Length % blockAlign != 0)
        {
            int paddedLen = ((pcm.Length + blockAlign - 1) / blockAlign) * blockAlign;
            Array.Resize(ref pcm, paddedLen); // pads with zeros
        }

        // Create DataStream and keep it alive until XAudio finishes it
        var ds = new DataStream(pcm.Length, true, true);
        ds.Write(pcm, 0, pcm.Length);
        ds.Position = 0;

        var buffer = new AudioBuffer
        {
            Stream = ds,
            AudioBytes = pcm.Length,
            Flags = BufferFlags.None // DO NOT set EndOfStream for normal buffers
        };

        try
        {
            _sourceVoice.SubmitSourceBuffer(buffer, null);
            _pendingStreams.Enqueue(ds);
        }
        catch (SharpDX.SharpDXException ex)
        {
            Console.WriteLine($"XAudio2 submit error: {ex.ResultCode} / {ex.Message}");
            try { ds.Dispose(); } catch { }
        }
    }
}
        

        // private void SubmitPcmToXAudio(byte[] pcm)
        // {
        //     if (pcm == null || pcm.Length == 0) return;
        //
        //     lock (_audioLock)
        //     {
        //         if (_sourceVoice == null || !_xaudioStarted) return;
        //
        //         // Ensure block alignment (channels * bytesPerSample)
        //         int blockAlign = _waveFormat.BlockAlign;
        //         if (pcm.Length % blockAlign != 0)
        //         {
        //             int paddedLen = ((pcm.Length + blockAlign - 1) / blockAlign) * blockAlign;
        //             Array.Resize(ref pcm, paddedLen); // pads with zeros
        //         }
        //
        //         // Create DataStream and keep it alive until XAudio finishes it
        //         var ds = new DataStream(pcm.Length, true, true);
        //         ds.Write(pcm, 0, pcm.Length);
        //         ds.Position = 0;
        //
        //         var buffer = new AudioBuffer
        //         {
        //             Stream = ds,
        //             AudioBytes = pcm.Length,
        //             Flags = BufferFlags.None // DO NOT set EndOfStream for normal buffers
        //         };
        //
        //         try
        //         {
        //             _sourceVoice.SubmitSourceBuffer(buffer, null);
        //             // Track stream for later disposal
        //             _pendingStreams.Enqueue(ds);
        //         }
        //         catch (SharpDX.SharpDXException ex)
        //         {
        //             Console.WriteLine($"XAudio2 submit error: {ex.ResultCode} / {ex.Message}");
        //             // If it fails, release the DataStream immediately
        //             try
        //             {
        //                 ds.Dispose();
        //             }
        //             catch
        //             {
        //             }
        //         }
        //     }
        // }

        private void PollXAudioAndCleanup()
        {
            lock (_audioLock)
            {
                if (_sourceVoice == null) return;

                var state = _sourceVoice.State;
                int buffersQueued = (int)state.BuffersQueued;

                // Dispose DataStreams that correspond to finished buffers
                while (_pendingStreams.Count > buffersQueued)
                {
                    var ds = _pendingStreams.Dequeue();
                    try
                    {
                        ds.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
        }



        
        private void start_audio()
{
    const long PACKET_FLAG_CONFIG    = 1L << 62;
    const long PACKET_FLAG_KEY_FRAME = 1L << 61;

    while (isrunning)
    {
        audioReadyEvent.Wait();

        // ---------------------------------------------------------
        // Connect if not already connected
        // ---------------------------------------------------------
        if (audioClient == null)
        {
            audioClient = new TcpClient();

            if (!audioClient.ConnectAsync(host, 1012).Wait(1000))
            {
                ErrorOccurred?.Invoke("Control connection timeout");
                audioClient = null;
                continue;
            }

            Console.WriteLine("audio socket connected");
            continue;
        }

        try
        {
            Console.WriteLine("started reciving audio....");

            NetworkStream audioStream = audioClient.GetStream();
            audioStream.ReadTimeout = Timeout.Infinite;

            // ---------------------------------------------------------
            // Codec header — sent ONCE per connection (4 bytes BE fourcc)
            // ---------------------------------------------------------
            byte[] codecBuffer = new byte[4];
            if (!ReadExact(audioStream, codecBuffer, 4))
                throw new IOException("codec header read failed");

            uint codecId = BinaryPrimitives.ReadUInt32BigEndian(codecBuffer);
            Console.WriteLine($"Audio codec ID: {codecId}");

            byte[] frameMeta = new byte[12];

            // ---------------------------------------------------------
            // Frame loop:
            //   [8 bytes BE pts+flags][4 bytes BE size][N bytes payload]
            // ---------------------------------------------------------
            while (isrunning && audioClient.Connected)
            {
                if (!ReadExact(audioStream, frameMeta, 12))
                {
                    Console.WriteLine("audio stream closed (frame meta)");
                    break;
                }

                long ptsAndFlags =
                    BinaryPrimitives.ReadInt64BigEndian(
                        frameMeta.AsSpan(0, 8));

                int packetSize =
                    (int)BinaryPrimitives.ReadUInt32BigEndian(
                        frameMeta.AsSpan(8, 4));

                bool isConfig =
                    (ptsAndFlags & PACKET_FLAG_CONFIG) != 0;

                if (packetSize <= 0 || packetSize > 1_000_000)
                {
                    Console.WriteLine($"Invalid audio packet size: {packetSize}");
                    break;
                }

                byte[] payload = new byte[packetSize];
                if (!ReadExact(audioStream, payload, packetSize))
                {
                    Console.WriteLine("audio stream closed (payload)");
                    break;
                }

                // -----------------------------------------------------
                // Config packet (Opus extradata / OpusHead).
                // Do NOT feed it as a normal frame.
                // -----------------------------------------------------
                if (isConfig)
                {
                    Console.WriteLine(
                        $"Audio config packet: {payload.Length} bytes");
                    _audio_decoder?.SetExtradata(payload);
                    continue;
                }

                byte[]? pcm = _audio_decoder?.Decode(payload);
                if (pcm != null && pcm.Length > 0)
                    SubmitPcmToXAudio(pcm);
            }
        }
        catch (IOException ioEx) when (ioEx.InnerException is SocketException sockEx)
        {
            Console.WriteLine($"Audio socket error: {sockEx.SocketErrorCode}");
            ErrorOccurred?.Invoke(sockEx.Message);
            audioReadyEvent.Reset();
            PollXAudioAndCleanup();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Audio receive error: {ex.Message}");
            ErrorOccurred?.Invoke(ex.Message);
            audioReadyEvent.Reset();
            PollXAudioAndCleanup();
        }
        finally
        {
            try { audioClient?.Close(); } catch { }
            audioClient = null;
        }

        Thread.Sleep(16);
    }
}

private static bool ReadExact(NetworkStream stream, byte[] buffer, int count)
{
    int offset = 0;
    while (offset < count)
    {
        int read;
        try { read = stream.Read(buffer, offset, count - offset); }
        catch { return false; }

        if (read <= 0) return false;
        offset += read;
    }
    return true;
}


        #endregion





        private void Run()
        {

            while (isrunning)
            {

                try
                {

                    Thread.Sleep(2000);
                    videoClient = new TcpClient();
                    if (!videoClient.ConnectAsync(host, 1011).Wait(timeoutMs))
                    {

                        ErrorOccurred?.Invoke("Connection timeout");

                    }

                    videoClient.NoDelay = true;


                    var infoStream = videoClient.GetStream();
                    infoStream.ReadTimeout = 2000;

                    // FIRST: Read the dummy byte (like Python does)
                    byte[] dummyByte = new byte[1];
                    int dummyRead = infoStream.Read(dummyByte, 0, 1);
                    Console.WriteLine($"Dummy byte read: {dummyRead} bytes, value: {dummyByte[0]}");

                    if (dummyRead != 1)
                    {

                        ErrorOccurred?.Invoke($"Expected to read dummy byte (1 byte), but got {dummyRead} bytes.");
                        // throw new Exception($"Expected to read dummy byte (1 byte), but got {dummyRead} bytes.");

                    }

                    Thread.Sleep(500);

                    if (UISettings.Instance.AudioEnabled)
                    {

                        audioReadyEvent.Set();

                    }



                    Thread.Sleep(1000);

                    controlClient = new TcpClient();
                    if (!controlClient.ConnectAsync(host, 1013).Wait(timeoutMs))
                    {
                        // throw new TimeoutException("Control connection timeout");
                        ErrorOccurred?.Invoke("Control connection timeout");

                    }






                    ControlSocketReady?.Invoke(controlClient);

                    ReadDeviceInfo();

                    // audioReadyEvent.Set();

                    ReceiveVideoData();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    ErrorOccurred?.Invoke(e.Message);

                }



            }


        }



        private void ReadDeviceInfo()
        {
            if (videoClient == null) throw new InvalidOperationException("Not connected");

            var infoStream = videoClient.GetStream();
            infoStream.ReadTimeout = 2000;

            // Read 68-byte header.
            var deviceInfoBuf = pool.Rent(64);
            int bytesRead = infoStream.Read(deviceInfoBuf, 0, 64);

            Console.WriteLine("recived device info");

            Console.WriteLine($"Received device info: {bytesRead} bytes");


            // Console.WriteLine(bytesRead.length);
            if (bytesRead != 64)
            {
                // throw new Exception($"Expected to read exactly 64 bytes for device name, but got {bytesRead} bytes.");
                ErrorOccurred?.Invoke($"Expected to read exactly 64 bytes for device name, but got {bytesRead} bytes.");
                return;
            }

            // THIRD: Read 4-byte resolution


            // Decode device name
            DeviceName = Encoding.UTF8.GetString(deviceInfoBuf, 0, 64).TrimEnd('\0');
            Console.WriteLine("Device name: " + DeviceName);




            byte[] codecMeta = new byte[12];
            int codecBytesRead = infoStream.Read(codecMeta, 0, 12);
            if (codecBytesRead != 12)
            {
                // throw new Exception($"Expected 12 bytes for codec metadata, got {codecBytesRead}");
                ErrorOccurred?.Invoke($"Expected 12 bytes for codec metadata, got {codecBytesRead}");

                return;
            }

            // Parse codec metadata (all big-endian u32)
            uint codecId = BinaryPrimitives.ReadUInt32BigEndian(codecMeta.AsSpan(0, 4));
            Width = (int)BinaryPrimitives.ReadUInt32BigEndian(codecMeta.AsSpan(4, 4));
            Height = (int)BinaryPrimitives.ReadUInt32BigEndian(codecMeta.AsSpan(8, 4));

            Console.WriteLine($"Codec ID: {codecId}, Resolution: {Width}x{Height}");



            device_width = Width;

            device_height = Height;

            DeviceResolutionReady?.Invoke((Width, Height));



        }

        private short ReadInt16BigEndian(byte[] buffer, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                return (short)((buffer[offset] << 8) | buffer[offset + 1]);
            }

            return BitConverter.ToInt16(buffer, offset);
        }




        private void RenderTexture(Texture2D frame)
        {
            // Frame timing variables


            // Initialize timer if not already done
            if (_frameTimer == null)
            {
                _frameTimer = new Stopwatch();
                _frameTimer.Start();
            }

            if (frame == null) return;

            // Store the frame (dispose previous pending frame)
            _pendingFrame?.Dispose();
            _pendingFrame = frame;

            long currentTime = _frameTimer.ElapsedMilliseconds;
            long timeSinceLastFrame = currentTime - _lastFrameTime;

            // Only render if enough time has passed OR this is the first frame
            if (_lastFrameTime == 0 || timeSinceLastFrame >= MAX_FRAME_TIME_MS)
            {
                if (VideoHeight != _decoder.Height || VideoWidth != _decoder.Width)
                {
                    VideoHeight = _decoder.Height;
                    VideoWidth = _decoder.Width;



                    if (VideoWidth > VideoHeight)
                    {
                        // landscape mode
                        if (device_height > device_width)
                        {
                            DeviceResolutionReady?.Invoke((device_height, device_width));
                        }
                        else if (device_width > device_height)
                        {
                            DeviceResolutionReady?.Invoke((device_width, device_height));
                        }
                    }
                    else
                    {
                        // portrait mode 
                        if (device_height > device_width)
                        {
                            DeviceResolutionReady?.Invoke((device_width, device_height));
                        }
                        else if (device_width > device_height)
                        {
                            DeviceResolutionReady?.Invoke((device_height, device_width));
                        }
                    }


                    Console.WriteLine($"from scrcpy worker : {VideoWidth} , {VideoHeight}");
                    videosizeReady?.Invoke((VideoWidth, VideoHeight));
                }

                // FrameReady?.Invoke(_pendingFrame);
                k_info.Instance.directx?.PresentFrame(_pendingFrame);
                _lastFrameTime = currentTime;
                _pendingFrame = null; // Frame is now rendered, clear reference

                // Log frame rate occasionally
                if (_decoder.FrameCount % 100 == 0)
                {
                    Console.WriteLine($"Frames: {_decoder.FrameCount}");
                }
            }
            else
            {
                // Frame is stored in _pendingFrame and will be rendered when time allows
                _framesDropped++;
            }
        }






        // Frame skipping method
        private bool ShouldSkipFrame()
        {
            if (_lastFrameTime == 0) return false; // Always process first frame

            long currentTime = _frameTimer.ElapsedMilliseconds;
            long timeSinceLastFrame = currentTime - _lastFrameTime;

            // Skip frame if less than target frame time since last frame
            return timeSinceLastFrame < TARGET_FRAME_TIME_MS;
        }













        private void ReceiveVideoData()
        {
            if (videoClient == null || !videoClient.Connected)
                return;

            NetworkStream stream = videoClient.GetStream();

            // CRITICAL: Remove or drastically increase timeout for scrcpy
            stream.ReadTimeout = Timeout.Infinite; // No timeout - wait indefinitely

            // Buffer for receiving data
            // byte[] buffer = new byte[0x10000]; // 64KB buffer
            byte[] buffer = new byte[0x100000];

            Console.WriteLine("Starting to receive H264 video data...");
            // OnStatusUpdate?.Invoke("Started receiving video data");

            // _decoder = new my_AV_3(dx_Device);


            // audioReadyEvent.Set();


            // _decoder = new my_AV_4(dx_Device);
            
            
            _decoder = new my_AV(dx_Device);

            Frame_almostready.Invoke();

            // Console.WriteLine("started waiting for native window");
            // Thread.Sleep(5000);

            
            // _decoder.OnFrameDecoded += (texture) => 
            // {
            //     // Use the texture for rendering
            //     RenderTexture(texture);
            // };


            int num = 8;

            string mm = null;

            Console.WriteLine("im reciving data");
            Console.WriteLine(isrunning );
            Console.WriteLine(videoClient.Connected);
            while (isrunning && videoClient.Connected)
            {
                try
                {



                    // Read raw H264 data from the stream
                    // int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    // Console.WriteLine(bytesRead);
                    
                    ScrcpyVideoPacket packet =
                        ReadScrcpyVideoPacket(stream);

                    if (packet == null ||
                        packet.Data == null ||
                        packet.Data.Length == 0)
                    {
                        continue;
                    }


                    

                    


                    // Console.WriteLine(
                    //     $"H264 packet: {packet.Data.Length} bytes, " +
                    //     $"Config={packet.IsConfig}, " +
                    //     $"Key={packet.IsKeyFrame}, " +
                    //     $"PTS={packet.Pts}");
                    
                    
                        // Create a properly sized array for the received data
                        // byte[] receivedData = new byte[bytesRead];
                        // Buffer.BlockCopy(buffer, 0, receivedData, 0, bytesRead);
                        //

                      
                        // Console.WriteLine(receivedData.Length);

                        Stopwatch sw = new Stopwatch();


                        try
                        {


                            sw.Restart();
                            
                            // Console.WriteLine(
                            //     $"H264 packet: {receivedData.Length} bytes, " + $"first bytes: {BitConverter.ToString(receivedData, 0,Math.Min(16, receivedData.Length))}");
                            //
                            // Texture2D frame  =  _decoder.Decode(receivedData);
                            
                            Texture2D frame =
                                _decoder.DecodePacket(
                                    packet.Data,
                                    packet.Pts,
                                    packet.IsConfig);
                            
                            
                            
                            
                            if (frame == null)
                            {
                                continue;
                            }
                            
                            
                            if (My_Store.Instance.DisplayHeight == 0 || My_Store.Instance.DisplayHeight != (int)k_info.Instance.ImageContainer.ActualWidth)
                            {
                                // My_Store.Instance.SetDisplayResolution((int)my_image.ActualWidth, (int)my_image.ActualHeight);
                                My_Store.Instance.SetDisplayResolution((int)k_info.Instance.ImageContainer.ActualWidth, (int)k_info.Instance.ImageContainer.ActualHeight);
                            }
                            
                            
                            
                            
                            if (My_Store.Instance.VideoHeight == 0 || My_Store.Instance.VideoWidth == 0)
                            {
                                My_Store.Instance.SetVideoResolution(frame.Description.Width, frame.Description.Height);
                            }
                            
                            if (My_Store.Instance?.DeviceHeight == 0 ||
                                My_Store.Instance?.DeviceWidth == 0 && my_info.Instance.DeveloperMode)
                            {
                                My_Store.Instance.SetDeviceResolution(frame.Description.Width, frame.Description.Height);
                            }
                            
                            
                            
                            
                            
                            
                            
                            if (_previousFrame != null && !_previousFrame.IsDisposed)
                            {
                                _previousFrame.Dispose();
                            }
                            
                            _previousFrame = frame;
                            
                            RenderTexture(frame);
                            
                            
                            

                            sw.Stop();
    
                            // Print total time (decode + render)
                            
                            
                            // double totalMs = sw.Elapsed.TotalMilliseconds;
                            // Console.WriteLine($"Frame time (decode + render): {totalMs:F2} ms");
                            //





                            continue;



                        }
                        catch (Exception decodeEx)
                        {
                            // Handle decoder errors gracefully without breaking the connection
                            Console.WriteLine($"Decoder error (non-fatal): {decodeEx.Message}");
                            continue; // Continue to next frame
                        }




                    

                }
                catch (IOException ex) when (ex.InnerException is SocketException sockEx)
                {
                    // Handle socket-specific exceptions
                    switch (sockEx.SocketErrorCode)
                    {

                        case SocketError.ConnectionReset:
                        case SocketError.ConnectionAborted:
                            Console.WriteLine($"Connection reset by peer: {sockEx.SocketErrorCode}");
                            ErrorOccurred?.Invoke(sockEx.Message);
                            break;

                        default:

                            continue;
                    }

                    break;
                }

            }

            Console.WriteLine("Video data receiving loop ended");
            ErrorOccurred?.Invoke("Video data receiving loop ended");

            // OnStatusUpdate?.Invoke("Video stream disconnected");
        }





        private static void ReadExactly(Stream stream, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int read = stream.Read(buffer, offset, count);

                if (read <= 0)
                    throw new IOException("Connection closed while reading video packet.");

                offset += read;
                count -= read;
            }
        }
        
        
        
        protected virtual void OnErrorOccurred(string errorMessage)
        {
            Console.WriteLine($"Error occurred: {errorMessage}");
            _dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new Action(() => { ErrorOccurred?.Invoke(errorMessage); }));
        }
        
        
        
        private ScrcpyVideoPacket ReadScrcpyVideoPacket(Stream stream)
        {
            byte[] header = new byte[12];

            ReadExactly(stream, header, 0, 12);

            ulong ptsFlags =
                ((ulong)header[0] << 56) |
                ((ulong)header[1] << 48) |
                ((ulong)header[2] << 40) |
                ((ulong)header[3] << 32) |
                ((ulong)header[4] << 24) |
                ((ulong)header[5] << 16) |
                ((ulong)header[6] << 8) |
                header[7];

            int size =
                (header[8] << 24) |
                (header[9] << 16) |
                (header[10] << 8) |
                header[11];

            if (size <= 0 || size > 10 * 1024 * 1024)
                throw new InvalidDataException(
                    $"Invalid scrcpy video packet size: {size}");

            byte[] data = new byte[size];

            ReadExactly(stream, data, 0, size);

            bool isConfig =
                (ptsFlags & (1UL << 63)) != 0;

            bool isKeyFrame =
                (ptsFlags & (1UL << 62)) != 0;

            long pts =
                (long)(ptsFlags & ((1UL << 62) - 1));

            return new ScrcpyVideoPacket
            {
                Data = data,
                Pts = pts,
                IsConfig = isConfig,
                IsKeyFrame = isKeyFrame
            };
        }

        public void Dispose()
        {
            if (isDisposed) return;

            isDisposed = true;
            Stop();


            _frameTimer?.Stop();


            currentFrame?.Dispose();


            _audio_decoder.Dispose();


            lock (_audioLock)
            {
                try
                {
                    if (_sourceVoice != null)
                    {
                        _sourceVoice.Stop();
                        _sourceVoice.FlushSourceBuffers();
                    }

                    // Dispose pending streams
                    while (_pendingStreams.Count > 0)
                    {
                        try
                        {
                            _pendingStreams.Dequeue().Dispose();
                        }
                        catch
                        {
                        }
                    }

                    if (_sourceVoice != null)
                    {
                        _sourceVoice.DestroyVoice();
                        _sourceVoice = null;
                    }

                    if (_masteringVoice != null)
                    {
                        _masteringVoice.Dispose();
                        _masteringVoice = null;
                    }

                    if (_xaudio != null)
                    {
                        _xaudio.Dispose();
                        _xaudio = null;
                    }

                    _xaudioStarted = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing audio: {ex.Message}");

                }


            }

        }

    }

}

