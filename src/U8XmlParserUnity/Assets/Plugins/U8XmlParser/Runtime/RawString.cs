﻿#nullable enable
using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using U8Xml.Internal;
using System.Buffers;

namespace U8Xml
{
    /// <summary>Provides raw byte array of utf8, which is compatible <see cref="ReadOnlySpan{T}"/> of <see langword="byte"/>.</summary>
    [DebuggerTypeProxy(typeof(RawStringDebuggerTypeProxy))]
    [DebuggerDisplay("{ToString()}")]
    public readonly unsafe partial struct RawString : IEquatable<RawString>
    {
        private readonly IntPtr _ptr;
        private readonly int _length;

        /// <summary>Get an empty instance of <see cref="RawString"/>.</summary>
        public static RawString Empty => default;

        /// <summary>Get whether the byte array is empty or not.</summary>
        public bool IsEmpty => _length == 0;

        /// <summary>Get length of the byte array. (NOT length of utf-8 string)</summary>
        public int Length => _length;

        /// <summary>Get or set an item with specified index</summary>
        /// <param name="index">index of an item</param>
        /// <returns>the item</returns>
        public ref readonly byte this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if((uint)index >= (uint)_length) { ThrowHelper.ThrowArgOutOfRange(nameof(index)); }
                return ref ((byte*)_ptr)[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RawString(byte* ptr, int length)
        {
            Debug.Assert(length >= 0);
            _ptr = (IntPtr)ptr;
            _length = length;
        }

        /// <summary>Get read-only bytes data</summary>
        /// <returns><see cref="ReadOnlySpan{T}"/> of type <see langword="byte"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsSpan() => SpanHelper.CreateReadOnlySpan<byte>(_ptr.ToPointer(), _length);

        /// <summary>Copy the bytes to a new byte array.</summary>
        /// <returns>new array</returns>
        public byte[] ToArray() => AsSpan().ToArray();

        /// <summary>Get slice of the array</summary>
        /// <param name="start">start index to slice</param>
        /// <returns>sliced array</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawString Slice(int start)
        {
            if((uint)start > (uint)_length) { ThrowHelper.ThrowArgOutOfRange(nameof(start)); }
            return new RawString((byte*)_ptr + start, _length - start);
        }

        /// <summary>Get slice of the array</summary>
        /// <param name="start">start index to slice</param>
        /// <param name="length">length to slice from <paramref name="start"/></param>
        /// <returns>sliced array</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawString Slice(int start, int length)
        {
            if((uint)start > (uint)_length) { ThrowHelper.ThrowArgOutOfRange(nameof(start)); }
            if((uint)length > (uint)(_length - start)) { ThrowHelper.ThrowArgOutOfRange(nameof(length)); }
            return new RawString((byte*)_ptr + start, length);
        }

        /// <summary>Trim invisible charactors. (whitespace, '\t', '\r', and '\n')</summary>
        /// <returns>trimmed string</returns>
        public RawString Trim()
        {
            return TrimStart().TrimEnd();
        }

        /// <summary>Trim invisible charactors of start. (whitespace, '\t', '\r', and '\n')</summary>
        /// <returns>trimmed string</returns>
        public RawString TrimStart()
        {
            for(int i = 0; i < _length; i++) {
                ref var p = ref ((byte*)_ptr)[i];
                if(p != ' ' && p != '\t' && p != '\r' && p != '\n') {
                    return SliceUnsafe(i, _length - i);
                }
            }
            return Empty;
        }

        /// <summary>Trim invisible charactors of end. (whitespace, '\t', '\r' and '\n')</summary>
        /// <returns>trimmed string</returns>
        public RawString TrimEnd()
        {
            for(int i = Length - 1; i >= 0; i--) {
                ref var p = ref ((byte*)_ptr)[i];
                if(p != ' ' && p != '\t' && p != '\r' && p != '\n') {
                    return SliceUnsafe(0, i + 1);
                }
            }
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal byte* GetPtr() => (byte*)_ptr;

        /// <summary>Get or set an item with specified index.</summary>
        /// <remarks>[CAUTION] This method does not check index boundary!</remarks>
        /// <param name="index">index of an item</param>
        /// <returns>reference to the item</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref byte At(int index)
        {
            // This method is same as this[index], but no boundary check.
#if DEBUG
            if((uint)index >= (uint)_length) { ThrowHelper.ThrowArgOutOfRange(nameof(index)); }
#endif
            return ref ((byte*)_ptr)[index];
        }

        /// <summary>Get slice of the array</summary>
        /// <remarks>[CAUTION] Boundary is not checked. Be careful !</remarks>
        /// <param name="start">start index to slice</param>
        /// <param name="length">length to slice from <paramref name="start"/></param>
        /// <returns>sliced array</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RawString SliceUnsafe(int start, int length)
        {
#if DEBUG
            if((uint)start > (uint)_length) { ThrowHelper.ThrowArgOutOfRange(nameof(start)); }
            if((uint)length > (uint)(_length - start)) { ThrowHelper.ThrowArgOutOfRange(nameof(length)); }
#endif
            return new RawString((byte*)_ptr + start, length);
        }

        /// <summary>Get pinnnable reference.</summary>
        /// <returns>reference to the head of the data</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]               // Only for 'fixed' statement
        public ref readonly byte GetPinnableReference()
        {
            return ref Unsafe.AsRef<byte>((void*)_ptr);
        }

        /// <summary>Decode byte array as utf-8 and get <see langword="string"/></summary>
        /// <returns>decoded string</returns>
        public override string ToString() => IsEmpty ? "" : Encoding.UTF8.GetString((byte*)_ptr, _length);

        public override bool Equals(object? obj) => obj is RawString array && Equals(array);

        public bool Equals(RawString other) => (_ptr == other._ptr && _length == other._length) || SequenceEqual(other);

        public bool SequenceEqual(RawString other) => AsSpan().SequenceEqual(other.AsSpan());

        public bool SequenceEqual(ReadOnlySpan<byte> other) => AsSpan().SequenceEqual(other);

        public bool StartWith(RawString other)
        {
            if(_length < other.Length) { return false; }
            for(int i = 0; i < other.Length; i++) {
                if(At(i) != other.At(i)) {
                    return false;
                }
            }
            return true;
        }

        public bool StartWith(ReadOnlySpan<byte> other)
        {
            if(_length < other.Length) { return false; }
            for(int i = 0; i < other.Length; i++) {
                if(At(i) != other.At(i)) {
                    return false;
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetHashCode(byte* ptr, int length)
        {
            // Returns hash computed by same algorithm as RawString.
            // This method is used in RawStringTable

            return XXHash32.ComputeHash(ptr, length);
        }

        public override int GetHashCode()
        {
            return GetHashCode((byte*)_ptr, _length);
        }

        public static bool operator ==(RawString left, RawString right) => left.Equals(right);

        public static bool operator !=(RawString left, RawString right) => !(left == right);

        public static bool operator ==(RawString left, string right)
        {
            if(right is null) { return left.IsEmpty; }
            var utf8 = Encoding.UTF8;
            var byteLen = utf8.GetByteCount(right);
            if(byteLen != left.Length) { return false; }
            if(byteLen <= 128) {
                byte* buf = stackalloc byte[byteLen];
                fixed(char* ptr = right) {
                    utf8.GetBytes(ptr, right.Length, buf, byteLen);
                }
                return SpanHelper.CreateReadOnlySpan<byte>(buf, byteLen).SequenceEqual(left.AsSpan());
            }
            else {
                var rentArray = ArrayPool<byte>.Shared.Rent(byteLen);
                try {
                    fixed(byte* buf = rentArray)
                    fixed(char* ptr = right) {
                        utf8.GetBytes(ptr, right.Length, buf, byteLen);
                        return SpanHelper.CreateReadOnlySpan<byte>(buf, byteLen).SequenceEqual(left.AsSpan());
                    }
                }
                finally {
                    ArrayPool<byte>.Shared.Return(rentArray);
                }
            }
        }

        public static bool operator !=(RawString left, string right) => !(left == right);

        public static bool operator ==(string left, RawString right) => right == left;

        public static bool operator !=(string left, RawString right) => !(right == left);

        //public static implicit operator ReadOnlySpan<byte>(RawString rawString) => rawString.AsSpan();
    }

    internal sealed class RawStringDebuggerTypeProxy
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly RawString _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public byte[] Items => _entity.ToArray();


        public RawStringDebuggerTypeProxy(RawString entity)
        {
            _entity = entity;
        }
    }
}
