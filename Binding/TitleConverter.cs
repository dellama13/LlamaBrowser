using System;
using System.Globalization;
using System.Windows.Data;
namespace LlamaBrowser.Binding
{
    public class TitleConverter : IValueConverter
    {


        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            return "LlamaBrowserDemo - " + (value ?? "No Title Specified");
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Windows.Data.Binding.DoNothing;
        }




    }
}
