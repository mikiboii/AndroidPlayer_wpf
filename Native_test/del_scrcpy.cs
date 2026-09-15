using System.Diagnostics;
using System.Net.Sockets;
using Androidplayer_wpf.Src;
using SharpDX.Direct3D11;

namespace Androidplayer_wpf.Native_test
{
    public class del_scrcpy
    {
        private Thread _workerThread;
        DirectX directX;

        private del_decoder my_decoder;
        
        private bool _running = false;

        private string _host = "127.0.0.1";
        private int _port = 8080;

        public del_scrcpy(IntPtr hwnd)
        {
            // Forward adb port
            // string adb_cmd = "cd adb && adb.exe forward tcp:8080 tcp:8080";
            string adb_cmd = "cd adb && adb.exe reverse tcp:8080 tcp:8080";
            
            
            string result = ShellHelper_2.ExecuteCommand(adb_cmd);
            Console.WriteLine(result);
            
            
            directX = new DirectX(hwnd);

            my_decoder = new del_decoder(directX._device);
        }

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
                Console.WriteLine("Connecting to server...");

                TcpClient client = null;
                for (int i = 0; i < 30; i++)
                {
                    try
                    {
                        client = new TcpClient();
                        client.Connect(_host, _port);
                        Console.WriteLine("Connected to server!");
                        break;
                    }
                    catch (SocketException)
                    {
                        Thread.Sleep(100);
                    }
                }

                if (client == null || !client.Connected)
                {
                    Console.WriteLine("Failed to connect to server.");
                    return;
                }

                NetworkStream stream = client.GetStream();
                stream.ReadTimeout = 1000;

                Console.WriteLine("Starting to receive video data...");

                Stopwatch sw = new Stopwatch();
                
                
                
                while (_running)
                {
                    try
                    {
                        
                        
                        
                        
                        
                        
                        
                        // Read 4-byte frame length (big-endian)
                        byte[] lengthBytes = new byte[4];
                        int read = 0;
                        while (read < 4)
                        {
                            int n = stream.Read(lengthBytes, read, 4 - read);
                            if (n == 0) throw new Exception("Server closed connection");
                            read += n;
                        }

                        int frameLen = BitConverter.ToInt32(new byte[] { lengthBytes[3], lengthBytes[2], lengthBytes[1], lengthBytes[0] }, 0);

                        // Read frame data
                        byte[] frameData = new byte[frameLen];
                        read = 0;
                        while (read < frameLen)
                        {
                            int n = stream.Read(frameData, read, frameLen - read);
                            if (n == 0) throw new Exception("Server closed connection");
                            read += n;
                        }

                        sw.Restart();
                        
                        // At this point, frameData contains the raw H.264 frame
                        // Console.WriteLine($"Received frame: {frameLen} bytes");

                        Texture2D texture = my_decoder.DecodePacketToTexture(frameData);


                        // Console.WriteLine($"{texture.Dimension}");

                        var desc = texture.Description;

                        // Console.WriteLine($"Width: {desc.Width}, Height: {desc.Height}, Format: {desc.Format}");

                        
                        directX.PresentFrame(texture);
                        
                        
                        
                        sw.Stop(); // Stop measuring after presenting
            
                        // 3️⃣ Print time in milliseconds
                        Console.WriteLine($"Frame time (decode + render): {sw.Elapsed.TotalMilliseconds:F2} ms");

                        
                        // Optional: you could queue it for decoding elsewhere
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Stream error: {ex.Message}");
                        Thread.Sleep(500);
                    }
                }

                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReceiveLoop error: {ex.Message}");
            }

            Console.WriteLine("Video receiving loop ended.");
        }
    }
}
