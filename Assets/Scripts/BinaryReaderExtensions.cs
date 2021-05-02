using System.IO;
using System.Text;
using System.Runtime.InteropServices;

public static class BinaryReaderExtensions
{
    public static string ReadASCIIString(this BinaryReader reader, int length)
    {
        var chars = reader.ReadChars(length);
        return new string(chars);
    }

    public static string ReadASCIIStringAtPos(this BinaryReader reader, long pos)
    {
        var prevPos = reader.BaseStream.Position;

        if (pos >= reader.BaseStream.Length)
            throw new System.Exception("ReadASCIIStringAtPos: Position exceeds length: " + pos + " " + reader.BaseStream.Length);

        reader.BaseStream.Position = pos;
        var str = ReadNullTerminatedString(reader);
        reader.BaseStream.Position = prevPos;
        return str;
    }

    public static string ReadNullTerminatedString(this BinaryReader reader)
    {
        var sb = new StringBuilder();
        char ch;
        while ((int)(ch = reader.ReadChar()) != 0)
            sb.Append(ch);
        return sb.ToString();
    }

    public static T ReadStruct<T>(this BinaryReader reader) where T : struct
    {
        byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        var retStruct = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        handle.Free();

        return retStruct;
    }
}
