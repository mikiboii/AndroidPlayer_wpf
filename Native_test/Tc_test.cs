


using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Androidplayer_wpf.Src.Keymap.K_store;
using OPack;
using SharpAdbClient;

namespace Androidplayer_wpf.Native_test;

public class Tc_test
{
    
    private my_AV? _decoder;
    
    private readonly AdbClient adbClient;
    private DeviceData device;
    private CancellationTokenSource cts;  
    
    private Thread _workerThread;
    private bool _running = false;
    
    private TcpClient _controlClient;
    private NetworkStream _controlStream;

    
    
    private TcpClient _dataClient;
    private NetworkStream _dataStream;   // add this too
    
    private static readonly byte[] Magic = System.Text.Encoding.ASCII.GetBytes("RCMS");
    private const ushort Version = 1;
    private ushort _sequence = 0;
    
    
    public Tc_test()
    {
        Console.WriteLine("Tc_test called");
        
        
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
        
        
        deploy_server();
        
        
        
    }
    
    private void OnDeviceConnected(object? sender, DeviceDataEventArgs e)
    {
                    
                   
    }
        
    private void OnDeviceDisconnected(object? sender, DeviceDataEventArgs e)
    {
        // Console.WriteLine("device disconnected event is working");
                   
    }


    private void deploy_server()
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
        Console.WriteLine(device.Serial);
        cts = new CancellationTokenSource();
                
                
                
        // var receiver = new ConsoleOutputReceiver();
        var receiver = new MyReceiver();



        // adbClient.CreateForward(device, 10001, 1200);

        adbClient.CreateForward(device, 10001, 12010);
        adbClient.CreateForward(device, 10002, 12003);
        adbClient.CreateForward(device, 10003, 12012);
        adbClient.CreateForward(device, 10004, 12028);
        adbClient.CreateForward(device, 10005, 12026);
        adbClient.CreateForward(device, 10006, 12001);
        adbClient.CreateForward(device, 10007, 12002);
        adbClient.CreateForward(device, 10008, 12015);
        
        
        // adbClient.ExecuteRemoteCommand("/data/local/tmp/tcg/mobileserverstop.sh", device, receiver);

        // _ = adbClient.ExecuteRemoteCommandAsync("/data/local/tmp/tcg/mobileserverstop.sh", device, receiver, cts.Token);
        
        
        // Thread.Sleep(2000);
        // adbClient.ExecuteRemoteCommand("/data/local/tmp/tcg/mobileserverstart.sh", device, receiver);
        
        // _ = adbClient.ExecuteRemoteCommandAsync("/data/local/tmp/tcg/mobileserverstart.sh", device, receiver, cts.Token);
        
        // Thread.Sleep(2000);
        Console.WriteLine("adb finished!");

