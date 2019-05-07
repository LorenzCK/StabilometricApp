using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Plugin.SimpleAudioPlayer;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace StabilometricApp.ViewModels {

    public class RecordingViewModel : BaseViewModel {

        StreamWriter _writer = null;
        int _readingCount = 0;
        ISimpleAudioPlayer _beepSecondaryPlayer;
        ISimpleAudioPlayer _beepPrimaryPlayer;
        ISimpleAudioPlayer _beepFinalPlayer;

        public RecordingViewModel() {
            IsRecording = false;
            
            
            StartRecording = new Command(async () => await StartRecordingPerform());
        }

        private void InitializeAudioPlayers() {
            InitializeSecondaryBeepPlayer();
            InitializePrimaryBeepPlayer();
            InitializeFinalBeepPlayer();
        }

        public ICommand StartRecording { get; }

        private async Task StartRecordingPerform() {
            if(IsRecording) {
                return;
            }

            InitializeAudioPlayers();

            string filename = "stabilo-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv";
            string filepath = Path.Combine(App.GetExternalRootPath(), filename);
            _writer = new StreamWriter(new FileStream(filepath, FileMode.CreateNew));
            _readingCount = 0;

            await _writer.WriteLineAsync("Ticks, Timestamp, AccX, AccY, AccZ, ");

            _beepSecondaryPlayer.Play();
            Counter = 3;
            IsRecording = true;
            await Task.Delay(1000);

            _beepSecondaryPlayer.Play();
            Counter = 2;
            await Task.Delay(1000);

            _beepSecondaryPlayer.Play();
            Counter = 1;
            await Task.Delay(1000);

            _beepPrimaryPlayer.Play();
            Counter = 0;
            

            Accelerometer.Start(SensorSpeed.Fastest);
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;

            Counter = 60;
            while(Counter > 0) {
                await Task.Delay(1000);
                Counter -= 1;
            }

            Accelerometer.ReadingChanged -= Accelerometer_ReadingChanged;
            Accelerometer.Stop();

            await _writer.FlushAsync();
            _writer.Dispose();
            _writer = null;

            // Stop 
            _beepFinalPlayer.Play();

            IsRecording = false;
        }

        private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e) {
            var data = e.Reading.Acceleration;
            var now = DateTime.Now;

            _writer.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0}, {1}, {2}, {3}, {4}, ",
                now.Ticks,
                now.ToString("O"),
                data.X, data.Y, data.Z
            ));

            Debug.WriteLine("Reading {0}", ++_readingCount);
        }

        private void InitializeSecondaryBeepPlayer() {
            _beepSecondaryPlayer = CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
            Stream beepStream = GetType().Assembly.GetManifestResourceStream("StabilometricApp.Beep-2.mp3");
            bool isSuccess = _beepSecondaryPlayer.Load(beepStream);
        }
        private void InitializePrimaryBeepPlayer() {
            _beepPrimaryPlayer = CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
            Stream beepStream = GetType().Assembly.GetManifestResourceStream("StabilometricApp.Beep-1.mp3");
            bool isSuccess = _beepPrimaryPlayer.Load(beepStream);
        }
        private void InitializeFinalBeepPlayer() {
            _beepFinalPlayer = CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
            Stream beepStream = GetType().Assembly.GetManifestResourceStream("StabilometricApp.Beep-final.mp3");
            bool isSuccess = _beepFinalPlayer.Load(beepStream);
        }

        bool _isRecording = false;
        public bool IsRecording {
            get {
                return _isRecording;
            }
            private set {
                SetProperty(ref _isRecording, value);
            }
        }

        int _counter;
        public int Counter {
            get {
                return _counter;
            }
            private set {
                SetProperty(ref _counter, value);
            }
        }

    }

}
