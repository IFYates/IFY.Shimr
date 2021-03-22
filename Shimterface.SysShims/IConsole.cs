using Shimterface.SysShims.IO;
using System;
using System.IO;

namespace Shimterface.SysShims
{
    /// <summary>
    /// Static shim of <see cref="Console"/>.
    /// </summary>
    [StaticShim(typeof(Console))]
    public interface IConsole
    {
        bool IsInputRedirected { get; }
        int BufferHeight { get; set; }
        int BufferWidth { get; set; }
        bool CapsLock { get; }
        int CursorLeft { get; set; }
        int CursorSize { get; set; }
        int CursorTop { get; set; }
        bool CursorVisible { get; set; }
        ITextWriter Error { get; }
        ConsoleColor ForegroundColor { get; set; }
        ITextReader In { get; }
        IEncoding InputEncoding { get; set; }
        bool IsErrorRedirected { get; }
        int WindowWidth { get; set; }
        bool IsOutputRedirected { get; }
        bool KeyAvailable { get; }
        int LargestWindowHeight { get; }
        int LargestWindowWidth { get; }
        bool NumberLock { get; }
        ITextWriter Out { get; }
        IEncoding OutputEncoding { get; set; }
        string Title { get; set; }
        bool TreatControlCAsInput { get; set; }
        int WindowHeight { get; set; }
        int WindowLeft { get; set; }
        int WindowTop { get; set; }
        ConsoleColor BackgroundColor { get; set; }

        event ConsoleCancelEventHandler CancelKeyPress;

        void Beep();
        void Beep(int frequency, int duration);
        void Clear();
        void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop);
        void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, ConsoleColor sourceForeColor, ConsoleColor sourceBackColor);
        IStream OpenStandardError(int bufferSize);
        IStream OpenStandardError();
        IStream OpenStandardInput(int bufferSize);
        IStream OpenStandardInput();
        IStream OpenStandardOutput(int bufferSize);
        IStream OpenStandardOutput();
        int Read();
        ConsoleKeyInfo ReadKey(bool intercept);
        ConsoleKeyInfo ReadKey();
        string ReadLine();
        void ResetColor();
        void SetBufferSize(int width, int height);
        void SetCursorPosition(int left, int top);
        void SetError([TypeShim(typeof(TextWriter))] ITextWriter newError);
        void SetIn([TypeShim(typeof(TextReader))] ITextReader newIn);
        void SetOut([TypeShim(typeof(TextWriter))] ITextWriter newOut);
        void SetWindowPosition(int left, int top);
        void SetWindowSize(int width, int height);
        void Write(ulong value);
        void Write(bool value);
        void Write(char value);
        void Write(char[] buffer);
        void Write(char[] buffer, int index, int count);
        void Write(double value);
        void Write(long value);
        void Write(object value);
        void Write(float value);
        void Write(string value);
        void Write(string format, object arg0);
        void Write(string format, object arg0, object arg1);
        void Write(string format, object arg0, object arg1, object arg2);
        void Write(string format, params object[] arg);
        void Write(uint value);
        void Write(decimal value);
        void Write(int value);
        void WriteLine(ulong value);
        void WriteLine(string format, params object[] arg);
        void WriteLine();
        void WriteLine(bool value);
        void WriteLine(char[] buffer);
        void WriteLine(char[] buffer, int index, int count);
        void WriteLine(decimal value);
        void WriteLine(double value);
        void WriteLine(uint value);
        void WriteLine(int value);
        void WriteLine(object value);
        void WriteLine(float value);
        void WriteLine(string value);
        void WriteLine(string format, object arg0);
        void WriteLine(string format, object arg0, object arg1);
        void WriteLine(string format, object arg0, object arg1, object arg2);
        void WriteLine(long value);
        void WriteLine(char value);
    }
}
