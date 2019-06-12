using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Essentials;

namespace StabilometricApp {

    public static class Settings {

        private const string CountdownDurationKey = "Settings." + nameof(CountdownDuration);

        public static int CountdownDuration {
            get {
                return Preferences.Get(CountdownDurationKey, 5);
            }
            set {
                if(value <= 0) {
                    throw new ArgumentOutOfRangeException();
                }

                Preferences.Set(CountdownDurationKey, value);
            }
        }

    }

}
