using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Android.Content;
using Android.Hardware;

namespace StabilometricApp.Droid {

    class SensorListener : Java.Lang.Object, ISensorEventListener {

        private readonly MainActivity _owner;

        public SensorListener(MainActivity activity) : base() {
            _owner = activity;
        }

        public void Register() {
            var sensorManager = (SensorManager)_owner.GetSystemService(Context.SensorService);

            var accelerometerSensor = sensorManager.GetDefaultSensor(SensorType.Accelerometer);
            sensorManager.RegisterListener(this, accelerometerSensor, SensorDelay.Fastest);

            var gyroscopeSensor = sensorManager.GetDefaultSensor(SensorType.Gyroscope);
            sensorManager.RegisterListener(this, gyroscopeSensor, SensorDelay.Fastest);

            var gravitySensor = sensorManager.GetDefaultSensor(SensorType.Gravity);
            sensorManager.RegisterListener(this, gravitySensor, SensorDelay.Fastest);
        }

        public void Unregister() {
            var sensorManager = (SensorManager)_owner.GetSystemService(Context.SensorService);
            sensorManager.UnregisterListener(this);
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy) {

        }

        public void OnSensorChanged(SensorEvent e) {
            float x = e.Values[0];
            float y = e.Values[1];
            float z = e.Values[2];

            switch(e.Sensor.Type) {
                case SensorType.Accelerometer:
                    App.LastAccelerometerReading = new Vector3(x, y, z);
                    break;
                case SensorType.Gyroscope:
                    App.LastGyroscopeReading = new Vector3(x, y, z);
                    break;
                case SensorType.Gravity:
                    App.LastGravityReading = new Vector3(x, y, z);
                    break;
#if DEBUG
                default:
                    throw new ArgumentException("Unforeseen sensor reading");
#endif
            }
        }

    }

}
