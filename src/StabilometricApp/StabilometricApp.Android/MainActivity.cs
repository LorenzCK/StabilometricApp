using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using static Android.OS.PowerManager;
using Android.Hardware;
using Android.Util;
using Xamarin.Forms;
using StabilometricApp.ViewModels;
using StabilometricApp.Models;
using System.IO;
using System.Globalization;

namespace StabilometricApp.Droid {
    [Activity(Label = "StabilometricApp", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity {

        WakeLock _wakeLock;
        private SensorManager _sensorManager;
        private SensorListener _sensorListener;
        protected StreamWriter Writer { get; set; }
        private int[] _readingCount;

        public MainActivity() {
            App.GetExternalRootPath = () => {
                return this.GetExternalFilesDir(null).AbsolutePath;
            };
        }

        protected override void OnCreate(Bundle savedInstanceState) {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            MessagingCenter.Subscribe<RecordingViewModel, SimpleMessage>(this, "MC", async (sender, msg) => {

                switch(msg.Type) {
                    case SimpleMessage.MessageType.START:
                        Log.Debug(this.LocalClassName, "Start Recording");
                        await InitializeRecordingAsync(sender);
                        RegisterSensorListeners();
                        break;
                    case SimpleMessage.MessageType.STOP:
                        Log.Debug(this.LocalClassName, "Stop Recording");
                        UnregisterSensorListeners();
                        await StopRecording();
                        break;
                }

            });
            _sensorManager = GetSystemService(SensorService) as Android.Hardware.SensorManager;

            global::Xamarin.Forms.Forms.SetFlags("Shell_Experimental", "Visual_Experimental", "CollectionView_Experimental", "FastRenderers_Experimental");
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
        }



        private async System.Threading.Tasks.Task InitializeRecordingAsync(RecordingViewModel sender) {
            string filename = "stabilo-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv";
            string filepath = Path.Combine(App.GetExternalRootPath(), filename);
            Writer = new StreamWriter(new FileStream(filepath, FileMode.CreateNew));
            _readingCount = new int[] { 0, 0, 0, 0 };

            await Writer.WriteLineAsync(string.Format("# Start time (local): {0:G}", DateTime.Now));
            await Writer.WriteLineAsync(string.Format("# Track name: {0}", sender.TrackName));
            await Writer.WriteLineAsync(string.Format("# Height (cm): {0}", sender.Height));
            await Writer.WriteLineAsync("# a: accelerometer");
            await Writer.WriteLineAsync("# g: gyroscope");
            await Writer.WriteLineAsync("# G: gravity");
            await Writer.WriteLineAsync("# l: linear acceleration");
            await Writer.WriteLineAsync("a/g/G/l, Ticks, Timestamp, AccX, AccY, AccZ ");

            await Writer.FlushAsync();
        }

        private async System.Threading.Tasks.Task StopRecording() {
            await Writer.FlushAsync();
            Writer.Dispose();
            Writer = null;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults) {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void RegisterSensorListeners() {
            _sensorListener = new SensorListener(this);

            var accelerometerSensor = _sensorManager.GetDefaultSensor(Android.Hardware.SensorType.Accelerometer);
            _sensorManager.RegisterListener(_sensorListener, accelerometerSensor, Android.Hardware.SensorDelay.Fastest);

            var gyroscopeSensor = _sensorManager.GetDefaultSensor(Android.Hardware.SensorType.Gyroscope);
            _sensorManager.RegisterListener(_sensorListener, gyroscopeSensor, Android.Hardware.SensorDelay.Fastest);

            var gravitySensor = _sensorManager.GetDefaultSensor(Android.Hardware.SensorType.Gravity);
            _sensorManager.RegisterListener(_sensorListener, gravitySensor, Android.Hardware.SensorDelay.Fastest);

            var linearAccSensor = _sensorManager.GetDefaultSensor(Android.Hardware.SensorType.LinearAcceleration);
            _sensorManager.RegisterListener(_sensorListener, linearAccSensor, Android.Hardware.SensorDelay.Fastest);
        }

        private void UnregisterSensorListeners() {
            if(_sensorListener != null) {
                _sensorManager.UnregisterListener(_sensorListener);
            }
        }

        protected override void OnStart() {
            base.OnStart();

            if(_wakeLock is null) {
                var context = this.ApplicationContext;
                PowerManager powerManager = (PowerManager)context.GetSystemService(PowerService);
                _wakeLock = powerManager.NewWakeLock(WakeLockFlags.ScreenDim, "whatever");
                _wakeLock.Acquire();
            }

        }

        protected override void OnStop() {
            base.OnStop();

            if(_wakeLock != null) {
                _wakeLock.Release();
                _wakeLock = null;
            }

        }


        // ISensorEventListener
        class SensorListener : Java.Lang.Object, ISensorEventListener {

            private MainActivity _parent;

            public SensorListener(Activity context) : base() {
                _parent = (MainActivity)context;
            }

            public void OnAccuracyChanged(Android.Hardware.Sensor sensor, Android.Hardware.SensorStatus accuracy) {
            }

            public void OnSensorChanged(Android.Hardware.SensorEvent e) {
                float x = e.Values[0];
                float y = e.Values[1];
                float z = e.Values[2];

                var now = DateTime.Now;
                var mark = "";

                switch(e.Sensor.Type) {
                    case Android.Hardware.SensorType.Accelerometer:
                        mark = "a";
                        break;
                    case Android.Hardware.SensorType.Gyroscope:
                        mark = "g";
                        break;
                    case Android.Hardware.SensorType.Gravity:
                        mark = "G";
                        break;
                    case Android.Hardware.SensorType.LinearAcceleration:
                        mark = "l";
                        break;
                }

                //Log.Debug(_parent.LocalClassName, "{0}, {1}, {2}, {3}, {4}, {5}, ", mark, now.Ticks, now.ToString("O"), x, y, z);

                if(_parent.Writer != null) {
                    _parent.Writer.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}, {1}, {2}, {3}, {4}, {5}",
                        mark,
                        now.Ticks,
                        now.ToString("O"),
                        x, y, z
                    ));
                }
            }
        }
    }

}
