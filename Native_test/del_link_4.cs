using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Androidplayer_wpf.Src;
using SharpAdbClient;
using SharpDX.Direct3D11;
// using SharpAdbClient;

// using AdvancedSharpAdbClient;
// using AdvancedSharpAdbClient.Models;
// using AdvancedSharpAdbClient.Receivers;
using Buffer = System.Buffer;



namespace Androidplayer_wpf.Native_test
{
    
    // class MyReceiver : IShellOutputReceiver
    // {
    //     public bool ParsesErrors => true;
    //
    //     public void AddOutput(string line)
    //     {
    //         Console.WriteLine("[ADB] " + line);
    //     }
    //
    //     public void Flush() { }
    // }
    //
    public class del_link_4
    {
         private readonly AdbClient adbClient;
         private DeviceData device;
         private CancellationTokenSource cts;       
                
         private TcpClient? videoClient;
         private TcpClient? controlClient;
         private TcpListener? listener;
         
        private Thread _workerThread;
        private Thread _adbThread;
        DirectX directX;

        private Directx_2 directX_2;
        
        private del_decoder my_decoder;
        
        private del_decoder_2 my_decoder2;
        
        private bool _running = false;

        private string _host = "127.0.0.1";
        private int _port = 8080;
        
        private static readonly ArrayPool<byte> pool = ArrayPool<byte>.Shared;
        
        // public string JAR = "scrcpy-server-3_old.jar";
        public string JAR = "anlink.jar";
        
        // public string JAR = "scrcpy-server-3.jar";
        
        public string VERSION = "1.20";
        public int max_size = 1080;
        // public int bitrate = 8000000;
        public int bitrate = 8000000;
        
            // 8000000
        // public int bitrate = 5000000;
        // public int bitrate = 20000;
        
        
        public int max_fps = 120;
        public bool block_frame = true;
        public bool stay_awake = false;
        public int lock_screen_orientation = -1;
        public bool skip_same_frame = false;
        public double min_frame_interval => 1.0 / max_fps;

        public del_link_4(IntPtr hwnd)
        {
            
            try
            {
                AdbServer server = new AdbServer();
                StartServerResult result = server.StartServer(@"adb\adb.exe", false);
                
                if (result != StartServerResult.Started)
                {
                    Console.WriteLine($"Server start result: {result}");
                    Console.WriteLine("Can't start adb server");
                }
                
                adbClient = new AdbClient();

                Console.WriteLine("running adb");


               
                
                
                var monitor = new DeviceMonitor(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)));
                monitor.DeviceDisconnected += this.OnDeviceDisconnected;
                monitor.DeviceConnected += this.OnDeviceConnected;
                monitor.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error initializing ADB: {e}");
               

            }
            
            
            
            // // Forward adb port
            // string adb_cmd = "cd adb && adb.exe forward tcp:8080 tcp:8080";
            // string result_2 = ShellHelper_2.ExecuteCommand(adb_cmd);
            // Console.WriteLine(result_2);
            //
            
            
            
            directX = new DirectX(hwnd);

            // directX_2 = new Directx_2(hwnd);

            // my_decoder = new del_decoder(directX._device);

