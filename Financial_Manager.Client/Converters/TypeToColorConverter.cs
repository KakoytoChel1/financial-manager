using AccountingTool;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Financial_Manager.Client.Converters
{
    public class TypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is OperationType type)
            {
                switch (type)
                {
                    case OperationType.Income:
                        return new SolidColorBrush(Colors.ForestGreen);
                    case OperationType.Expense:
                        return new SolidColorBrush(ColorHelper.FromArgb(255, 212, 6, 6));
                    default:
                        return new SolidColorBrush(Colors.Gray);
                }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
