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
        private WaveInEvent _waveSource;
        private Channel<byte[]> _channel;
        private CancellationTokenSource _streamCancelToken;
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
            _hubConnection.On("ListStreams", (IEnumerable<string> users) =>
            {
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }
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
                _waveSource.WaveFormat = new WaveFormat(44100, 1);
                _waveSource.DataAvailable += async (s, e) =>
                {
                    await _channel.Writer.WriteAsync(e.Buffer);
                };

                _waveSource.StartRecording();

            }
            else
            {
                _waveSource.StopRecording();
                _channel.Writer.Complete();


                BTN_Connect.Content = "Start Recording";
            }
        }

        private async void BTN_Listen_Click(object sender, RoutedEventArgs e)
        {
            if (BTN_Listen.Content.ToString().Contains("Start"))
            {
                var stream = LB_Users.SelectedItem?.ToString();
                if (stream != null)
                {
                    BTN_Listen.Content = "Stop Listening";
                    try
                    {
                        await ListenToStream(stream);
                    }
                    catch (OperationCanceledException)
                    {

                    }
                }
            }
            else
            {
                BTN_Listen.Content = "Start Listening";
                _streamCancelToken.Cancel();
            }


        }

        public async Task ListenToStream(string streamName)
        {

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

            var buffer = new BufferedWaveProvider(new WaveFormat(44100, 1))
            {
                BufferDuration = TimeSpan.FromSeconds(10),
                DiscardOnBufferOverflow = true
            };
            var waveOut = new WaveOut();
            waveOut.Init(buffer);
            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;

            try
            {
                waveOut.Play();
                await GetStreamAudio(buffer, streamName);
                //await Task.WhenAll(GetStreamAudio(buffer, streamName), PlayAudio(buffer, waveOut));
            }
            finally
            {
                BTN_Listen.Content = "Start Listening";
                buffer.ClearBuffer();
                waveOut.Dispose();
            }


        }

        private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private async Task GetStreamAudio(BufferedWaveProvider buffer, string streamName)
        {
            _streamCancelToken = new CancellationTokenSource();
            var channel = await _hubConnection.StreamAsChannelAsync<byte[]>(
                "ListenToStream", streamName, _streamCancelToken.Token);


            while (await channel.WaitToReadAsync())
            {
                while (channel.TryRead(out var soundStream))
                {
                    try
                    {
                        buffer.AddSamples(soundStream, 0, soundStream.Length);
                    }
                    catch (Exception ex)
                    {
                        var x = ex;
                    }
                }
            }
        }

    }
}
