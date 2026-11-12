using CoffeeNChill.Functions.Validation;

namespace CoffeeNChill.Functions.Helpers;

/// <summary>
/// Stream utilities so uploads are validated and size-limited WHILE they are
/// streamed to Blob Storage theerefore the file is never buffered in memory.
/// </summary>
public static class StreamHelper
{
    /// <summary>
    /// Reads only the first 5 bytes, checks for the "%PDF-" signature, and returns a
    /// stream that replays those bytes which are followed by the rest of the original stream.
    /// </summary>
    public static async Task<Stream> ReadAndValidatePdfHeaderAsync(Stream source, CancellationToken cancellationToken = default)
    {
        byte[] header = new byte[5];
        int total = 0;

        while (total < header.Length)
        {
            int read = await source.ReadAsync(header.AsMemory(total, header.Length - total), cancellationToken);
            if (read == 0) break;
            total += read;
        }

        FileValidator.ValidatePdfSignature(header, total);

        return new PrefixedStream(header, total, source);
    }
}

/// <summary>Forward-only read stream: prefix bytes first, then the inner stream.</summary>
public sealed class PrefixedStream : Stream
{
    private readonly byte[] _prefix;
    private readonly int _prefixLength;
    private readonly Stream _inner;
    private int _prefixPosition;

    public PrefixedStream(byte[] prefix, int prefixLength, Stream inner)
    {
        _prefix = prefix;
        _prefixLength = prefixLength;
        _inner = inner;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_prefixPosition < _prefixLength)
        {
            int n = Math.Min(count, _prefixLength - _prefixPosition);
            Buffer.BlockCopy(_prefix, _prefixPosition, buffer, offset, n);
            _prefixPosition += n;
            return n;
        }

        return _inner.Read(buffer, offset, count);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_prefixPosition < _prefixLength)
        {
            int n = Math.Min(buffer.Length, _prefixLength - _prefixPosition);
            _prefix.AsMemory(_prefixPosition, n).CopyTo(buffer);
            _prefixPosition += n;
            return n;
        }

        return await _inner.ReadAsync(buffer, cancellationToken);
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

/// <summary>
/// Counts the real bytes read and throws ArgumentException (-> HTTP 400) as soon as the
/// limit is exceeded. So a lying Content-Length header can't bypass the 10 MB cap.
/// </summary>
public sealed class MaxLengthStream : Stream
{
    private readonly Stream _inner;
    private readonly long _maxBytes;
    private long _bytesRead;

    public MaxLengthStream(Stream inner, long maxBytes)
    {
        _inner = inner;
        _maxBytes = maxBytes;
    }

    private void Track(int read)
    {
        _bytesRead += read;
        if (_bytesRead > _maxBytes)
        {
            throw new ArgumentException("The uploaded file cannot be larger than 10 MB.");
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = _inner.Read(buffer, offset, count);
        Track(read);
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int read = await _inner.ReadAsync(buffer, cancellationToken);
        Track(read);
        return read;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
