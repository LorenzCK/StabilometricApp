using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using static Android.OS.PowerManager;

namespace StabilometricApp.Droid {
    [Activity(Label = "StabilometricApp", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity {

        WakeLock _wakeLock;

        public MainActivity() {
            App.GetExternalRootPath = () => {
                return this.GetExternalFilesDir(null).AbsolutePath;
            };
        }

        protected override void OnCreate(Bundle savedInstanceState) {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            global::Xamarin.Forms.Forms.SetFlags("Shell_Experimental", "Visual_Experimental", "CollectionView_Experimental", "FastRenderers_Experimental");
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults) {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }


        protected override void OnStart() {
            base.OnStart();

            if(_wakeLock is null) {
                var context = this.ApplicationContext;
                PowerManager powerManager = (PowerManager)context.GetSystemService("power");
                _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Full, "whatever");
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
