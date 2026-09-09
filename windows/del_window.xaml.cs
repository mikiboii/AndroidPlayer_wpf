using System.Windows;
using Androidplayer_wpf.Src;

namespace Androidplayer_wpf.windows;

public partial class del_window : Window
{
    
    private Adb_worker_test my_adb_worker;
    public del_window()
    {
        InitializeComponent();


       
        
        this.Loaded += OnLoaded;
        
       
        
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Console.WriteLine($"current DeviceIP is : {UISettings.Instance.DeviceIP}");
        Console.WriteLine($"current mode is : {UISettings.Instance.SelectedConnectionType}");
        
        
       
        
        
        
        
        my_adb_worker = new Adb_worker_test();
        // my_adb_worker.ProgressChanged += my_app_worker_ProgressChanged;
        // my_adb_worker.CountingCompleted += My_adb_workerOnCountingCompleted;
        // my_adb_worker.devicedisconnected += My_adb_workerOndevicedisconnected;

        // my_adb_worker.ErrorOccurred += general_error;

        my_adb_worker.Start();
        
            // UISettings.Instance.SelectedConnectionType = "USB";
            UISettings.Instance.SelectedConnectionType = "Wireless";
            UISettings.Instance.Save();
        
        
        
    }
}