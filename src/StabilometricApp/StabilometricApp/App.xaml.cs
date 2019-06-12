using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using StabilometricApp.Views;
using System.Numerics;
using StabilometricApp.ViewModels;

namespace StabilometricApp {

    public partial class App : Application {

        public App() {
            InitializeComponent();

            MainPage = new AppShell();
        }

        protected override void OnStart() {
            // Handle when your app starts
        }

        protected override void OnSleep() {
            // Handle when your app sleeps
        }

        protected override void OnResume() {
            // Handle when your app resumes
        }

        public static Func<string> GetExternalRootPath { get; set; }

        public static Action OpenExternalRootPath { get; set; }

        public static Vector3 LastAccelerometerReading,
                              LastGravityReading,
                              LastGyroscopeReading;

    }

}
