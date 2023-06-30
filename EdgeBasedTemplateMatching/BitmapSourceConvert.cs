using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace EdgeBasedTemplateMatching
{


    internal class BitmapSourceConvert : IValueConverter
    {
        /// <summary>
        /// mat to bitmapsource
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Mat mat)
            {
                return mat.ToBitmapSource();
            }
            return null;
        }

        /// <summary>
        /// bitmapsource to mat
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BitmapSource bitmap)
            {
                return bitmap.ToMat();
            }
            return null;
        }


    }
}