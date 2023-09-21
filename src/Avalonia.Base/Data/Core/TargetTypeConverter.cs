﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Data.Converters;
using System.Windows.Input;
using Avalonia.Utilities;

namespace Avalonia.Data.Core;

internal abstract class TargetTypeConverter
{
    public static TargetTypeConverter GetDefaultConverter() => new DefaultConverter();

    [RequiresUnreferencedCode(TrimmingMessages.TypeConversionRequiresUnreferencedCodeMessage)]
    public static TargetTypeConverter GetReflectionConverter() => new ReflectionConverter();

    public abstract bool TryConvert(object? value, Type type, CultureInfo culture, out object? result);

    private class DefaultConverter : TargetTypeConverter
    {
        public override bool TryConvert(object? value, Type type, CultureInfo culture, out object? result)
        {
            var t = Nullable.GetUnderlyingType(type) ?? type;

            if (value is null)
            {
                result = null;
                return !t.IsValueType || t != type;
            }

            if (value == AvaloniaProperty.UnsetValue)
            {
                // Here the behavior is different from the ReflectionConverter: there isn't any way
                // to create the default value for a type without using reflection, so we have to report
                // that we can't convert the value.
                result = null;
                return false;
            }

            if (t.IsAssignableFrom(value.GetType()))
            {
                result = value;
                return true;
            }

            if (t == typeof(string))
            {
                result = value.ToString();
                return true;
            }

            if (value is IConvertible convertible)
            {
                try
                {
                    result = convertible.ToType(t, culture);
                    return true;
                }
                catch
                {
                    result = null;
                    return false;
                }
            }

            result = null;
            return false;
        }
    }

    [RequiresUnreferencedCode(TrimmingMessages.TypeConversionRequiresUnreferencedCodeMessage)]
    private class ReflectionConverter : TargetTypeConverter
    {
        public override bool TryConvert(object? value, Type type, CultureInfo culture, out object? result)
        {
            if (value == AvaloniaProperty.UnsetValue)
            {
                result = Activator.CreateInstance(type);
                return true;
            }
            else if (typeof(ICommand).IsAssignableFrom(type) && 
                value is Delegate d &&
                !d.Method.IsPrivate &&
                d.Method.GetParameters().Length <= 1)
            {
                result = new MethodToCommandConverter(d);
                return true;
            }
            else
            {
                return TypeUtilities.TryConvert(type, value, culture, out result);
            }
        }
    }
}
