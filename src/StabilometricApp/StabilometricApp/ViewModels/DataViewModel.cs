using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace StabilometricApp.ViewModels {

    public class DataViewModel : BaseViewModel {

        public DataViewModel() {
            Title = "Data";

            OpenDataDirectory = new Command(() => {
                App.OpenExternalRootPath();
            });
        }

        public ICommand OpenDataDirectory { get; private set; }

    }

}
