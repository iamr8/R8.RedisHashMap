using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace R8.RedisHashMap
{
    /// <summary>
    ///     A reusable <see cref="IBufferWriter{T}" /> over a growable byte array optimized for serialization hot paths:
    ///     resetting does not clear the underlying memory (unlike <see cref="ArrayBufferWriter{T}.Clear" />),
    ///     and all internal arrays are allocated uninitialized to skip zeroing costs.
    /// </summary>
    public sealed class PooledBufferWriter : IBufferWriter<byte>
    {
        private byte[] _buffer;
        private int _index;

        public PooledBufferWriter(int initialCapacity = 1024)
        {
            if (initialCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));

            _buffer = GC.AllocateUninitializedArray<byte>(initialCapacity);
            _index = 0;
        }

        /// <summary>Gets the number of bytes written so far.</summary>
        public int WrittenCount => _index;

        /// <summary>Gets the written bytes as a span.</summary>
        public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count)
        {
            _index += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsMemory(_index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsSpan(_index);
        }

        /// <summary>
        ///     Ensures <paramref name="size" /> writable bytes and returns a span of exactly that length,
        ///     so callers can write through unchecked <c>Unsafe.Add</c> stores within a known bound.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> GetSpanExact(int size)
        {
            EnsureCapacity(size);
            return _buffer.AsSpan(_index, size);
        }

        /// <summary>
        ///     Ensures <paramref name="size" /> writable bytes and returns a reference to the first of them.
        ///     Callers write through <c>Unsafe.Add</c> within that bound and then call <see cref="Advance" />.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref byte GetReferenceExact(int size)
        {
            EnsureCapacity(size);
            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_buffer), (nint)(uint)_index);
        }

        /// <summary>Moves the write cursor back to a previously observed <see cref="WrittenCount" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rewind(int position)
        {
            _index = position;
        }

        /// <summary>Resets the written count without clearing the underlying buffer (no memset).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetWrittenCount()
        {
            _index = 0;
        }

        /// <summary>Copies the written bytes into a new exact-size uninitialized array and resets the writer.</summary>
        public byte[] ToArrayAndReset()
        {
            var array = GC.AllocateUninitializedArray<byte>(_index);
            _buffer.AsSpan(0, _index).CopyTo(array);
            _index = 0;
            return array;
        }

        private void EnsureCapacity(int sizeHint)
        {
            if (sizeHint < 1)
                sizeHint = 1;

            var available = _buffer.Length - _index;
            if (sizeHint <= available)
                return;

            var newSize = Math.Max(_buffer.Length * 2, _index + sizeHint);
            var newBuffer = GC.AllocateUninitializedArray<byte>(newSize);
            _buffer.AsSpan(0, _index).CopyTo(newBuffer);
            _buffer = newBuffer;
        }
    }
}
