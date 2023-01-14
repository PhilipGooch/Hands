using System;
using UnityEngine;

namespace NBG.Core.Streams
{
    internal class BasicStreamImpl : IStream
    {
        private const int kBufferSizeIncrement = 1024;

        private byte[] buffer;
        private int offsetBits = 0; // 'start' bit position of stream in buffer (used when streams contain sub-streams)
        private int currentByte;
        private int currentBit;
        private int readLimit;

        /// <summary>
        /// Creates a new basic IStream.
        /// </summary>
        /// <param name="buffer">Byte buffer to use</param>
        /// <param name="offsetBits">offset, that is applied for reading and seeking</param>
        /// <param name="readLimitBits">Optional read limit in bits. Reads beyond this bit throw exceptions</param>
        public BasicStreamImpl(byte[] buffer, int offsetBits, int readLimitBits = 0)
        {
            this.buffer = buffer;
            this.offsetBits = offsetBits;
            this.readLimit = readLimitBits;
        }

        public byte[] Buffer => buffer;
        public int PositionBits => currentByte * 8 + currentBit;
        public int PositionBytes => currentByte + (currentBit > 0 ? 1 : 0); //Position bytes pads upwards.

        public int LimitBytes => (readLimit + 7) / 8;
        public int LimitBits => readLimit;
        
        public void Seek(int pos)
        {
            if (readLimit > 0 && pos > readLimit)
                throw new Exception($"Seek {pos} is beyond readLimitBits of {readLimit}");
            pos += offsetBits;
            currentByte = pos / 8;
            currentBit = pos % 8;
        }
        public void Flip()
        {
            readLimit = PositionBits;
            Seek(0);
        }

        public void Reset()
        {
            readLimit = 0; 
            Seek(0);
        }
        public bool BitsAvailable(uint count)
        {
            Debug.Assert(count > 0);
            return (PositionBits + count) <= LimitBits;
        }
        private void Advance()
        {
            if (currentBit == 7)
            {
                currentBit = 0;
                currentByte++;
            }
            else
                currentBit++;
            
            if (readLimit > 0 && PositionBits > readLimit)
                throw new Exception($"Advanced beyond readLimitBits {readLimit}");
        }
        private void Advance(int bits)
        {
            currentBit += bits;
            while (currentBit > 8)
            {
                currentBit -= 8;
                currentByte++;
            }
            
            if (readLimit > 0 && PositionBits > readLimit)
                throw new Exception($"Advanced beyond readLimitBits {readLimit}");
        }
        #region Netstream Expansion
        void Expand(int targetSize)
        {
            int newSize = targetSize;
            if (targetSize % kBufferSizeIncrement != 0)
                newSize += kBufferSizeIncrement - (targetSize % kBufferSizeIncrement);

            //Catch shrink and 0-growth cases here.
            Debug.Assert(newSize > buffer.Length, "Buffer failed to grow in Expand");
            Array.Resize(ref buffer, newSize);
        }
        void EnsureFitsBits(int extraBits)
        {
            extraBits -= (8 - currentBit);// bits in the current byte;

            var requiredSize = (PositionBits + extraBits + 7) / 8;

            if (requiredSize > buffer.Length)
                Expand(requiredSize);
        }
        public void EnsureFitsBytes(int extraBytes, bool pad)
        {
            var extraBits = extraBytes * 8;
            if (pad) extraBits += 8 - currentBit; // this amount goes to waste
            EnsureFitsBits(extraBits);
        }

        #endregion


        #region IStreamReader
        //TODO: This uses lenBits, but only support LenBytes. Can we change this? (breaks file compatiblity to HFF skins, etc)
        public byte[] ReadArray(ushort lenBytes)
        {
            // note: if lenBits is not a multiple of 8, then this function will fail. But for backwards compatibility with saves etc we cannot change this code //TODO: verify this comment
            // Also, if lenBits < 32 the length field will get sign-extended, which is almost certainly NOT what's wanted! //TODO: verify this comment
            PadToByte();
            var result = new byte[lenBytes];
            Array.Copy(buffer, currentByte, result, 0, lenBytes);
            currentByte += lenBytes;
            return result;
        }

        public bool ReadBool()
        {
            if (currentByte >= buffer.Length)
            {
                throw new Exception("End of Buffer reached");
            }
            var currentMask = (byte)(0x80 >> currentBit);
            var result = (buffer[currentByte] & currentMask) != 0;
            Advance();
            return result;
        }
        public uint ReadUInt32(ushort bitfull)
        {
            int blocker = 8;
            int result = 0;
            while (bitfull > 0)
            {
                if (blocker-- <= 0) break;
                
                if (readLimit > 0 && PositionBits >= readLimit)
                    throw new Exception("Limit reached");

                if (currentByte > buffer.Length - 1)
                    throw new Exception("End of Buffer reached");

                var b = buffer[currentByte];
                b &= (byte)(0xFF >> currentBit);

                if (currentBit + bitfull < 8)
                {
                    b >>= 8 - (currentBit + bitfull);
                    currentBit += bitfull;
                    bitfull = 0;
                }
                else
                {
                    bitfull = (ushort)(bitfull - (8 - currentBit));
                    currentBit = 0;
                    currentByte++;
                }
                result += b << bitfull;
            }
            return (uint)result;
        }

        public byte ReadByte()
        {
            return (byte)ReadUInt32(8);
        }