        // adb shell /data/local/tmp/tcg/mobileserverstart.sh
        // adb shell /data/local/tmp/tcg/mobileserverstop.sh

    }



    public void Start()
    {
        if (_running) return;
        _running = true;

        _workerThread = new Thread(Tcp_Server);
        _workerThread.IsBackground = true;
        _workerThread.Start();
            
     
    }

    public void Stop()
    {
        _running = false;
        _workerThread?.Join();
        
    }






    #region Helpers

    
    
    
    private ushort NextSequence()
    {
        _sequence = (ushort)((_sequence + 1) % 65536);
        return _sequence;
    }

    // private void SendControl(uint command, byte[] payload = null)
    // {
    //     payload ??= Array.Empty<byte>();
    //
    //     ushort seq = NextSequence();
    //     bool hasPayload = payload.Length > 0;
    //
    //     uint length = hasPayload
    //         ? (uint)(12 + payload.Length)
    //         : 8u;
    //
    //     // Python:
    //     // struct.pack(">4sHHIII", MAGIC, VERSION, command, seq, length, 0)
    //     byte[] header = new byte[20];
    //
    //     // 4s - MAGIC
    //     Buffer.BlockCopy(Magic, 0, header, 0, 4);
    //
    //     // H - VERSION
    //     BinaryPrimitives.WriteUInt16BigEndian(
    //         header.AsSpan(4, 2), Version);
    //
    //     // H - COMMAND
    //     BinaryPrimitives.WriteUInt16BigEndian(
    //         header.AsSpan(6, 2), (ushort)command);
    //
    //     // I - SEQUENCE
    //     BinaryPrimitives.WriteUInt32BigEndian(
    //         header.AsSpan(8, 4), seq);
    //
    //     // I - LENGTH
    //     BinaryPrimitives.WriteUInt32BigEndian(
    //         header.AsSpan(12, 4), length);
    //
    //     // I - RESERVED
    //     BinaryPrimitives.WriteUInt32BigEndian(
    //         header.AsSpan(16, 4), 0);
    //
    //     _controlStream.Write(header, 0, header.Length);
    //     _controlStream.Flush();
    //
    //     if (hasPayload)
    //     {
    //         Thread.Sleep(5);
    //
    //         // Python:
    //         // struct.pack(">I", len(payload))
    //         byte[] payloadSize = new byte[4];
    //
    //         BinaryPrimitives.WriteUInt32BigEndian(
    //             payloadSize,
    //             (uint)payload.Length);
    //
    //         _controlStream.Write(
    //             payloadSize, 0, payloadSize.Length);
    //
    //         _controlStream.Write(
    //             payload, 0, payload.Length);
    //
    //         _controlStream.Flush();
    //     }
    // }
    
    
    private void SendControl(uint command, byte[] payload = null)
    {
        payload ??= Array.Empty<byte>();

        ushort seq = NextSequence();

        uint length = payload.Length > 0
            ? (uint)(12 + payload.Length)
            : 8u;

        // Python:
        // struct.pack(">4sHHIII",
        //     MAGIC, VERSION, seq, length, command, 0)

        byte[] header = new byte[20];

        // 4s - MAGIC
        Buffer.BlockCopy(Magic, 0, header, 0, 4);

        // H - VERSION
        BinaryPrimitives.WriteUInt16BigEndian(
            header.AsSpan(4, 2),
            Version);

        // H - SEQUENCE
        BinaryPrimitives.WriteUInt16BigEndian(
            header.AsSpan(6, 2),
            seq);

        // I - LENGTH
        BinaryPrimitives.WriteUInt32BigEndian(
            header.AsSpan(8, 4),
            length);

        // I - COMMAND
        BinaryPrimitives.WriteUInt32BigEndian(
            header.AsSpan(12, 4),
            command);

        // I - SUB
        BinaryPrimitives.WriteUInt32BigEndian(
            header.AsSpan(16, 4),
            0);

        Console.WriteLine(
            $"> command={command}, seq={seq}, length={length}, payload={payload.Length}");

        // Python sendall(header)
        _controlStream.Write(header, 0, header.Length);

        if (payload.Length > 0)
        {
            // Python:
            // header += struct.pack(">I", len(payload))
            //
            // IMPORTANT:
            // payload size is part of the header write in Python,
            // then the actual JSON payload is a separate write.

            Thread.Sleep(5);

            byte[] size = new byte[4];

            BinaryPrimitives.WriteUInt32BigEndian(
                size,
                (uint)payload.Length);

            _controlStream.Write(size, 0, size.Length);

            // Python sendall(payload)
            _controlStream.Write(
                payload,
                0,
                payload.Length);
        }

        _controlStream.Flush();
    }

    private void SendJson(uint command, byte[] jsonBytes)
    {
        SendControl(command, jsonBytes);
    }

    
    
    
    private byte[] RecvExact(int size)
    {
        byte[] buffer = new byte[size];
        int received = 0;

        while (received < size)
        {
            int n = _controlStream.Read(
                buffer,
                received,
                size - received);

            if (n == 0)
                return null;

            received += n;
        }

        return buffer;
    }
    
    
    
    
    // private bool ReadControlMessage()
    // {
    //     byte[] header = RecvExact(20);
    //
    //     if (header == null)
    //     {
    //         Console.WriteLine("Control connection closed while waiting for header.");
    //         return false;
    //     }
    //
    //     string magic = System.Text.Encoding.ASCII.GetString(
    //         header, 0, 4);
    //
    //     ushort version = BinaryPrimitives.ReadUInt16BigEndian(
    //         header.AsSpan(4, 2));
    //
    //     ushort command = BinaryPrimitives.ReadUInt16BigEndian(
    //         header.AsSpan(6, 2));
    //
    //     uint sequence = BinaryPrimitives.ReadUInt32BigEndian(
    //         header.AsSpan(8, 4));
    //
    //     uint length = BinaryPrimitives.ReadUInt32BigEndian(
    //         header.AsSpan(12, 4));
    //
    //     uint reserved = BinaryPrimitives.ReadUInt32BigEndian(
    //         header.AsSpan(16, 4));
    //
    //     Console.WriteLine(
    //         $"< command={command}, seq={sequence}, length={length}");
    //
    //     if (length > 8)
    //     {
    //         byte[] size = RecvExact(4);
    //
    //         if (size == null)
    //         {
    //             Console.WriteLine("Control connection closed while reading payload size.");
    //             return false;
    //         }
    //
    //         uint payloadSize =
    //             BinaryPrimitives.ReadUInt32BigEndian(size);
    //
    //         byte[] payload = RecvExact((int)payloadSize);
    //
    //         if (payload == null)
    //         {
    //             Console.WriteLine("Control connection closed while reading payload.");
    //             return false;
    //         }
    //
    //         Console.WriteLine(
    //             $"< payload={payloadSize} bytes");
    //
    //         Console.WriteLine(
    //             System.Text.Encoding.UTF8.GetString(payload));
    //     }
    //
    //     return true;
    // }
    
    
    
    
    private bool ReadControlMessage()
    {
        byte[] header = RecvExact(20);

        if (header == null)
        {
            Console.WriteLine("Control connection closed.");
            return false;
        }

        string magic = System.Text.Encoding.ASCII.GetString(
            header, 0, 4);

        ushort version =
            BinaryPrimitives.ReadUInt16BigEndian(
                header.AsSpan(4, 2));

        ushort seq =
            BinaryPrimitives.ReadUInt16BigEndian(
                header.AsSpan(6, 2));

        uint length =
            BinaryPrimitives.ReadUInt32BigEndian(
                header.AsSpan(8, 4));

        uint command =
            BinaryPrimitives.ReadUInt32BigEndian(
                header.AsSpan(12, 4));

        uint sub =
            BinaryPrimitives.ReadUInt32BigEndian(
                header.AsSpan(16, 4));

        if (magic != "RCMS")
        {
            Console.WriteLine($"Invalid magic: {magic}");
            return false;
        }

        Console.WriteLine(
            $"<- command={command}, seq={seq}, length={length}");

        byte[] payload = Array.Empty<byte>();

        if (length > 8)
        {
            byte[] sizeBytes = RecvExact(4);

            if (sizeBytes == null)
                return false;

            uint size =
                BinaryPrimitives.ReadUInt32BigEndian(sizeBytes);

            payload = RecvExact((int)size);

            if (payload == null)
                return false;

            Console.WriteLine(
                $"<- payload={size} bytes");

            Console.WriteLine(
                System.Text.Encoding.UTF8.GetString(payload));
        }

        return true;
    }

    #endregion


    
    
    
    
    
    

    private void Tcp_Server()
    {

        // while (_running)
        // {
        //     
        //     Thread.Sleep(2000);
        //     Console.WriteLine("Tcp server running...");
        // }
        
        
        _controlClient = new TcpClient();
        _controlClient.ReceiveTimeout = 5000;
        _controlClient.SendTimeout = 5000;
        _controlClient.Connect("127.0.0.1", 10001);
        _controlStream = _controlClient.GetStream();

        Console.WriteLine("Control socket connected.");

        
        // k_info.Instance.directx.my_Device
        
        _decoder = new my_AV(k_info.Instance.directx.my_Device);

        
        
        SendJson(1025, System.Text.Encoding.UTF8.GetBytes("{\"id\":17}"));
        Console.WriteLine("Sent 1025 {\"id\":17}");
        
        // Python:
        // send_control(1069)
        SendControl(1069);

        Console.WriteLine("Sent 1069");

        // Python:
        // send_control(1103)
        SendControl(1103);

        string deviceJson =
            $"{{\"connected_device_name\":\"PC Client\",\"connected_mode\":2,\"connected_serial\":\"{device.Serial}\"}}";

        SendJson(
            1061,
            System.Text.Encoding.UTF8.GetBytes(deviceJson)
        );

        Console.WriteLine($"Sent 1061 {deviceJson}");

        SendJson(
            1089,
            System.Text.Encoding.UTF8.GetBytes("{\"client_version\":\"3.0.47.16388\"}")
        );

        Console.WriteLine("Sent 1089 {\"client_version\":\"3.0.47.16388\"}");
        
        // Python:
// send_control(1153)
        SendControl(1153);

        Console.WriteLine("Sent 1153");

// Python:
// send_control(1105)
        SendControl(1105);

        Console.WriteLine("Sent 1105");

// Python:
// send_json(1053, b'{"bitrate":16000000,...}')



        // string settingsJson =
        //     "{\"bitrate\":16000000," +
        //     "\"bitrateMode\":1," +
        //     "\"encodeType\":1," +
        //     "\"fps\":60," +
        //     "\"iFrameInter\":30," +
        //     "\"quality\":0," +
        //     "\"qualityRate\":100," +
        //     "\"resolution\":480}";
        //
        
        
        string settingsJson =
            "{\"bitrate\":16000000," +
            "\"bitrateMode\":1," +
            "\"encodeType\":1," +
            "\"fps\":60," +
            "\"iFrameInter\":30," +
            "\"quality\":0," +
            "\"qualityRate\":100," +
            "\"resolution\":480}";

        SendJson(
            1053,
            System.Text.Encoding.UTF8.GetBytes(settingsJson)
        );

        Console.WriteLine($"Sent 1053 {settingsJson}");
        
        
        Console.WriteLine("Waiting for 3 control messages...");
        
        ReadControlMessage();
        ReadControlMessage();
        ReadControlMessage();
        
        Console.WriteLine("Received 3 control messages.");
        
        Console.WriteLine("Received 3 control messages.");

// --- Data port handshake ---
        _dataClient = new TcpClient();
        _dataClient.ReceiveTimeout = 5000;
        _dataClient.SendTimeout = 5000;
        _dataClient.Connect("127.0.0.1", 10002);
        _dataStream = _dataClient.GetStream();

        Console.WriteLine("Data socket connected.");

        byte[] syn = new byte[]
        {
            0x00, 0x00, 0x00, 0x01,
            (byte)'E', (byte)'X', (byte)'R', (byte)'S', (byte)' ',
            (byte)'S', (byte)'Y', (byte)'N', (byte)'V',
            0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00
        };

        _dataStream.Write(syn, 0, syn.Length);
        _dataStream.Flush();

        byte[] ack = RecvExactData(20);
        Console.WriteLine($"<- data port ack: {BitConverter.ToString(ack)}");

        _dataStream.Write(ack, 0, ack.Length);
        _dataStream.Flush();
        Console.WriteLine("Echoed ack back.");
        
        
        // Two raw (non-JSON) control sends
        byte[] rawPing = { 0x00, 0x00, 0xff, 0xa3, 0x00, 0x00, 0x00, 0x03 };
        SendControl(109, rawPing);
        SendControl(109, rawPing);

        SendJson(1097, System.Text.Encoding.UTF8.GetBytes("{\"statue\":false}"));
        SendJson(1099, System.Text.Encoding.UTF8.GetBytes("{\"lowest\":false}"));
        SendControl(1107);
        SendJson(1083, System.Text.Encoding.UTF8.GetBytes("{\"model\":1}"));
        SendJson(1095, System.Text.Encoding.UTF8.GetBytes("{\"status\":false}"));

        Console.WriteLine("Handshake complete.");
        
        // ReceiveVideo();
        
        ReceiveVideo_2();
        
        // ReceiveVideoToFile();
        
        
        
        
        
        
        
    }
    
    
    private byte[] RecvExactData(int size) => RecvExact(_dataStream, size);
    private static byte[] RecvExact(NetworkStream stream, int size)
    {
        byte[] buffer = new byte[size];
        int received = 0;
        while (received < size)
        {
            int n = stream.Read(buffer, received, size - received);
            if (n == 0) return null;
            received += n;
        }
        return buffer;
    }
    
    
    
    
    
    private void ReceiveVideo(int durationSeconds = 15)
{
    _dataClient.ReceiveTimeout = 500;

    var buf = new MemoryStream();
    int parsedPos = 0;                       // how much of `buf` we've already parsed
    var start = DateTime.UtcNow;

    while (_running)
    {
        try
        {
            byte[] tmp = new byte[65536];
            int read = _dataStream.Read(tmp, 0, tmp.Length);
            if (read == 0) break;
            buf.Write(tmp, 0, read);
        }
        catch (IOException) { /* receive timeout -- just loop */ }

        // Try to parse as many complete AUs as we now have
        byte[] raw = buf.GetBuffer();        // no copy; only first buf.Length bytes valid
        int avail = (int)buf.Length;

        Console.WriteLine(avail);

        while (parsedPos + 52 <= avail)
        {
            if (BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(parsedPos)) != 0x07000000)
            {
                parsedPos++;
                continue;
            }

            uint f1   = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(parsedPos + 4));
            uint f2   = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(parsedPos + 8));
            uint s0   = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(parsedPos + 36));
            uint s1   = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(parsedPos + 40));
            uint size = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(parsedPos + 44));

            if ((f1 & 0xFFFF0000) != 0x00060000 || f2 != 256 || s0 != 0 || s1 != 32)
            {
                parsedPos++;
                continue;
            }

            int payloadStart = parsedPos + 52;
            if (payloadStart + (long)size > avail)
                break;   // incomplete AU -- wait for more data

            if (size > 0)  // skip the 0-byte announce
            {
                byte[] h264 = new byte[size];
                Buffer.BlockCopy(raw, payloadStart, h264, 0, (int)size);


                // Console.WriteLine(h264.Length);
                
                var tex = _decoder.Decode(h264);
                if (tex != null)
                {
                    // Present on the UI thread (D3D11 is thread-affine)
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        // k_info.Instance.directx.PresentFrame(tex);
                        
                        try
                        {
                            k_info.Instance.directx.PresentFrame(tex);
                        }
                        finally
                        {
                            tex.Dispose();
                        }
                    });
                    // tex.Dispose();
                }
            }

            parsedPos = payloadStart + (int)size;
        }
    }

    Console.WriteLine("Capture loop ended.");
}
    
    
  
    
    private void ReceiveVideo_2(int durationSeconds = 15)
{
    _dataClient.ReceiveTimeout = 500;

    const int HeaderSize = 52;
    const int MaxPayloadSize = 3_264_052; // Matches server's C buffer size

    var buf = new MemoryStream();
    int parsedPos = 0;

    var start = DateTime.UtcNow;

    while (_running)
    {
        try
        {
            byte[] tmp = new byte[65536];

            int read = _dataStream.Read(tmp, 0, tmp.Length);

            if (read == 0)
                break;

            buf.Write(tmp, 0, read);
        }
        catch (IOException)
        {
            // ReceiveTimeout = 500 ms.
            // Timeout is normal; continue parsing whatever we already received.
        }
        catch (ObjectDisposedException)
        {
            break;
        }

        int avail = (int)buf.Length;

        if (avail <= parsedPos)
            continue;

        byte[] raw = buf.GetBuffer();

        // ------------------------------------------------------------
        // Parse as many complete packets as possible.
        // Packet format:
        //
        //   52-byte header
        //   H264 payload
        //
        // Payload size is stored at offset 44.
        // ------------------------------------------------------------

        while (parsedPos + HeaderSize <= avail)
        {
            int packetStart = parsedPos;

            // --------------------------------------------------------
            // Byte 0 = packet type.
            //
            // Java:
            //     bArr[0] = 7;
            // --------------------------------------------------------

            byte packetType = raw[packetStart];

            if (packetType != 7)
            {
                // Not the beginning of a video packet.
                // Search for the next possible packet.
                parsedPos++;
                continue;
            }

            // --------------------------------------------------------
            // Basic header fields
            // --------------------------------------------------------

            byte state = raw[packetStart + 4];
            byte videoType = raw[packetStart + 5];
            byte frameType = raw[packetStart + 7];

            byte profile = raw[packetStart + 8];

            // Java:
            //     bArr[10] = 1;   // H264
            //     bArr[10] = 2;   // H265
            //
            byte codec = raw[packetStart + 10];

            // Java:
            //     bArr[11] = bVar.f1477e;
            //
            // 0/2 = portrait-style orientation
            // 1/3 = landscape-style orientation
            //
            byte rotation = raw[packetStart + 11];

            // --------------------------------------------------------
            // Timestamp
            // Java h1.d.s() is BIG-ENDIAN.
            // --------------------------------------------------------

            long presentationTimeUs =
                BinaryPrimitives.ReadInt64BigEndian(
                    raw.AsSpan(packetStart + 12, 8));

            // --------------------------------------------------------
            // Screen dimensions
            // --------------------------------------------------------

            uint screenWidth =
                BinaryPrimitives.ReadUInt32BigEndian(
                    raw.AsSpan(packetStart + 20, 4));

            uint screenHeight =
                BinaryPrimitives.ReadUInt32BigEndian(
                    raw.AsSpan(packetStart + 24, 4));

            // --------------------------------------------------------
            // Encoder/video dimensions
            // --------------------------------------------------------

            uint videoWidth =
                BinaryPrimitives.ReadUInt32BigEndian(
                    raw.AsSpan(packetStart + 28, 4));

            uint videoHeight =
                BinaryPrimitives.ReadUInt32BigEndian(
                    raw.AsSpan(packetStart + 32, 4));

            // --------------------------------------------------------
            // Remaining header fields
            // --------------------------------------------------------

            uint field36 =
                BinaryPrimitives.ReadUInt32BigEndian(
                    raw.AsSpan(packetStart + 36, 4));

            uint field40 =
                BinaryPrimitives.ReadUInt32BigEndian(
                    raw.AsSpan(packetStart + 40, 4));

            // --------------------------------------------------------
            // Java:
            //
            // System.arraycopy(
            //     h1.d.q(cVar.f1494p.size),
            //     0,
            //     bArr,
            //     44,
            //     4
            // );
            //
            // Therefore offset 44 = MediaCodec BufferInfo.size
            // --------------------------------------------------------

            uint payloadSize =
                BinaryPrimitives.ReadUInt32BigEndian(
                    raw.AsSpan(packetStart + 44, 4));

            // --------------------------------------------------------
            // Basic sanity checks
            // --------------------------------------------------------

            if (codec != 1)
            {
                Console.WriteLine(
                    $"Skipping packet: unsupported codec={codec}, " +
                    $"rotation={rotation}, " +
                    $"screen={screenWidth}x{screenHeight}, " +
                    $"video={videoWidth}x{videoHeight}");

                parsedPos++;
                continue;
            }

            if (payloadSize > MaxPayloadSize)
            {
                Console.WriteLine(
                    $"Invalid payload size: {payloadSize}. " +
                    $"packetStart={packetStart}, avail={avail}");

                parsedPos++;
                continue;
            }

            int payloadStart = packetStart + HeaderSize;

            // --------------------------------------------------------
            // IMPORTANT:
            //
            // TCP may have given us only part of the H264 payload.
            // Don't consume anything until the complete payload exists.
            // --------------------------------------------------------

            long packetEnd = (long)payloadStart + payloadSize;

            if (packetEnd > avail)
            {
                // Header is valid, but the complete H264 frame hasn't
                // arrived yet. Wait for another Receive().
                break;
            }

            // --------------------------------------------------------
            // Log orientation/dimensions.
            // This is especially useful for our landscape problem.
            // --------------------------------------------------------

            // Console.WriteLine(
            //     $"VIDEO packet: " +
            //     $"rotation={rotation}, " +
            //     $"frameType={frameType}, " +
            //     $"screen={screenWidth}x{screenHeight}, " +
            //     $"video={videoWidth}x{videoHeight}, " +
            //     $"payload={payloadSize}, " +
            //     $"pts={presentationTimeUs}");

            // --------------------------------------------------------
            // The server sends a zero-byte packet as an announcement.
            // Don't send that to the decoder.
            // --------------------------------------------------------

            if (payloadSize > 0)
            {
                byte[] h264 = new byte[(int)payloadSize];

                Buffer.BlockCopy(
                    raw,
                    payloadStart,
                    h264,
                    0,
                    (int)payloadSize);

                // Console.WriteLine(
                //     $"  -> Sending {h264.Length} bytes to decoder");

                try
                {
                    var tex = _decoder.Decode(h264);

                    // Console.WriteLine(
                    //     tex != null
                    //         ? "  -> Decoder returned texture"
                    //         : "  -> Decoder returned null");

                    if (tex != null)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                k_info.Instance.directx.PresentFrame(tex);
                            }
                            finally
                            {
                                tex.Dispose();
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"  -> DECODER ERROR: {ex}");
                }
            }
            else
            {
                Console.WriteLine(
                    $"  -> Zero-byte video packet " +
                    $"(frameType={frameType})");
            }

            // --------------------------------------------------------
            // We successfully consumed this complete packet.
            // --------------------------------------------------------

            parsedPos = payloadStart + (int)payloadSize;
        }

        // ------------------------------------------------------------
        // Compact the MemoryStream.
        //
        // Without this, parsedPos and buf will grow forever during
        // long-running capture sessions.
        // ------------------------------------------------------------

        if (parsedPos > 0)
        {
            int remaining = (int)buf.Length - parsedPos;

            if (remaining > 0)
            {
                Buffer.BlockCopy(
                    raw,
                    parsedPos,
                    raw,
                    0,
                    remaining);
            }

            buf.SetLength(remaining);
            parsedPos = 0;
        }

        // ------------------------------------------------------------
        // Optional duration limit.
        // ------------------------------------------------------------
        //
        // if (durationSeconds > 0 &&
        //     (DateTime.UtcNow - start).TotalSeconds >= durationSeconds)
        // {
        //     break;
        // }
    }

    Console.WriteLine("Capture loop ended.");
}
    
    

