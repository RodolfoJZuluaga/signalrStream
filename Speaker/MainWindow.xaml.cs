using Microsoft.AspNetCore.SignalR.Client;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Channels;
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
        private Channel<byte[]> _channel;
        public ObservableCollection<string> Users { get; set; } = new ObservableCollection<string>();
        public MainWindow()
        {
            InitializeComponent();
            LB_Users.ItemsSource = Users;
            InitConnection();
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
               .WithUrl("http://localhost:64747/stream")
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
                _channel = Channel.CreateBounded<byte[]>(2);
                await _hubConnection.SendAsync("StartStream", TB_Name.Text, _channel.Reader);

                _waveSource = new WaveInEvent();
                //string tempFile = (@"C:\Users\r.zuluaga\Desktop\Test\test1.wav");
                _waveSource.WaveFormat = new WaveFormat(44100, 1);
                _waveSource.DataAvailable += async (s, e) =>
                {
                    //_waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                    await _channel.Writer.WriteAsync(e.Buffer);
                };

                //_waveFile = new WaveFileWriter(tempFile, _waveSource.WaveFormat);
                _waveSource.StartRecording();

            }
            else
            {
                _waveSource.StopRecording();
                //_waveFile.Dispose();
                _channel.Writer.Complete();
                

                BTN_Connect.Content = "Start Recording";
            }
        }

        private void WaveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            _waveFile.Write(e.Buffer, 0, e.BytesRecorded);
        }

        private async void BTN_Listen_Click(object sender, RoutedEventArgs e)
        {
            var stream = LB_Users.SelectedItem?.ToString();
            if(stream != null)
            {
                await WatchStream(stream);
            }
        }

        public async Task WatchStream(string streamName)
        {
            //using (var ms = new MemoryStream())
            //{
            //    var cancellationTokenSource = new CancellationTokenSource();
            //    var channel = await _hubConnection.StreamAsChannelAsync<byte[]>(
            //        "WatchStream", streamName, cancellationTokenSource.Token);

            //    //var w = new WaveOut();
            //    //IWaveProvider provider = new RawSourceWaveStream(ms, new WaveFormat(44100, 1));

            //    //w.Init(provider);


            //    while (await channel.WaitToReadAsync())
            //    {
            //        while (channel.TryRead(out var soundStream))
            //        {
            //            ms.Write(soundStream, 0 , soundStream.Length);
            //        }
            //    }
            //    ms.Position = 0;

            //    var w = new WaveOut();
            //    IWaveProvider provider = new RawSourceWaveStream(
            //             new MemoryStream(ms.ToArray()), new WaveFormat(44100, 1));

            //    w.Init(provider);
            //    w.Play();
            //}

            //var cancellationTokenSource = new CancellationTokenSource();
            //var channel = await _hubConnection.StreamAsChannelAsync<byte[]>(
            //    "WatchStream", streamName, cancellationTokenSource.Token);

            //string tempFile = (@"C:\Users\rzla8\Documents\test\test1.wav");
            //var w = new WaveFileWriter(tempFile, new WaveFormat(44100, 1));
            //while (await channel.WaitToReadAsync())
            //{
            //    while (channel.TryRead(out var soundStream))
            //    {
            //        w.Write(soundStream, 0, soundStream.Length);
            //    }
            //}

            //w.Dispose();

            var waveFormat = new WaveFormat(44100, 1);
            var buffer = new BufferedWaveProvider(waveFormat);


            new Thread(async () =>
            {
                var cancellationTokenSource = new CancellationTokenSource();
                var channel = await _hubConnection.StreamAsChannelAsync<byte[]>(
                    "WatchStream", streamName, cancellationTokenSource.Token);


                while (await channel.WaitToReadAsync())
                {
                    while (channel.TryRead(out var soundStream))
                    {
                        try
                        {
                            buffer.AddSamples(soundStream, 0, soundStream.Length);
                        }
                        catch(Exception ex)
                        {
                            var x = ex;
                        }
                    }
                }

                Thread.ResetAbort();
            }).Start();

            var w = new WaveOut();
            w.Init(buffer);
            w.PlaybackStopped += W_PlaybackStopped;
            w.Play();
        }

        private void W_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
