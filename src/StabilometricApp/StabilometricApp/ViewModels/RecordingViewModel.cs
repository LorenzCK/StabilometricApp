using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Plugin.SimpleAudioPlayer;
using StabilometricApp.Models;
using Xamarin.Forms;


namespace StabilometricApp.ViewModels {

    public class RecordingViewModel : BaseViewModel {

        //StreamWriter _writer = null;
        //int[] _readingCount = { 0, 0, 0, 0 };
        ISimpleAudioPlayer _beepSecondaryPlayer;
        ISimpleAudioPlayer _beepPrimaryPlayer;
        ISimpleAudioPlayer _beepFinalPlayer;

        public RecordingViewModel() {
            IsRecording = false;
            
            StartRecording = new Command(async () => await StartRecordingPerform());

            Task.Run(() => InitializeAudioPlayers());
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

            _startSensors();

            Counter = 30;
            while(Counter > 0) {
                await Task.Delay(1000);
                Counter -= 1;
            }

            _stopSensors();

            
            // Stop 
            _beepFinalPlayer.Play();

            IsRecording = false;
        }

        private void _startSensors() {

            MessagingCenter.Send<RecordingViewModel, SimpleMessage>(this, "MC", new SimpleMessage(SimpleMessage.MessageType.START));
        }

        private void _stopSensors() {

            MessagingCenter.Send<RecordingViewModel, SimpleMessage>(this, "MC", new SimpleMessage(SimpleMessage.MessageType.STOP));

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
