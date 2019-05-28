using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Plugin.SimpleAudioPlayer;
using StabilometricApp.Models;
using Xamarin.Forms;

namespace StabilometricApp.ViewModels {

    public class RecordingViewModel : BaseViewModel {

        StreamWriter _writer = null;

        ISimpleAudioPlayer _beepSecondaryPlayer;
        ISimpleAudioPlayer _beepPrimaryPlayer;
        ISimpleAudioPlayer _beepFinalPlayer;

        private const int TimerFrequencyHz = 100;
        private const int TimerIntervalMs = (1000 / TimerFrequencyHz);
        private readonly Timer _timer;
        private readonly object _writerLock = new object();

        public RecordingViewModel() {
            IsRecording = false;

            _timer = new Timer(TimerTick, null, Timeout.Infinite, Timeout.Infinite);

            StartRecording = new Command(async () => await StartRecordingPerform());

            Task.Run(() => InitializeAudioPlayers());
        }

        private DateTime _lastTickTimestamp = DateTime.MaxValue;
        private const int ExpectedTimerIntervalMs = (1000 / 100);
        private const int ToleratedTimerIntervalMs = (int)(ExpectedTimerIntervalMs * ToleranceIntervalMultiplier);
        private const double ToleranceIntervalMultiplier = 1.7;

        private DateTime _targetTimestamp = DateTime.MinValue;

        private int _secondCounter = 0;

        private void TimerTick(object v) {
            var elapsed = DateTime.UtcNow - _lastTickTimestamp;
            if(elapsed.Ticks >= 0) {
                if(elapsed.TotalMilliseconds > ToleratedTimerIntervalMs) {
                    // System.Diagnostics.Debug.WriteLine("Cannot keep up");
                }
            }
            _lastTickTimestamp = DateTime.UtcNow;

            lock(_writerLock) {
                _writer.Write(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0},{1:F3},{2:F3},{3:F3},{4:F3},{5:F3},{6:F3},{7:F3},{8:F3},{9:F3}",
                    DateTime.UtcNow.Ticks,
                    App.LastAccelerometerReading.X, App.LastAccelerometerReading.Y, App.LastAccelerometerReading.Z,
                    App.LastGravityReading.X, App.LastGravityReading.Y, App.LastGravityReading.Z,
                    App.LastGyroscopeReading.X, App.LastGyroscopeReading.Y, App.LastGyroscopeReading.Z
                ));

                _writer.WriteLine();
            }

            // Recount elapsed time approx. every half-second
            if(_secondCounter-- < 0) {
                var remaining = _targetTimestamp.Subtract(DateTime.UtcNow).TotalSeconds;
                Counter = (int)Math.Ceiling(remaining);

                if(remaining <= 0) {
                    Task.Run(() => { StopRecordingPerform().ConfigureAwait(false); });
                }

                _secondCounter = TimerFrequencyHz / 2;
            }
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

            MessagingCenter.Send(this, "MC", new SimpleMessage(SimpleMessage.MessageType.START));

            string filename = "stabilo-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv";
            string filepath = Path.Combine(App.GetExternalRootPath(), filename);
            _writer = new StreamWriter(new FileStream(filepath, FileMode.CreateNew));

            await _writer.WriteLineAsync(string.Format("# Start time (local): {0:G}", DateTime.Now));
            await _writer.WriteLineAsync(string.Format("# Track name: {0}", TrackName));
            await _writer.WriteLineAsync(string.Format("# Height (cm): {0}", Height));
            await _writer.WriteLineAsync("Ticks, AccX, AccY, AccZ, GravX, GravY, GravZ, GyroX, GyroY, GyroZ");

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

            _secondCounter = 0;
            _targetTimestamp = DateTime.UtcNow.Add(TimeSpan.FromSeconds(30));

            _timer.Change(TimerIntervalMs, TimerIntervalMs);
        }

        private async Task StopRecordingPerform() {
            if(!IsRecording) {
                return;
            }

            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            _beepFinalPlayer.Play();

            MessagingCenter.Send(this, "MC", new SimpleMessage(SimpleMessage.MessageType.STOP));

            await _writer.FlushAsync();
            lock(_writerLock) {
                _writer.Dispose();
                _writer = null;
            }

            IsRecording = false;
        }

        private void InitializeSecondaryBeepPlayer() {
            _beepSecondaryPlayer = CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
            Stream beepStream = GetType().Assembly.GetManifestResourceStream("StabilometricApp.Beep-2.mp3");
            _beepSecondaryPlayer.Load(beepStream);
        }

        private void InitializePrimaryBeepPlayer() {
            _beepPrimaryPlayer = CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
            Stream beepStream = GetType().Assembly.GetManifestResourceStream("StabilometricApp.Beep-1.mp3");
            _beepPrimaryPlayer.Load(beepStream);
        }

        private void InitializeFinalBeepPlayer() {
            _beepFinalPlayer = CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
            Stream beepStream = GetType().Assembly.GetManifestResourceStream("StabilometricApp.Beep-final.mp3");
            _beepFinalPlayer.Load(beepStream);
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

        string _trackName;
        public string TrackName {
            get {
                return _trackName;
            }
            set {
                SetProperty(ref _trackName, value);
            }
        }

        int _height = 180;
        public string Height {
            get {
                return _height.ToString();
            }
            set {
                if(!int.TryParse(value, out int height)) {
                    return;
                }

                SetProperty(ref _height, height);
            }
        }

    }

}
