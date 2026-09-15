using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Androidplayer_wpf.Src;
using Androidplayer_wpf.Src.Keymap.K_store;
using SharpDX.Direct3D11;

namespace Androidplayer_wpf.Native_test
{
    public partial class My_window : Window
    {

        // string fileToPlay = @"I:\Hour Timer_1080p.mp4";
        // string fileToPlay = @"C:\Users\miki\Downloads\Compressed\scrcpy-win64-v1.20\demo.mp4";
        string fileToPlay = @"M:\movie\Kung.Fu.Panda.3.2016.720p.WEBRip.x264.AAC-ETRG.mp4";

        Src.FFmpeg ffmpeg; // FFmpeg Video Demuxing & HW Decoding
        public DirectX directX; // DirectX Video Processing & Rendering
        // public static DirectX directX { get; set; }
        Thread threadPlay; // Simulates FPS  

        private del_scrcpy my_scrcpy;
        
        private del_scrcpy_2 my_scrcpy_2;
        private del_scrcpy_3 my_scrcpy_3;
        
        private del_link_4 my_link_4;
        
        private Tc_test my_tc_test;

        private bool is_running = true;

        public My_window()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
            Closing += OnClosing;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            my_scrcpy_3?.Stop();
            my_scrcpy_2?.Stop();
            
            my_link_4?.Stop();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            is_running = false;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            NativePanel.HandleCreated += OnHandleCreated;

        }

        private void OnHandleCreated(IntPtr hwnd)
        {
            string path = @"I:\movie\Kung.Fu.Panda.3.2016.720p.WEBRip.x264.AAC-ETRG.mp4";


            Console.WriteLine("HWND = " + hwnd);
            
            
            StartMemoryMonitor();
            
            
            
            k_info.Instance.directx = new DirectX(hwnd);
            
            
           my_tc_test = new Tc_test();
            my_tc_test.Start();
           
           
           
           
            
            // my_scrcpy = new del_scrcpy(hwnd);
            //
            // my_scrcpy.Start();

            // my_scrcpy_2 = new del_scrcpy_2(hwnd);
            //
            //
            // my_scrcpy_2.Start();
            
            
            
            
            
            
            // my_scrcpy_3 = new del_scrcpy_3(hwnd);
            //
            // my_scrcpy_3.Start();
            //
            
            
            
            
            //
            // my_link_4 = new del_link_4(hwnd);
            //
            // my_link_4.Start();
            //
            
            
            
            
            
            
            // directX = new DirectX(hwnd);
            //
            // directX.DisplayImage("dev_img1.jpg");
            //
            //
            
            
            //      try
            // {
            //     ffmpeg = new Src.FFmpeg();
            //     directX = new DirectX(hwnd);
            //
            //     if (!ffmpeg.InitHWAccel(directX._device)) 
            //     { 
            //         MessageBox.Show("Failed to Initialize FFmpeg's HW Acceleration"); 
            //         return; 
            //     }
            //
            //     if (!ffmpeg.Open(fileToPlay)) 
            //     { 
            //         MessageBox.Show("FFmpeg failed to open input"); 
            //         return; 
            //     }
            //
            //     threadPlay = new Thread(() =>
            //     {
            //         try
            //         {
            //             Stopwatch sw = new Stopwatch();
            //             while (is_running)
            //             {
            //                 
            //                 sw.Restart();
            //                 // FFmpeg HW Decode Frame
            //                 Texture2D textureHW = ffmpeg.GetFrame();
            //                 if (textureHW == null) 
            //                 { 
            //                     Console.WriteLine("Empty Texture!"); 
            //                     continue; 
            //                 }
            //
            //                 // DirectX HW Process & Present Frame
            //                 // directX.PresentFrame(textureHW);
            //                 
            //                 try
            //                 {
            //                     directX.PresentFrame(textureHW);
            //                 }
            //                 finally
            //                 {
            //                     textureHW.Dispose();
            //                 }
            //                 
            //                 
            //                 sw.Stop(); // Stop measuring after presenting
            //                 
            //                 // 3️⃣ Print time in milliseconds
            //                 // Console.WriteLine($"Frame time (decode + render): {sw.Elapsed.TotalMilliseconds:F2} ms");
            //                 
            //
            //                 Thread.Sleep(16); // Simulates FPS
            //             }
            //         }
            //         catch (Exception ex)
            //         {
            //             Console.WriteLine($"Thread error: {ex.Message}");
            //             Console.WriteLine($"Stack trace: {ex.StackTrace}");
            //         }
            //     });
            //
            //     threadPlay.SetApartmentState(ApartmentState.STA);
            //     threadPlay.Start();
            // }
            // catch (Exception ex)
            // {
            //     Console.WriteLine($"Initialization error: {ex.Message}");
            //     Console.WriteLine($"Stack trace: {ex.StackTrace}");
            //     // MessageBox.Show($"Failed to initialize: {ex.Message}");
            // }
            //
            //      





            /////////////////////////////////////////////////         
        }

       
        
        
 

        private static void StartMemoryMonitor()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    using Process process = Process.GetCurrentProcess();

                    long memoryMB = process.PrivateMemorySize64 / 1024 / 1024;

                    // Console.WriteLine($"Memory: {memoryMB} MB");

                    if (memoryMB > 400)
                    {
                        
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Application.Current.Shutdown();
                        });
                        
                        Environment.FailFast(
                            $"Memory limit exceeded: {memoryMB} MB");
                    }

                    await Task.Delay(1000);
                }
            });
        }
    }
}