/// <summary>
/// Android MediaCodec emits AVCC-style NAL units:
///     [4-byte BE length][NAL bytes] repeated
/// FFmpeg expects Annex-B:
///     [00 00 00 01][NAL bytes] repeated
/// This converts one to the other.
/// </summary>
private static byte[] AvccToAnnexB(byte[] avcc)
{
    var outp = new List<byte>(avcc.Length + 16);
    int p = 0;

    while (p + 4 <= avcc.Length)
    {
        int nalLen = BinaryPrimitives.ReadInt32BigEndian(
            avcc.AsSpan(p, 4));
        p += 4;

        if (nalLen <= 0 || p + nalLen > avcc.Length)
            break;

        // Annex-B start code
        outp.Add(0x00);
        outp.Add(0x00);
        outp.Add(0x00);
        outp.Add(0x01);

        // NAL payload
        for (int i = 0; i < nalLen; i++)
            outp.Add(avcc[p + i]);

        p += nalLen;
    }

    return outp.ToArray();
}
    
    
    
    private void ReceiveVideoToFile(int durationSeconds = 15, string outputFile = "tc_stream.h264")
{
    _dataClient.ReceiveTimeout = 500;

    var buf = new MemoryStream();
    int parsedPos = 0;
    var start = DateTime.UtcNow;
    long h264Total = 0;
    int auCount = 0;

    using (var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
    {
        while ((DateTime.UtcNow - start).TotalSeconds < durationSeconds && _running)
        {
            try
            {
                byte[] tmp = new byte[65536];
                int read = _dataStream.Read(tmp, 0, tmp.Length);
                if (read == 0) break;
                buf.Write(tmp, 0, read);
            }
            catch (IOException) { /* receive timeout -- just loop */ }

            byte[] raw = buf.GetBuffer();
            int avail = (int)buf.Length;

            while (parsedPos + 52 <= avail)
            {
                if (BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(parsedPos)) != 0x07000000)
                {
                    parsedPos++;
                    continue;
                }

                uint f1   = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(parsedPos + 4));
                uint f2   = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(parsedPos + 8));
                uint s0   = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(parsedPos + 36));
                uint s1   = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(parsedPos + 40));
                uint size = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(parsedPos + 44));

                if ((f1 & 0xFFFF0000) != 0x00060000 || f2 != 256 || s0 != 0 || s1 != 32)
                {
                    parsedPos++;
                    continue;
                }

                int payloadStart = parsedPos + 52;
                if (payloadStart + (long)size > avail)
                    break; // incomplete AU -- wait for more data

                if (size > 0) // skip the 0-byte announce
                {
                    fs.Write(raw, payloadStart, (int)size);
                    h264Total += size;
                    auCount++;
                }

                parsedPos = payloadStart + (int)size;
            }
        }
    }

    Console.WriteLine($"Captured {buf.Length} raw bytes");
    Console.WriteLine($"Parsed {auCount} access units, wrote {h264Total} H.264 bytes to {outputFile}");
    Console.WriteLine($"Play/convert with: ffplay -f h264 -i {outputFile}");
    Console.WriteLine($"                or: ffmpeg -f h264 -i {outputFile} -c copy out.mp4");
}
    
    
    //
    // private void ReceiveVideo(int durationSeconds = 15)
    // {
    //     _dataClient.ReceiveTimeout = 500;
    //
    //     var buf = new MemoryStream();
    //     var start = DateTime.UtcNow;
    //     while ((DateTime.UtcNow - start).TotalSeconds < durationSeconds && _running)
    //     {
    //         try
    //         {
    //             byte[] tmp = new byte[65536];
    //             int read = _dataStream.Read(tmp, 0, tmp.Length);
    //             if (read == 0) break;
    //             // My_window.directX.PresentFrame();
    //             buf.Write(tmp, 0, read);
    //         }
    //         catch (IOException) { }
    //     }
    //
    //     byte[] raw = buf.ToArray();
    //     Console.WriteLine($"Captured {raw.Length} raw bytes");
    //
    //     int pos = 0, count = 0;
    //     long total = 0;
    //
    //     while (pos + 52 <= raw.Length)
    //     {
    //         // Marker check + recovery
    //         if (BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(pos)) != 0x07000000)
    //         {
    //             pos++;
    //             continue;
    //         }
    //
    //         uint f1   = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(pos + 4));
    //         uint f2   = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(pos + 8));
    //         uint s0   = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(pos + 36));
    //         uint s1   = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(pos + 40));
    //         uint size = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(pos + 44));
    //
    //         if ((f1 & 0xFFFF0000) != 0x00060000 || f2 != 256 || s0 != 0 || s1 != 32)
    //         {
    //             pos++;
    //             continue;
    //         }
    //
    //         if (pos + 52 + size > raw.Length) break;
    //
    //         count++;
    //         total += size;
    //         Console.WriteLine($"AU #{count}: {size} bytes");
    //         pos += 52 + (int)size;
    //     }
    //
    //     Console.WriteLine($"Parsed {count} access units, {total} total H.264 bytes");
    //     Console.WriteLine($"{raw.Length - pos} trailing bytes left over");
    // }
    //
    
    
    
    
}

















