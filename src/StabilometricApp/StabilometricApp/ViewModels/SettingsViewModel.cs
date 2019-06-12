using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Essentials;

namespace StabilometricApp.ViewModels {

    public class SettingsViewModel : BaseViewModel {

        int _countdownDuration = Settings.CountdownDuration;

        public string CountdownDuration {
            get {
                return _countdownDuration.ToString();
            }
            set {
                if(!int.TryParse(value, out int duration)) {
                    return;
                }
                if(duration <= 0) {
                    return;
                }

                SetProperty(ref _countdownDuration, duration);
                Settings.CountdownDuration = duration;
            }
        }

    }

}
