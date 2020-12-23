using Microsoft.AspNetCore.SignalR.Client;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Speaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HubConnection _hubConnection;
        private WaveFileWriter _waveFile;
        private WaveInEvent _waveSource;
        public ObservableCollection<string> Users { get; set; } = new ObservableCollection<string>();
        public MainWindow()
        {
            InitializeComponent();
            LB_Users.ItemsSource = Users;
            //InitConnection();
        }

        private async void BTN_Connect_Click(object sender, RoutedEventArgs e)
        {
            bool isRecoding = false;

            if (BTN_Connect.Content.ToString() == "Start Recording")
            {
                isRecoding = true;
            }
            await ToggleRecord(isRecoding);
        }

        private async Task InitConnection()
        {
            _hubConnection = new HubConnectionBuilder()
               .WithUrl("http://localhost:65322/hubs/stream")
               .Build();

            await _hubConnection.StartAsync();

            InitMethods();
        }

        private void InitMethods()
        {
            _hubConnection.On("NewStream", (string user) =>
            {
                Users.Add(user);
            });
            _hubConnection.On("RemoveStream", (string user) =>
            {
                Users.Remove(user);
            });

        }

        private async Task ToggleRecord(bool isRecording)
        {
            if (isRecording)
            {
                BTN_Connect.Content = "Stop Recording";

                _waveSource = new WaveInEvent();
                string tempFile = (@"C:\Users\r.zuluaga\Desktop\Test\test1.wav");
                _waveSource.WaveFormat = new WaveFormat(44100, 1);
                _waveSource.DataAvailable += WaveSource_DataAvailable;

                _waveFile = new WaveFileWriter(tempFile, _waveSource.WaveFormat);
                _waveSource.StartRecording();


                //await _hubConnection.InvokeAsync("StartStream", TB_Name.Text.ToString());
            }
            else
            {
                _waveSource.StopRecording();
                _waveFile.Dispose();


                BTN_Connect.Content = "Start Recording";
            }
        }

        private void WaveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            _waveFile.Write(e.Buffer, 0, e.BytesRecorded);
        }

        private void BTN_Listen_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
