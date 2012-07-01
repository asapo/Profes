using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using Profes.BinaryEditorBase;
using Profes.P2P.FileShare.ServiceModel;
using Profes.P2P.FileShare.Properties;
using Profes.Security.Cryptography;

namespace Profes.P2P.FileShare.FileShareControl
{
    /// <summary>
    /// DateTimeをStringに変換します
    /// </summary>
    [ValueConversion(typeof(DateTime), typeof(string))]
    public class DateTimeToStringConverter : IValueConverter
    {
        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            DateTime date = (DateTime)value;
            return date.ToString("yyyy/MM/dd HH:mm:ss");
        }

        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            string strValue = value.ToString();
            DateTime resultDateTime;
            if (DateTime.TryParse(strValue, out resultDateTime))
            {
                return resultDateTime;
            }
            return value;
        }
    }

    /// <summary>
    /// BytesをStringに変換する
    /// </summary>
    [ValueConversion(typeof(byte[]), typeof(string))]
    public class BytesToStringConverter : IValueConverter
    {
        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            return BinaryEditor.BytesToHexString((byte[])value).ToLower();
        }

        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            return BinaryEditor.HexStringToBytes(value.ToString());
        }
    }

    /// <summary>
    /// byte配列をStringに変換する
    /// </summary>
    [ValueConversion(typeof(byte[]), typeof(string))]
    public class Base64ToStringConverter : IValueConverter
    {
        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            return System.Convert.ToBase64String((byte[])value);
        }

        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            return System.Convert.FromBase64String(value.ToString());
        }
    }

    /// <summary>
    /// long(Size)をStringに変換する
    /// </summary>
    [ValueConversion(typeof(long), typeof(string))]
    public class Long_Size_ToStringConverter : IValueConverter
    {
        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            var v = value as long?;
            if (v == null) return null;

            if (Settings.Default.DisplaySize_1 == true)
            {
                return v.ToString();
            }
            if (Settings.Default.DisplaySize_2 == true)
            {
                return String.Format("{0:#,0}", v);
            }
            if (Settings.Default.DisplaySize_3 == true)
            {
                return convertBytes((double)v);
            }

            return null;
        }

        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        static string convertBytes(double b)
        {
            int i = 0;
            string f = (b < 0) ? "-" : "";
            List<string> u = new List<string> { "Byte", "KB", "MB", "GB", "TB", "PB" };

            b = Math.Abs(b);

            while (b >= 1024 && (b / 1024) >= 1)
            {
                b /= 1024;
                i++;
            }

            return f + Math.Round(b, 2).ToString().Trim() + u[i];
        }
    }

    /// <summary>
    /// NodeをStringに変換する
    /// </summary>
    [ValueConversion(typeof(Node), typeof(string))]
    public class NodeToStringConverter : IValueConverter
    {
        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            return ((Node)value).Endpoint.Uri.DnsSafeHost;
        }

        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// CategoryListをStringに変換する
    /// </summary>
    [ValueConversion(typeof(string[]), typeof(string))]
    public class CategoryListToStringConverter : IValueConverter
    {
        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            StringBuilder sb = new StringBuilder();

            foreach (string ss in (string[])value)
            {
                if (ss != "")
                    sb.Append("\"" + ss + "\",");
            }

            return sb.ToString().TrimEnd(',');
        }

        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            return value.ToString().Split(',').Select(n => n.Substring(1, n.Length - 1)).ToArray();
        }
    }

    /// <summary>
    /// StringをStringに変換する（PublicKeyのハッシュのBase64を取得する）
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class PublicKey_StringToBase64_StringConverter : IValueConverter
    {
        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            return System.Convert.ToBase64String(HashFunction.HashCreate((string)value));
        }

        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ReviewをStringに変換する
    /// </summary>
    [ValueConversion(typeof(Review), typeof(string))]
    public class ReviewToStringConverter : IValueConverter
    {
        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            return ((Review)value).ToString();
        }

        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ReviewをBrushに変換する
    /// </summary>
    [ValueConversion(typeof(Review), typeof(System.Windows.Media.Brush))]
    public class ReviewToBrushConverter : IValueConverter
    {
        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            switch ((Review)value)
            {
                case Review.良い:
                    return System.Windows.Media.Brushes.Blue;
                case Review.悪い:
                    return System.Windows.Media.Brushes.Red;
                default:
                    return System.Windows.Media.Brushes.Black;
            }
        }

        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// BoolをStringに変換する
    /// </summary>
    [ValueConversion(typeof(bool), typeof(string))]
    public class BoolToStringConverter : IValueConverter
    {
        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            return (((bool)value) == true) ? "＋" : "－";
        }

        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// RefineSearchStringをboolに変換する
    /// </summary>
    [ValueConversion(typeof(RefineSearchString), typeof(bool))]
    public class RefineSearchStringToBoolConverter : IValueConverter
    {
        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            return ((RefineSearchString)value).Include;
        }

        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// RefineSearchStringをStringに変換する
    /// </summary>
    [ValueConversion(typeof(RefineSearchString), typeof(string))]
    public class RefineSearchStringToStringConverter : IValueConverter
    {
        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            return ((RefineSearchString)value).Value;
        }

        /// <summary>
        /// 値を変換します。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}