using Shimterface.SysShims.Threading;
using Shimterface.SysShims.Threading.Tasks;
using System;
using System.IO;
using System.Threading;

namespace Shimterface.SysShims.IO
{
    /// <summary>
    /// Shim of <see cref="Stream"/>.
    /// </summary>
    public interface IStream
    {
        long Position { get; set; }
        long Length { get; }
        bool CanWrite { get; }
        bool CanTimeout { get; }
        bool CanSeek { get; }
        bool CanRead { get; }
        int ReadTimeout { get; set; }
        int WriteTimeout { get; set; }

        IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state);
        IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state);
        void Close();
        void CopyTo([TypeShim(typeof(Stream))] IStream destination, int bufferSize);
        void CopyTo([TypeShim(typeof(Stream))] IStream destination);
        ITask CopyToAsync([TypeShim(typeof(Stream))] IStream destination, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken);
        ITask CopyToAsync([TypeShim(typeof(Stream))] IStream destination);
        ITask CopyToAsync([TypeShim(typeof(Stream))] IStream destination, int bufferSize);
        ITask CopyToAsync([TypeShim(typeof(Stream))] IStream destination, int bufferSize, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken);
        void Dispose();
        IValueTask DisposeAsync();
        int EndRead(IAsyncResult asyncResult);
        void EndWrite(IAsyncResult asyncResult);
        void Flush();
        ITask FlushAsync([TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken);
        ITask FlushAsync();
        int Read(byte[] buffer, int offset, int count);
        int Read(Span<byte> buffer);
        IValueTask<int> ReadAsync(Memory<byte> buffer, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken = default);
        ITask<int> ReadAsync(byte[] buffer, int offset, int count, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken);
        ITask<int> ReadAsync(byte[] buffer, int offset, int count);
        int ReadByte();
        long Seek(long offset, SeekOrigin origin);
        void SetLength(long value);
        void Write(byte[] buffer, int offset, int count);
        void Write(ReadOnlySpan<byte> buffer);
        ITask WriteAsync(byte[] buffer, int offset, int count);
        ITask WriteAsync(byte[] buffer, int offset, int count, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken);
        IValueTask WriteAsync(ReadOnlyMemory<byte> buffer, [TypeShim(typeof(CancellationToken))] ICancellationToken cancellationToken = default);
        void WriteByte(byte value);
        IWaitHandle CreateWaitHandle();
        void Dispose(bool disposing);
        void ObjectInvariant();
    }
}
