﻿using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Plugin.SimpleAudioPlayer;
using StabilometricApp.Messages;
using Xamarin.Essentials;
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

        private Reading[] _collector;
        private int _collectorIndex = 0;

        public RecordingViewModel() {
            IsRecording = false;

            _timer = new Timer(TimerTick, null, Timeout.Infinite, Timeout.Infinite);

            StartRecording = new Command(async () => await StartRecordingPerform());
            StopRecording = new Command(async () => await StopRecordingPerform(true));

            Task.Run(() => InitializeAudioPlayers());

            // Restore values from settings
            var t = GetType();
            foreach(var propertyName in new string[] {
                nameof(DurationSelectionIndex),
                nameof(TrackNotes),
                nameof(PersonHeight),
                nameof(PersonWeight),
                nameof(PersonName),
                nameof(SexSelectionIndex)
            }) {
                var property = t.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                if(property != null) {
                    var propertyValueKey = nameof(RecordingViewModel) + propertyName;
                    if(Preferences.ContainsKey(propertyValueKey)) {
                        var setter = property.GetSetMethod();
                        if(property.PropertyType == typeof(int)) {
                            setter.Invoke(this, new object[] { Preferences.Get(propertyValueKey, 0) });
                        }
                        else {
                            setter.Invoke(this, new object[] { Preferences.Get(propertyValueKey, string.Empty) });
                        }
                    }
                }
            }
        }

        private DateTime _targetTimestamp = DateTime.MinValue;
        private DateTime _updateTimestamp = DateTime.MinValue;
        private bool _completed = false;

        private void TimerTick(object v) {
            if(_completed || _collectorIndex >= _collector.Length) {
                return;
            }

            _collector[_collectorIndex++] = new Reading {
                Ticks = DateTime.UtcNow.Ticks,
                AccX = App.LastAccelerometerReading.X,
                AccY = App.LastAccelerometerReading.Y,
                AccZ = App.LastAccelerometerReading.Z,
                GravX = App.LastGravityReading.X,
                GravY = App.LastGravityReading.Y,
                GravZ = App.LastGravityReading.Z,
                GyroX = App.LastGyroscopeReading.X,
                GyroY = App.LastGyroscopeReading.Y,
                GyroZ = App.LastGyroscopeReading.Z
            };

            if(DateTime.UtcNow >= _targetTimestamp) {
                _completed = true;
                Counter = 0;
                Task.Run(() => { StopRecordingPerform().ConfigureAwait(false); });
            }
            else if(DateTime.UtcNow >= _updateTimestamp) {
                var remaining = Math.Ceiling(_targetTimestamp.Subtract(DateTime.UtcNow).TotalSeconds);
                Counter = (int)remaining;

                _updateTimestamp = _targetTimestamp.Subtract(TimeSpan.FromSeconds(remaining - 1));
                System.Diagnostics.Debug.WriteLine(string.Format("Updating, {0}s remain, next update at {1}", remaining, _updateTimestamp));
            }
        }

        private void InitializeAudioPlayers() {
            InitializeSecondaryBeepPlayer();
            InitializePrimaryBeepPlayer();
            InitializeFinalBeepPlayer();
        }

        public ICommand StartRecording { get; }

        public ICommand StopRecording { get; }

        private async Task StartRecordingPerform() {
            if(IsRecording) {
                return;
            }
            var personName = PersonName.ToFilenamePart();
            if(string.IsNullOrEmpty(personName)) {
                MessagingCenter.Send<BaseViewModel, AlertMessage>(this, "Alert", new AlertMessage("Please set user name"));
                return;
            }

            string filename = "stabilo-" + personName.ToLowerInvariant() + "-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv";
            string filepath = Path.Combine(App.GetExternalRootPath(), filename);
            _writer = new StreamWriter(new FileStream(filepath, FileMode.CreateNew));

            await _writer.WriteLineAsync(string.Format("# Start time (local): {0:G}", DateTime.Now));
            await _writer.WriteLineAsync(string.Format("# Subject name: {0}", PersonName));
            await _writer.WriteLineAsync(string.Format("# Sex: {0}", SexList[SexSelectionIndex]));
            await _writer.WriteLineAsync(string.Format("# Height (cm): {0}", PersonHeight));
            await _writer.WriteLineAsync(string.Format("# Weight (kg): {0}", PersonWeight));
            await _writer.WriteLineAsync(string.Format("# Track notes: {0}", TrackNotes));
            await _writer.WriteLineAsync(string.Format("# Device information: {0}", App.GetDeviceInformation.Invoke()));
            await _writer.WriteLineAsync("Ticks, AccX, AccY, AccZ, GravX, GravY, GravZ, GyroX, GyroY, GyroZ");

            IsRecording = true;
            int countdownDuration = Settings.CountdownDuration;
            int recordingDuration = Duration;

            while(countdownDuration > 0) {
                _beepSecondaryPlayer.Play();
                Counter = countdownDuration;
                await Task.Delay(1000);

                countdownDuration--;
            }

            _beepPrimaryPlayer.Play();
            Counter = recordingDuration;

            _completed = false;
            _targetTimestamp = DateTime.UtcNow.Add(TimeSpan.FromSeconds(recordingDuration));
            _updateTimestamp = DateTime.UtcNow.Add(TimeSpan.FromSeconds(1));

            _collectorIndex = 0;
            _collector = new Reading[(int)(TimerFrequencyHz * recordingDuration * 1.2)];
            System.Diagnostics.Debug.WriteLine(string.Format("Allocated buffer of {0} elements for {1} seconds", _collector.Length, recordingDuration));

            _timer.Change(TimerIntervalMs, TimerIntervalMs);
        }

        private async Task StopRecordingPerform(bool dropFile = false) {
            if(!IsRecording) {
                return;
            }

            Debug.WriteLine(string.Format("Stopping, {0} readings collected", _collectorIndex));
            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            if(dropFile) {
                var filepath = ((FileStream)_writer.BaseStream).Name; // 🤢

                _writer.Dispose();
                _writer = null;

                File.Delete(filepath);
                Debug.WriteLine(string.Format("File {0} deleted", filepath));
            }
            else {
                _beepFinalPlayer.Play();

                Debug.WriteLine("Dumping data to file");

                await _writer.FlushAsync();
                await Task.Run(() => {
                    for(int i = 0; i < _collectorIndex; ++i) {
                        _writer.Write(string.Format(
                            CultureInfo.InvariantCulture,
                            "{0},{1:F3},{2:F3},{3:F3},{4:F3},{5:F3},{6:F3},{7:F3},{8:F3},{9:F3}",
                            _collector[i].Ticks,
                            _collector[i].AccX, _collector[i].AccY, _collector[i].AccZ,
                            _collector[i].GravX, _collector[i].GravY, _collector[i].GravZ,
                            _collector[i].GyroX, _collector[i].GyroY, _collector[i].GyroZ
                        ));

                        _writer.WriteLine();
                    }
                    _writer.Flush();
                });
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
        public string TrackNotes {
            get {
                return _trackName;
            }
            set {
                SetProperty(ref _trackName, value);
                Preferences.Set(nameof(RecordingViewModel) + nameof(TrackNotes), value);
            }
        }

        int _personHeight = 180;
        public string PersonHeight {
            get {
                return _personHeight.ToString();
            }
            set {
                if(!int.TryParse(value, out int height)) {
                    return;
                }

                SetProperty(ref _personHeight, height);
                Preferences.Set(nameof(RecordingViewModel) + nameof(PersonHeight), value);
            }
        }

        int _personWeight = 80;
        public string PersonWeight {
            get {
                return _personWeight.ToString();
            }
            set {
                if(!int.TryParse(value, out int weight)) {
                    return;
                }

                SetProperty(ref _personWeight, weight);
                Preferences.Set(nameof(RecordingViewModel) + nameof(PersonWeight), value);
            }
        }

        string _personName;
        public string PersonName {
            get {
                return _personName;
            }
            set {
                SetProperty(ref _personName, value);
                Preferences.Set(nameof(RecordingViewModel) + nameof(PersonName), value);
            }
        }

        public IList SexList { get; private set; } = new string[] {
            "Male",
            "Female"
        };

        private int _sexSelectionIndex = 0;
        public int SexSelectionIndex {
            get {
                return _sexSelectionIndex;
            }
            set {
                if(value < 0 || value >= SexList.Count) {
                    return;
                }

                SetProperty(ref _sexSelectionIndex, value);
                Preferences.Set(nameof(RecordingViewModel) + nameof(SexSelectionIndex), value);
            }
        }

        public IList DurationList { get; private set; } = new int[] {
            10, 15, 20, 30
        };

        private int _durationSelectionIndex = 0;
        public int DurationSelectionIndex {
            get {
                return _durationSelectionIndex;
            }
            set {
                if(value < 0 || value >= DurationList.Count) {
                    return;
                }

                SetProperty(ref _durationSelectionIndex, value);
                Preferences.Set(nameof(RecordingViewModel) + nameof(DurationSelectionIndex), value);
            }
        }

        public int Duration => (int)DurationList[DurationSelectionIndex];

    }

}
