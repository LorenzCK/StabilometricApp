using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace StabilometricApp.ViewModels {

    public class RecordingViewModel : BaseViewModel {

        StreamWriter _writer = null;
        int _readingCount = 0;

        public RecordingViewModel() {
            IsRecording = false;
            StartRecording = new Command(async () => await StartRecordingPerform());
        }

        public ICommand StartRecording { get; }

        private async Task StartRecordingPerform() {
            if(IsRecording) {
                return;
            }

            string filename = "stabilo-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv";
            string filepath = Path.Combine(App.GetExternalRootPath(), filename);
            _writer = new StreamWriter(new FileStream(filepath, FileMode.CreateNew));
            _readingCount = 0;

            await _writer.WriteLineAsync("Ticks, Timestamp, AccX, AccY, AccZ, ");

            Counter = 3;
            IsRecording = true;
            await Task.Delay(1000);
            Counter = 2;
            await Task.Delay(1000);
            Counter = 1;
            await Task.Delay(1000);
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
