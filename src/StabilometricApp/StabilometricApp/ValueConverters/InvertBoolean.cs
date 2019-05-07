using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace StabilometricApp.ValueConverters {
    class InvertBoolean : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return !((bool)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return !((bool)value);
        }
    }
}
