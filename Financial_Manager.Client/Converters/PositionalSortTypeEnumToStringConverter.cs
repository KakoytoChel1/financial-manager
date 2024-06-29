using AccountingTool;
using Microsoft.UI.Xaml.Data;
using System;

namespace Financial_Manager.Client.Converters
{
    public class PositionalSortTypeEnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is SortingTool.PositionalSortingType enumValue)
            {
                return enumValue.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string stringValue && Enum.TryParse(typeof(SortingTool.PositionalSortingType), stringValue, out var result))
            {
                return result;
            }
            return SortingTool.PositionalSortingType.ByDate;
        }
    }
}
