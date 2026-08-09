using System.Globalization;
using System.Xml;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb;

public static class XdbFloat
{
    // The editor keeps at most this many significant digits.
    private const int SignificantDigits = 6;
    private const int PositiveMantissaWidth = SignificantDigits + 1;
    private const string ExponentFormat = "0.00000e+00";

    public static string ToXdbString(float value)
    {
        var text = XmlConvert.ToString(value);
        var exponentIndex = text.IndexOf('E');
        if (exponentIndex > 0)
            return TrimExponentialMantissa(text, exponentIndex, value);

        var decimalIndex = text.IndexOf('.');
        var integerEnd = decimalIndex < 0 ? text.Length : decimalIndex;
        var firstSignificantIndex = FirstSignificantIndex(text);

        // More integer digits than we keep significant: only exponent form can render
        // the value at 6 significant digits (rounding to negative decimals is impossible).
        if (integerEnd - firstSignificantIndex > SignificantDigits)
            return value.ToString(ExponentFormat, CultureInfo.InvariantCulture);

        return XmlConvert.ToString(RoundToSignificantDigits(value, decimalIndex, firstSignificantIndex));
    }
    
    private static string TrimExponentialMantissa(string text, int exponentIndex, float value)
    {
        var mantissaWidth = value >= 0 ? PositiveMantissaWidth : PositiveMantissaWidth + 1;
        var endIndex = Math.Min(mantissaWidth, exponentIndex);
        return text[..endIndex] + text[exponentIndex..];
    }
    
    private static int FirstSignificantIndex(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is '0' or '-' or '.') continue;
            return i;
        }

        return text.Length;
    }

    private static double RoundToSignificantDigits(float value, int decimalIndex, int firstSignificantIndex)
    {
        return decimalIndex > firstSignificantIndex
            ? Math.Round(value, SignificantDigits - (decimalIndex - firstSignificantIndex))
            : Math.Round(value, SignificantDigits + (firstSignificantIndex - decimalIndex - 1));
    }
}
