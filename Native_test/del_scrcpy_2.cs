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
    public class del_scrcpy_2
    {
         private readonly AdbClient adbClient;
         private DeviceData device;
         private CancellationTokenSource cts;       
                
         private TcpClient? videoClient;
         private TcpClient? controlClient;
         
         
        private Thread _workerThread;
        DirectX directX;

        private Directx_2 directX_2;
        
        private del_decoder my_decoder;
        
        private del_decoder_2 my_decoder2;
        
        private bool _running = false;

        private string _host = "127.0.0.1";
        private int _port = 8080;
        
        private static readonly ArrayPool<byte> pool = ArrayPool<byte>.Shared;
        
        public string JAR = "scrcpy-server-3_old.jar";
        
        // public string JAR = "scrcpy-server-3.jar";
        
        public string VERSION = "1.20";
        public int max_size = 1080;
        // public int bitrate = 8000000;
        public int bitrate = 8000000;
        // public int bitrate = 20000;
        
        
        public int max_fps = 60;
        public bool block_frame = true;
        public bool stay_awake = false;
        public int lock_screen_orientation = -1;
        public bool skip_same_frame = false;
        public double min_frame_interval => 1.0 / max_fps;

        public del_scrcpy_2(IntPtr hwnd)
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
            
            
            deploy_server();
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
            service.Push(stream, "/data/local/tmp/scrcpy-server.jar", 444, DateTime.Now, null, CancellationToken.None);
        }


        private void deploy_server()
        {



            try
            {

                
                
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
                    "CLASSPATH=/data/local/tmp/scrcpy-server.jar",
                    "app_process",
                    "/",
                    "com.genymobile.scrcpy.Server",
                    "3.3.2",

                    "log_level=info",

                    "video=true",

                    // ---- VIDEO SETTINGS FIRST ----
                    $"max_size={max_size}",
                
                    $"video_bit_rate={bitrate}",
                    

                    $"max_fps={max_fps}",
                    "video_codec=h264",

                    // ---- AUDIO SETTINGS AFTER VIDEO ----
                    $"audio=false",
                    


                    // ---- CONTROL + TUNNEL ----
                    "tunnel_forward=true",
                    "control=true",

                    "cleanup=true",
                    "send_device_meta=true",
                    "send_codec_meta=true",
                    "send_frame_meta=false"
                };
                
                
                
                
                
                
                var cmd_2 = new List<string>
                {
                    "CLASSPATH=/data/local/tmp/scrcpy-server.jar",
                    "app_process",
                    "/",
                    "com.genymobile.scrcpy.Server",
                    "3.3.2",
                    "log_level=info",
                    "video=true",
                    $"max_size={max_size}",
                    $"video_bit_rate={bitrate}",
                    $"max_fps={max_fps}",
                    "video_codec=h264",
                    "audio=false",
                    "tunnel_forward=true",
                    "control=true",
                    "cleanup=true",
                    "send_device_meta=true",
                    "send_codec_meta=true",
                    "send_frame_meta=true", // Changed to true for better sync
                    "send_dummy_byte=true"  // Important for connection stability
                };
                
                
                
                
                
                

                cts = new CancellationTokenSource();
                var receiver = new ConsoleOutputReceiver();
                
                //
                // "audio_bit_rate=16000",
                // "audio_codec=opus",
                //
                UploadMobileServer();
                
                
                
                string adb_cmd = "cd adb && adb.exe forward tcp:1011 localabstract:scrcpy && adb.exe forward tcp:1012 localabstract:scrcpy && adb.exe forward tcp:1013 localabstract:scrcpy";
                
                
                // string adb_cmd = "cd adb && adb.exe reverse tcp:1011 localabstract:scrcpy && adb.exe reverse tcp:1012 localabstract:scrcpy && adb.exe reverse tcp:1013 localabstract:scrcpy";
                                
                string result = ShellHelper_2.ExecuteCommand(adb_cmd);
                Console.WriteLine(result);
                
                
                string command = string.Join(" ", cmd);
                // _ = adbClient.ExecuteRemoteCommandAsync(command, devices, receiver, cts.Token);
                
                Thread.Sleep(200);
                
                try
                {
                    _ = adbClient.ExecuteRemoteCommandAsync(command, device, receiver, cts.Token);
                    Console.WriteLine("started server");
                    
                    // adbClient.ExecuteRemoteCommandAsync(command, device, receiver, cts.Token).Wait(cts.Token);
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

            _workerThread = new Thread(ReceiveLoop);
            _workerThread.IsBackground = true;
            _workerThread.Start();
        }

        public void Stop()
        {
            _running = false;
            _workerThread?.Join();
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




                    byte[] codecMeta = new byte[12];
                    int codecBytesRead = infoStream_2.Read(codecMeta, 0, 12);
                    if (codecBytesRead != 12)
                    {
                        // throw new Exception($"Expected 12 bytes for codec metadata, got {codecBytesRead}");
                    
                        return;
                    }

                    // Parse codec metadata (all big-endian u32)
                    uint codecId = BinaryPrimitives.ReadUInt32BigEndian(codecMeta.AsSpan(0, 4));
                    var Width = (int)BinaryPrimitives.ReadUInt32BigEndian(codecMeta.AsSpan(4, 4));
                    var Height = (int)BinaryPrimitives.ReadUInt32BigEndian(codecMeta.AsSpan(8, 4));

                    Console.WriteLine($"Codec ID: {codecId}, Resolution: {Width}x{Height}");
                    
                    
                    
                    
                 
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
                            
                            
                            // Console.WriteLine($"Frame time (decode + render): {sw.Elapsed.TotalMilliseconds:F2} ms");

                            
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
