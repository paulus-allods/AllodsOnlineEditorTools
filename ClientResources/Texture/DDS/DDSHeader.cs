using System.Text;

namespace AllodsOnlineEditorTools.ClientResources.Texture.DDS;

public class DDSHeader
{
    public uint Size { get; set; } = 124;
    public DDSFlags Flags { get; set; }
    public uint Height { get; set; }
    public uint Width { get; set; }
    public uint PitchOrLinearSize { get; set; }
    public uint Depth { get; set; }
    public uint MipMapCount { get; set; }
    public byte[] Reserved1 { get; set; } = new byte[44];
    public DDSPixelFormat PixelFormat { get; set; } = new DDSPixelFormat();
    public DDSCaps Caps { get; set; }
    public DDSCaps2 Caps2 { get; set; }
    public uint Caps3 { get; set; }
    public uint Caps4 { get; set; }
    public uint Reserved2 { get; set; }
    
    public void SetCustomMetadata(string appName, string version, string compression)
    {
        using (var ms = new MemoryStream(Reserved1))
        using (var bw = new BinaryWriter(ms))
        {
            var appBytes = Encoding.ASCII.GetBytes(appName.PadRight(16, '\0'));
            bw.Write(appBytes, 0, 16);

            var verBytes = Encoding.ASCII.GetBytes(version.PadRight(16, '\0'));
            bw.Write(verBytes, 0, 16);
            
            var compBytes = Encoding.ASCII.GetBytes(compression.PadRight(12, '\0'));
            bw.Write(compBytes, 0, 12);
        }
    }
    
    public (string appName, string version, string compression) GetCustomMetadata()
    {
        var appName = Encoding.ASCII.GetString(Reserved1, 0, 16).TrimEnd('\0');
        var version = Encoding.ASCII.GetString(Reserved1, 16, 16).TrimEnd('\0');
        var compression = Encoding.ASCII.GetString(Reserved1, 32, 12).TrimEnd('\0');
        return (appName, version, compression);
    }
}