            my_decoder2 = new del_decoder_2(directX._device);
            
            
            // deploy_server();
        }


        #region Adb_Area

        

        
        
        private void OnDeviceConnected(object? sender, DeviceDataEventArgs e)
        {
                    
                   
        }
        
        private void OnDeviceDisconnected(object? sender, DeviceDataEventArgs e)
        {
                    // Console.WriteLine("device disconnected event is working");
                   
        }

        private void UploadMobileServer()
        {
            using SyncService service = new(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)), device);
            using Stream stream = File.OpenRead(JAR);
            service.Push(stream, "/data/local/tmp/anlink.jar", 444, DateTime.Now, null, CancellationToken.None);
        }


        private void deploy_server()
        {



            try
            {

                // Thread.Sleep(2000);
                
                
                 if ( adbClient == null)
                 {
                     return;
                 }
                              
                 var devices = adbClient.GetDevices().FirstOrDefault();
                
                 device = devices;
                
                if (devices == null)
                {
                    Console.WriteLine("No devices found");
                                       
                    return;
                }
                                
                
                var cmd = new List<string>
                {
                    "CLASSPATH=/data/local/tmp/anlink.jar",
                    "app_process",
                    "/",
                    "ink.anl.Server",
                    

                    

                    

                    // ---- VIDEO SETTINGS FIRST ----
                    $"max_size={max_size}",
                
                    $"bit_rate={bitrate}",
                    

                    $"max_fps={max_fps}",
                   

                   
                    


                    // ---- CONTROL + TUNNEL ----
                    "tunnel_forward=false",

                    
                    
                    "encoder_name=c2.mtk.avc.encoder"
                    
                };

               
                
                
                
             
                
                
                
                
                

                cts = new CancellationTokenSource();
                
                
                
                // var receiver = new ConsoleOutputReceiver();
                var receiver = new MyReceiver();
                
                
                //
                // "audio_bit_rate=16000",
                // "audio_codec=opus",
                //
                UploadMobileServer();
                
                
                
                // string adb_cmd = "cd adb && adb.exe forward tcp:1011 localabstract:scrcpy && adb.exe forward tcp:1012 localabstract:scrcpy && adb.exe forward tcp:1013 localabstract:scrcpy";
                
                
                string adb_cmd = "cd adb && adb.exe reverse localabstract:anlink  tcp:1011 ";
                                
                string result = ShellHelper_2.ExecuteCommand(adb_cmd);
                Console.WriteLine(result);

                Console.WriteLine("adb reversed!");
                
                
                string command = string.Join(" ", cmd);
                // _ = adbClient.ExecuteRemoteCommandAsync(command, devices, receiver, cts.Token);
                
                Thread.Sleep(4000);
                
                try
                {
                    _ = adbClient.ExecuteRemoteCommandAsync(command, device, receiver, cts.Token);
                    // Console.WriteLine(command);
                    
                    
                    // adbClient.ExecuteRemoteCommand(command, device, receiver);
                    
                    // adbClient.ExecuteRemoteCommandAsync(command, device, receiver, cts.Token).Wait(cts.Token);
                    Console.WriteLine("started server");
                    // receiver.Flush();

                    while (_running)
                    {
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ADB command cancelled.");
                    
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #endregion
        
        public void Start()
        {
            if (_running) return;
            _running = true;

            _workerThread = new Thread(ReceiveLoop_Server);
            _workerThread.IsBackground = true;
            _workerThread.Start();
            
            // deploy_server();
            _adbThread = new Thread(deploy_server);
            _adbThread.IsBackground = true;
            _adbThread.Start();
        }

        public void Stop()
        {
            _running = false;
            _workerThread?.Join();
            _adbThread?.Join();
            
            my_decoder2?.Dispose();
        }

        private void ReceiveLoop()
        {
            try
            {
                    Thread.Sleep(2000);
                    videoClient = new TcpClient();
                    if (!videoClient.ConnectAsync(_host, 1011).Wait(15000))
                    {
                
                        // ErrorOccurred?.Invoke("Connection timeout");
                      
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
                        
                        // ErrorOccurred?.Invoke($"Expected to read dummy byte (1 byte), but got {dummyRead} bytes.");
                        
                    }
                    
                    Thread.Sleep(500);

                  
                    Thread.Sleep(1000);
                    
                    controlClient = new TcpClient();
                    if (!controlClient.ConnectAsync(_host, 1013).Wait(5000))
                    {
                        // throw new TimeoutException("Control connection timeout");
                        // ErrorOccurred?.Invoke("Control connection timeout");
                       
                    }
                    
                    
                    if (videoClient == null) throw new InvalidOperationException("Not connected");

                    var infoStream_2 = videoClient.GetStream();
                    infoStream_2.ReadTimeout = 2000;

                    // Read 68-byte header.
                    var deviceInfoBuf = pool.Rent(64);
                    int bytesRead = infoStream_2.Read(deviceInfoBuf, 0, 64);

                    Console.WriteLine("recived device info");

                    Console.WriteLine($"Received device info: {bytesRead} bytes");


                    // Console.WriteLine(bytesRead.length);
                    if (bytesRead != 64)
                    {
                        // throw new Exception($"Expected to read exactly 64 bytes for device name, but got {bytesRead} bytes.");
                        // ErrorOccurred?.Invoke($"Expected to read exactly 64 bytes for device name, but got {bytesRead} bytes.");
                        return;
                    }

                    // THIRD: Read 4-byte resolution


                    // Decode device name
                    var DeviceName = Encoding.UTF8.GetString(deviceInfoBuf, 0, 64).TrimEnd('\0');
                    Console.WriteLine("Device name: " + DeviceName);



                    //
                    // byte[] codecMeta = new byte[12];
                    // int codecBytesRead = infoStream_2.Read(codecMeta, 0, 12);
                    // if (codecBytesRead != 12)
                    // {
                    //     // throw new Exception($"Expected 12 bytes for codec metadata, got {codecBytesRead}");
                    //
                    //     return;
                    // }
                    //
                    // // Parse codec metadata (all big-endian u32)
                    // uint codecId = BinaryPrimitives.ReadUInt32BigEndian(codecMeta.AsSpan(0, 4));
                    // var Width = (int)BinaryPrimitives.ReadUInt32BigEndian(codecMeta.AsSpan(4, 4));
                    // var Height = (int)BinaryPrimitives.ReadUInt32BigEndian(codecMeta.AsSpan(8, 4));
                    //
                    // Console.WriteLine($"Codec ID: {codecId}, Resolution: {Width}x{Height}");
                    //
                    //
                    
                    
                 
                    // ReadDeviceInfo();
                    
                    // audioReadyEvent.Set();

                    if (videoClient == null || !videoClient.Connected)
                        return;

                    NetworkStream stream = videoClient.GetStream();

                    // CRITICAL: Remove or drastically increase timeout for scrcpy
                    stream.ReadTimeout = Timeout.Infinite; // No timeout - wait indefinitely

                    // Buffer for receiving data
                    // byte[] buffer = new byte[0x10000]; // 64KB buffer
                    byte[] buffer = new byte[0x100000];

                    Stopwatch sw = new Stopwatch();
                
                
                    int num = 8;

                    string mm = null;
                
                while (_running)
                {
                    try
                    {
                        
                        int bytesRead_2 = stream.Read(buffer, 0, buffer.Length);
                
                        if (bytesRead_2 > 0)
                        {
                
                
                            // Create a properly sized array for the received data
                            byte[] receivedData = new byte[bytesRead_2];
                            Buffer.BlockCopy(buffer, 0, receivedData, 0, bytesRead_2);
                
                
                            continue;
                            
                            sw.Restart();
                            
                            // At this point, frameData contains the raw H.264 frame
                            // Console.WriteLine($"Received frame: {frameLen} bytes");
                            
                            Texture2D texture = my_decoder2.Decode(receivedData);
                            
                            // Texture2D texture = my_decoder2.Decode(receivedData);
                            
                            
                            // Texture2D texture = my_decoder2.DecodeScrcpyStream(receivedData);

                            
                            
                            // Texture2D texture = my_decoder2.DecodeBytes(receivedData, receivedData.Length);
                            
                            // Console.WriteLine($"{texture.Dimension}");

                            if (texture != null)
                            {
                                
                            
                                // Thread.Sleep(5);
                                
                            var desc = texture.Description;
                            
                            // Console.WriteLine($"Width: {desc.Width}, Height: {desc.Height}, Format: {desc.Format}");
                            
                            
                            directX.PresentFrame(texture);
                            
                            
                            
                            
                            // directX_2.PresentFrame(texture);
                            
                            }
                            
                            
                            
                            sw.Stop(); // Stop measuring after presenting
                            
                            
                            Console.WriteLine($"Frame time (decode + render): {sw.Elapsed.TotalMilliseconds:F2} ms");

                            
                            continue;
                
                
                        }
                
                       
                        
                        
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Stream error: {ex.Message}");
                        Thread.Sleep(500);
                    }
                }
                
                
                
                
                
                
               

                // client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReceiveLoop error: {ex.Message}");
            }

            Console.WriteLine("Video receiving loop ended.");
        }
        
        ///////////////////////////////
        
        
        
        
        
       private void ReceiveLoop_Server()
{
    Thread.Sleep(2000);
    try
    {
        int port = 1011;
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"Server listening on port {port}...");
        
        Console.WriteLine("Waiting for VIDEO client...");
        videoClient = listener.AcceptTcpClient();
        videoClient.NoDelay = true;
        Console.WriteLine("Client connected!");
        
        NetworkStream videoStream = videoClient.GetStream();
        videoStream.ReadTimeout = Timeout.Infinite;
        Console.WriteLine("VIDEO client connected!");

        Console.WriteLine("Waiting for CONTROL client...");
        controlClient = listener.AcceptTcpClient();
        NetworkStream controlStream = controlClient.GetStream();
        Console.WriteLine("CONTROL client connected!");
        
        // Read device metadata
        byte[] deviceMeta = new byte[68];
        ReadFully(videoStream, deviceMeta, 0, 68);
        
        string deviceName = Encoding.UTF8.GetString(deviceMeta, 0, 64).TrimEnd('\0');
        Console.WriteLine("Device: " + deviceName);
        
        int width = (deviceMeta[64] << 8) | deviceMeta[65];
        int height = (deviceMeta[66] << 8) | deviceMeta[67];
        Console.WriteLine($"Resolution: {width}x{height}");

        if (videoClient == null || !videoClient.Connected)
        {
            Console.WriteLine(videoClient.Connected);
            return;
        }

        Console.WriteLine("started receiving...");
        
        const int MAX_FRAME_SIZE = 4 * 1024 * 1024;
        byte[] frameBuffer = new byte[MAX_FRAME_SIZE];
        byte[] header = new byte[12];

        // Frame skipping variables
        Stopwatch frameTimer = new Stopwatch();
        frameTimer.Start();
        double targetFrameTime = 1000.0 / 30; // ms per frame for target FPS
        int framesSkipped = 0;
        int framesProcessed = 0;
        int lastReportedFrames = 0;
        Stopwatch reportTimer = new Stopwatch();
        reportTimer.Start();

        // Frame rate control
        double accumulatedTime = 0;
        Texture2D lastTexture = null;

        while (_running)
        {
            try
            {
                ReadFully(videoStream, header, 0, 12);
                
                long ptsAndFlags = BinaryPrimitives.ReadInt64BigEndian(header.AsSpan(0, 8));
                int frameLen = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(8, 4));
                
                if (frameLen <= 0)
                {
                    Console.WriteLine("Invalid frame length, skipping...");
                    continue;
                }
                
                if (frameLen > frameBuffer.Length)
                {
                    Console.WriteLine($"Frame too large ({frameLen} bytes), skipping...");
                    SkipBytes(videoStream, frameLen);
                    continue;
                }

                // Read frame data
                ReadFully(videoStream, frameBuffer, 0, frameLen);

                // ALWAYS decode every frame to maintain stream integrity
                Stopwatch decodeTimer = new Stopwatch();
                decodeTimer.Start();
                Texture2D texture = my_decoder2.Decode(frameBuffer.AsSpan(0, frameLen).ToArray());
                decodeTimer.Stop();

                if (texture != null)
                {
                    double decodeTime = decodeTimer.Elapsed.TotalMilliseconds;
                    accumulatedTime += decodeTime;

                    // Frame presentation skipping logic
                    bool shouldPresent = true;
                    
                    // Calculate if we're running behind
                    if (accumulatedTime > targetFrameTime * 1.5) // 50% behind target
                    {
                        // Skip presenting this frame to catch up
                        shouldPresent = false;
                        framesSkipped++;
                        
                        // Use the latest texture but don't present it
                        lastTexture?.Dispose();
                        lastTexture = texture;
                    }
                    else
                    {
                        // Present the frame
                        directX.PresentFrame(texture);
                        framesProcessed++;
                        accumulatedTime = Math.Max(0, accumulatedTime - targetFrameTime);
                        
                        // Dispose previous skipped texture if any
                        lastTexture?.Dispose();
                        lastTexture = null;
                    }

                    // Performance reporting
                    if (reportTimer.Elapsed.TotalSeconds >= 5.0) // Report every 5 seconds
                    {
                        int totalFrames = framesProcessed + framesSkipped;
                        double actualFps = framesProcessed / reportTimer.Elapsed.TotalSeconds;
                        double skipRate = totalFrames > 0 ? (double)framesSkipped / totalFrames * 100 : 0;
                        
                        Console.WriteLine($"FPS: {actualFps:F1}/{max_fps}, " +
                                        $"Frames: {framesProcessed} presented, {framesSkipped} skipped ({skipRate:F1}% skip rate), " +
                                        $"Avg decode: {decodeTime:F1}ms");
                        
                        // Reset counters
                        framesProcessed = 0;
                        framesSkipped = 0;
                        reportTimer.Restart();
                    }
                }
                else
                {
                    Console.WriteLine("Failed to decode frame");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing frame: {ex.Message}");
                if (!videoClient.Connected) break;
            }
        }

        // Cleanup
        lastTexture?.Dispose();
        
        Console.WriteLine($"Final stats: {framesProcessed} frames presented, {framesSkipped} frames skipped");

        videoClient.Close();
        controlClient.Close();
        listener.Stop();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Server error: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}
        
        
        
        
//         
//         
//         private void ReceiveLoop_Server()
// {
//     
//     Thread.Sleep(2000);
//     try
//     {
//         int port = 1011;
//
//         // One listener ONLY
//         listener = new TcpListener(IPAddress.Any, port);
//         listener.Start();
//         Console.WriteLine($"Server listening on port {port}...");
//         
//         // deploy_server();
//         
//         // ---- FIRST CLIENT: VIDEO ----
//         Console.WriteLine("Waiting for VIDEO client...");
//         videoClient = listener.AcceptTcpClient();
//         videoClient.NoDelay = true;
//         
//         
//         
//         
//         
//
//         
//         
//         
//         Console.WriteLine("Client connected!");
//         NetworkStream videoStream = videoClient.GetStream();
//         videoStream.ReadTimeout = Timeout.Infinite;
//         Console.WriteLine("VIDEO client connected!");
//
//         // // ---- SECOND CLIENT: CONTROL ----
//         Console.WriteLine("Waiting for CONTROL client...");
//         controlClient = listener.AcceptTcpClient();
//         NetworkStream controlStream = controlClient.GetStream();
//         Console.WriteLine("CONTROL client connected!");
//         
//         
//         
//
//         // 2. Device name (64 bytes)
//         // byte[] devInfo = new byte[64];
//         // int devRead = videoStream.Read(devInfo, 0, 64);
//         // string deviceName = Encoding.UTF8.GetString(devInfo).TrimEnd('\0');
//         // Console.WriteLine("Device: " + deviceName);
//
//         
//         
//         byte[] deviceMeta = new byte[68];
//         ReadFully(videoStream, deviceMeta, 0, 68);
//         
//         // Parse device name (first 64 bytes, null-terminated)
//         string deviceName = Encoding.UTF8.GetString(deviceMeta, 0, 64).TrimEnd('\0');
//         Console.WriteLine("Device: " + deviceName);
//         
//         // Parse resolution (last 4 bytes: width(16-bit BE), height(16-bit BE))
//         int width = (deviceMeta[64] << 8) | deviceMeta[65];
//         int height = (deviceMeta[66] << 8) | deviceMeta[67];
//         Console.WriteLine($"Resolution: {width}x{height}");
//         
//         
//         // Thread.Sleep(3000);
//         
//         // 3. Codec metadata (12 bytes)
//         // byte[] codecMeta = new byte[12];
//         // int metaRead = videoStream.Read(codecMeta, 0, 12);
//         //
//         // uint codecId = BinaryPrimitives.ReadUInt32BigEndian(codecMeta.AsSpan(0, 4));
//         // int width = (int)BinaryPrimitives.ReadUInt32BigEndian(codecMeta.AsSpan(4, 4));
//         // int height = (int)BinaryPrimitives.ReadUInt32BigEndian(codecMeta.AsSpan(8, 4));
//         //
//         // Console.WriteLine($"Codec={codecId}, Res={width}x{height}");
//
//
//         if (videoClient == null || !videoClient.Connected)
//         {
//             
//             
//
//         Console.WriteLine(videoClient.Connected);
//             return;
//         
//         }
//         
//         // Thread.Sleep(2000);
//
//         Console.WriteLine("started reciving...");
//         
//         
//         // NetworkStream stream = videoClient.GetStream();
//         //
//         // // CRITICAL: Remove or drastically increase timeout for scrcpy
//         // stream.ReadTimeout = Timeout.Infinite;
//         
//         
//         
//         
//         
//         //
//         // // ---- FRAME LOOP ----
//         // byte[] buffer = new byte[0x100000];
//         //
//         // // byte[] buffer = new byte[1];
//         // // Stopwatch sw = new Stopwatch();
//         //
//         // while (_running)
//         // {
//         //     // Console.WriteLine(buffer.Length);
//         //     int len = videoStream.Read(buffer, 0, buffer.Length);
//         //
//         //     Console.WriteLine(len);
//         //     if (len <= 0) continue;
//         //     
//         //     
//         //
//         //     // Console.WriteLine("reciving");
//         //     byte[] frame = new byte[len];
//         //     Buffer.BlockCopy(buffer, 0, frame, 0, len);
//         //     
//         //     continue;
//         //
//         //     // sw.Restart();
//         //     // var texture = my_decoder2.Decode(frame);
//         //     
//         //     Texture2D texture = my_decoder2.Decode(frame);
//         //
//         //     if (texture != null)
//         //         directX.PresentFrame(texture);
//         //
//         //     // sw.Stop();
//         //     // Console.WriteLine($"Frame time: {sw.Elapsed.TotalMilliseconds:F2} ms");
//         // }
//         
//         
//         const int MAX_FRAME_SIZE = 4 * 1024 * 1024; // 4 MB max, adjust if needed
//         byte[] frameBuffer = new byte[MAX_FRAME_SIZE];
//         byte[] header = new byte[12];
//
//        
//
//         while (_running)
//         {
//             // 3️⃣ Read 12-byte frame header
//          
//
//
//             ReadFully(videoStream, header, 0, 12);
//                 
//             // Parse header - both are BIG ENDIAN
//             long ptsAndFlags = BinaryPrimitives.ReadInt64BigEndian(header.AsSpan(0, 8));
//             int frameLen = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(8, 4));
//                 
//             // Debug output
//             // Console.WriteLine($"PTS/Flags: {ptsAndFlags}, Frame length: {frameLen}");
//             
//             if (frameLen <= 0)
//             {
//                 Console.WriteLine("Invalid frame length, skipping...");
//                 continue;
//             }
//             
//             
//             if (frameLen > frameBuffer.Length)
//             {
//                 Console.WriteLine($"Frame too large ({frameLen} bytes), skipping...");
//                 // Skip the frame
//                 int skipped = 0;
//                 byte[] skipTemp = new byte[4096];
//                 while (skipped < frameLen)
//                 {
//                     int toRead = Math.Min(skipTemp.Length, frameLen - skipped);
//                     skipped += videoStream.Read(skipTemp, 0, toRead);
//                 }
//                 continue;
//             }
//
//             // 4️⃣ Read full frame into preallocated buffer
//             int offset = 0;
//             while (offset < frameLen)
//                 offset += videoStream.Read(frameBuffer, offset, frameLen - offset);
//
//             // 5️⃣ Decode frame (pass only the actual bytes)
//             Texture2D texture = my_decoder2.Decode(frameBuffer.AsSpan(0, frameLen).ToArray());
//             if (texture != null)
//                 directX.PresentFrame(texture);
//         }
//
//
//
//         videoClient.Close();
//         controlClient.Close();
//         listener.Stop();
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine($"Server error: {ex.Message}");
//
//         Console.WriteLine(ex.StackTrace);
//     }
// }
//
//         
        
        
        
        
        
        
        
        
        private void SkipBytes(NetworkStream stream, int bytesToSkip)
        {
            byte[] temp = new byte[4096];
            int skipped = 0;
            while (skipped < bytesToSkip)
            {
                int toRead = Math.Min(temp.Length, bytesToSkip - skipped);
                int read = stream.Read(temp, 0, toRead);
                if (read <= 0) break;
                skipped += read;
            }
        }
        
        
        static void ReadFully(Stream stream, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int n = stream.Read(buffer, offset, count);
                if (n <= 0)
                    throw new EndOfStreamException();
                offset += n;
                count -= n;
            }
        }

        
      
        
        private short ReadInt16BigEndian(byte[] buffer, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                return (short)((buffer[offset] << 8) | buffer[offset + 1]);
            }

            return BitConverter.ToInt16(buffer, offset);
        }
        
        
    }
}
