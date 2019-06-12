using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using StabilometricApp.Models;
using StabilometricApp.ViewModels;
using Xamarin.Forms;

namespace StabilometricApp.Droid {

    [Activity(
        Label = "StabilometricApp",
        Icon = "@mipmap/icon",
        Theme = "@style/MainTheme",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation
    )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity {

        private PowerManager.WakeLock _wakeLock;
        private SensorManager _sensorManager;
        private readonly SensorListener _sensorListener;
        protected StreamWriter Writer { get; set; }

        public MainActivity() {
            _sensorListener = new SensorListener(this);

            App.GetExternalRootPath = () => {
                return GetExternalFilesDir(null).AbsolutePath;
            };
            App.OpenExternalRootPath = () => {
                var targetUri = global::Android.Net.Uri.Parse(App.GetExternalRootPath());
                var i = new Intent(Intent.ActionView);
                i.SetDataAndType(targetUri, "resource/folder");

                if(i.ResolveActivityInfo(ApplicationContext.PackageManager, 0) != null) {
                    StartActivity(i);
                }
                else {
                    Toast.MakeText(this, "File manager not installed", ToastLength.Long).Show();
                }
            };
        }

        protected override void OnCreate(Bundle savedInstanceState) {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            MessagingCenter.Subscribe<RecordingViewModel, SimpleMessage>(this, "MC", (sender, msg) => {
                switch(msg.Type) {
                    case SimpleMessage.MessageType.START:
                        Log.Debug(LocalClassName, "Start Recording (ignored)");
                        break;
                    case SimpleMessage.MessageType.STOP:
                        Log.Debug(LocalClassName, "Stop Recording (ignored)");
                        break;
                }
            });
            _sensorManager = GetSystemService(SensorService) as Android.Hardware.SensorManager;

            Forms.SetFlags("Shell_Experimental", "Visual_Experimental", "CollectionView_Experimental", "FastRenderers_Experimental");
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            Forms.Init(this, savedInstanceState);
            LoadApplication(new App());

            Log.Debug(LocalClassName, "Start Recording");
            _sensorListener.Register();
        }

        protected override void OnDestroy() {
            base.OnDestroy();

            Log.Debug(LocalClassName, "Stop Recording");
            _sensorListener.Unregister();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults) {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnStart() {
            base.OnStart();

            if(_wakeLock == null) {
                var context = ApplicationContext;
                PowerManager powerManager = (PowerManager)context.GetSystemService(PowerService);
                _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "whatever");
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

    }

}
