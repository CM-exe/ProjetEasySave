using EasySave.Model;
using System;
using System.Globalization;
using System.Windows.Data;

namespace EasySave.Converters;

[ValueConversion(typeof(string), typeof(string))]
class EntryHandlerToStringConverter : IValueConverter {
    private static string FormatSize(double size) {
        if (size < 1024) {
            return size + " B";
        } else if (size < 1024 * 1024) {
            return (size / 1024.0).ToString("F2") + " KB";
        } else if (size < 1024 * 1024 * 1024) {
            return (size / (1024.0 * 1024)).ToString("F2") + " MB";
        } else {
            return (size / (1024.0 * 1024 * 1024)).ToString("F2") + " GB";
        }
    }
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is IEntryHandler entryHandler) {
            if (entryHandler.Exists()) {
                try {
                    return entryHandler.GetName() + " (" + FormatSize(entryHandler.GetSize()) + ")";
                } catch {
                    return entryHandler.GetName();
                }
            } else {
                return entryHandler.GetName();
            }
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}