using System;
using System.Text;

namespace Shimterface.SysShims.IO
{
    /// <summary>
    /// Shim of <see cref="Encoding"/>.
    /// </summary>
    public interface IEncoding : ICloneable
    {
        string BodyName { get; }
        int CodePage { get; }
        DecoderFallback DecoderFallback { get; set; }
        EncoderFallback EncoderFallback { get; set; }
        string EncodingName { get; }
        bool IsMailNewsDisplay { get; }
        bool IsBrowserDisplay { get; }
        bool IsBrowserSave { get; }
        bool IsMailNewsSave { get; }
        bool IsReadOnly { get; }
        bool IsSingleByte { get; }
        ReadOnlySpan<byte> Preamble { get; }
        string HeaderName { get; }
        string WebName { get; }
        int WindowsCodePage { get; }

        bool Equals(object value);
        int GetByteCount(string s);
        int GetByteCount(char[] chars);
        int GetByteCount(char[] chars, int index, int count);
        int GetByteCount(ReadOnlySpan<char> chars);
        int GetByteCount(string s, int index, int count);
        byte[] GetBytes(string s);
        byte[] GetBytes(char[] chars, int index, int count);
        int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex);
        byte[] GetBytes(char[] chars);
        byte[] GetBytes(string s, int index, int count);
        int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes);
        int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex);
        int GetCharCount(ReadOnlySpan<byte> bytes);
        int GetCharCount(byte[] bytes);
        int GetCharCount(byte[] bytes, int index, int count);
        char[] GetChars(byte[] bytes, int index, int count);
        char[] GetChars(byte[] bytes);
        int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex);
        int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars);
        Decoder GetDecoder();
        Encoder GetEncoder();
        int GetHashCode();
        int GetMaxByteCount(int charCount);
        int GetMaxCharCount(int byteCount);
        byte[] GetPreamble();
        string GetString(ReadOnlySpan<byte> bytes);
        string GetString(byte[] bytes, int index, int count);
        string GetString(byte[] bytes);
        bool IsAlwaysNormalized(NormalizationForm form);
        bool IsAlwaysNormalized();
    }
}
