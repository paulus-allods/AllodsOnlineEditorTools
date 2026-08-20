namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Database;

/// <summary>Adler-32 checksum (RFC 1950): two 16-bit sums accumulated modulo the largest prime below 2^16.</summary>
internal static class Adler32
{
    private const uint ModAdler = 65521;

    public static uint Compute(ReadOnlySpan<byte> data)
    {
        uint a = 1, b = 0;
        foreach (var value in data)
        {
            a = (a + value) % ModAdler;
            b = (b + a) % ModAdler;
        }

        return (b << 16) | a;
    }
}
