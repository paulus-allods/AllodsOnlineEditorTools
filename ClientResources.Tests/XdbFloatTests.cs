using AllodsOnlineEditorTools.ClientResources.Serialization.Xdb;

namespace ClientResources.Tests;

/// <summary>
/// Validates <see cref="XdbFloat.ToXdbString"/>: 6-significant-digit formatting with trailing zeros
/// trimmed, exponent form for very small values, and exact reproduction of authored editor strings.
/// </summary>
[TestFixture]
public class XdbFloatTests
{
    [TestCase(0f, "0")]
    [TestCase(2f, "2")]
    [TestCase(1.5f, "1.5")]
    [TestCase(-1.5f, "-1.5")]
    [TestCase(0.5f, "0.5")]
    public void ToXdbString_FormatsSimpleValues(float value, string expected)
    {
        Assert.That(XdbFloat.ToXdbString(value), Is.EqualTo(expected));
    }

    [Test]
    public void ToXdbString_RoundsToSixSignificantDigits()
    {
        // 1/3 has more precision than the xdb format keeps; it must be trimmed, not full precision.
        var result = XdbFloat.ToXdbString(1f / 3f);
        Assert.That(result, Does.StartWith("0.33333"));
        Assert.That(result, Has.Length.LessThanOrEqualTo("0.333333".Length));
    }

    [Test]
    public void ToXdbString_KeepsExponentForVerySmallValues()
    {
        Assert.That(XdbFloat.ToXdbString(1e-10f), Does.Contain("E"));
    }

    [TestCase(3872858.2f, "3.87286e+06")]
    [TestCase(-3872858.2f, "-3.87286e+06")]
    [TestCase(1234567f, "1.23457e+06")]
    public void ToXdbString_UsesExponentWhenIntegerPartExceedsSixDigits(float value, string expected)
    {
        // Values >= 1e6 cannot be rounded to 6 significant digits with a decimal point;
        // they must render in exponent form instead of throwing.
        Assert.That(XdbFloat.ToXdbString(value), Is.EqualTo(expected));
    }
    
    [TestCase("3.14172")]
    [TestCase("0.548078")]
    [TestCase("0.0317405")]
    [TestCase("0.000234626")]
    [TestCase("0.000106286")]
    [TestCase("0.00018049")]
    [TestCase("0.00039161")]
    public void ToXdbString_MatchesAuthoredEditorFormat(string authored)
    {
        var value = float.Parse(authored, System.Globalization.CultureInfo.InvariantCulture);
        Assert.That(XdbFloat.ToXdbString(value), Is.EqualTo(authored));
    }
}