        public int ReadInt32(ushort bitfull)
        {
            var result = (int)ReadUInt32(bitfull);

            // pad to negative
            var signbit = 1 << (bitfull - 1);
            if ((result & signbit) != 0)
                result |= -1 << bitfull;

            return result;
        }

        public int ReadInt32(ushort bitsmall, ushort bitlarge)
        {
            if (ReadBool())
                return ReadInt32(bitsmall);
            else
                return ReadInt32(bitlarge);
        }

        public int ReadInt32(ushort bitsmall, ushort bitmed, ushort bitlarge)
        {
            if (ReadBool())
                return ReadInt32(bitsmall);
            else
            {
                if (ReadBool())
                    return ReadInt32(bitmed);
                else
                    return ReadInt32(bitlarge);
            }
        }
        public uint ReadUInt32(ushort bitsmall, ushort bitlarge)
        {
            if (ReadBool())
                return ReadUInt32(bitsmall);
            else
                return ReadUInt32(bitlarge);
        }

        public uint ReadUInt32(ushort bitsmall, ushort bitmed, ushort bitlarge)
        {
            if (ReadBool())
                return ReadUInt32(bitsmall);
            else
            {
                if (ReadBool())
                    return ReadUInt32(bitmed);
                else
                    return ReadUInt32(bitlarge);
            }
        }
        #endregion

        public void Write(bool v)
        {
            // ensure we have space
            if (currentBit == 0 && currentByte == buffer.Length)
                Expand(currentByte + 32);

            var currentMask = (byte)(0x80 >> currentBit);
            if (v)
                buffer[currentByte] = (byte)(buffer[currentByte] | currentMask); // set bit
            else
                buffer[currentByte] = (byte)(buffer[currentByte] & ~currentMask); // clear bit

            Advance();

        }
        //TODO: While overloads work, I find sometimes they hide issues, especially when refactoring quickly. I would go for ReadBool, ReadInt, etc, opposed to just Write
        public void Write(byte b)
        {
            Write((uint)b, 8);
        }
        //TODO: Assertions/warnings that you don't clamp the value? 
        public void Write(int x, ushort bitsmall, ushort bitlarge)
        {
            var maxSmall = (1 << (bitsmall - 1));
            if (x >= -maxSmall && x < maxSmall)
            {
                Write(true);
                Write(x, bitsmall);
            }
            else
            {
                Write(false);
                Write(x, bitlarge);
            }
        }
        public void Write(int x, ushort bitsmall, ushort bitmed, ushort bitlarge)
        {
            var maxSmall = (1 << (bitsmall - 1));
            if (x >= -maxSmall && x < maxSmall)
            {
                Write(true);
                Write(x, bitsmall);
            }
            else
            {
                Write(false);

                var maxMed = (1 << (bitmed - 1));
                if (x >= -maxMed && x < maxMed)
                {
                    Write(true);
                    Write(x, bitmed);
                }
                else
                {
                    Write(false);
                    Write(x, bitlarge);
                }
            }
        }

        public void Write(int x, ushort bits)
        {
            Write((uint)x, bits);
        }

        public void Write(uint x, ushort bitfull)
        {
            int blocker = 8;
            while (bitfull > 0)
            {
                if (blocker-- <= 0) break;

                if (currentBit == 0 && currentByte == buffer.Length)
                    Expand(currentByte + bitfull);

                var mask = (byte)(0xFF >> currentBit);


                if (currentBit + bitfull < 8)
                {
                    var b = (byte)(x << (8 - currentBit) - bitfull);
                    mask &= (byte)(0xFF << ((8 - currentBit) - bitfull)); // end clear bits
                    buffer[currentByte] = (byte)(buffer[currentByte] & ~mask | b & mask);
                    currentBit += bitfull;
                    bitfull = 0;
                }
                else
                {
                    var b = (byte)(x >> (bitfull - (8 - currentBit)));
                    buffer[currentByte] = (byte)(buffer[currentByte] & ~mask | b & mask);
                    bitfull = (ushort)(bitfull - (8 - currentBit));
                    currentBit = 0;
                    currentByte++;
                }
            }
        }

        public void Write(uint x, ushort bitsmall, ushort bitlarge)
        {
            var maxSmall = (1 << bitsmall);
            if (x < maxSmall)
            {
                Write(true);
                Write(x, bitsmall);
            }
            else
            {
                Write(false);
                Write(x, bitlarge);
            }
        }

        public void Write(uint x, ushort bitsmall, ushort bitmed, ushort bitlarge)
        {
            var maxSmall = (1 << bitsmall);
            if (x < maxSmall)
            {
                Write(true);
                Write(x, bitsmall);
            }
            else
            {
                Write(false);

                var maxMed = (1 << bitmed);
                if (x < maxMed)
                {
                    Write(true);
                    Write(x, bitmed);
                }
                else
                {
                    Write(false);
                    Write(x, bitlarge);
                }
            }
        }

        public void WriteArray(byte[] array, ushort lenBytes)
        {
            // note: if lenBits is not a multiple of 8, then this function will fail. But for backwards compatibility with saves etc we cannot change this code //TODO: verify this comment
            PadToByte();
            EnsureFitsBytes(lenBytes, true);
            Array.Copy(array, 0, buffer, currentByte, lenBytes);
            currentByte += lenBytes;
        }

        private void PadToByte()
        {
            if (currentBit > 0)
            {
                currentBit = 0;
                currentByte++;
            }
        }
    }
}
