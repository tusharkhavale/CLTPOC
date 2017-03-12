using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Pitch
{
    public class CircularBuffer<T> : IDisposable
    {
        int bufSize;
        int begBufOffset;
        int availBuf;
        long startPosition;   // total circular buffer position
        T[] buffer;

        /// <summary>
        /// Constructor
        /// </summary>
        public CircularBuffer()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bufCount"></param>
        public CircularBuffer(int bufCount)
        {
            SetSize(bufCount);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            SetSize(0);
        }

        /// <summary>
        /// Reset to the beginning of the buffer
        /// </summary>
        public void Reset()
        {
            begBufOffset = 0;
            availBuf = 0;
            startPosition = 0;
        }

        /// <summary>
        /// Set the buffer to the specified size
        /// </summary>
        /// <param name="newSize"></param>
        public void SetSize(int newSize)
        {
            Reset();

            if (bufSize == newSize)
                return;

            if (buffer != null)
                buffer = null;

            bufSize = newSize;

            if (bufSize > 0)
                buffer = new T[bufSize];
        }

        /// <summary>
        /// Clear the buffer
        /// </summary>
        public void Clear()
        {
            Array.Clear(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Get or set the start position
        /// </summary>
        public long StartPosition
        {
            get { return startPosition; }
            set { startPosition = value; }
        }

        /// <summary>
        /// Get the end position
        /// </summary>
        public long EndPosition
        {
            get { return startPosition + availBuf; }
        }

        /// <summary>
        /// Get or set the amount of avaliable space
        /// </summary>
        public int Available
        {
            get { return availBuf; }
            set { availBuf = Math.Min(value, bufSize); }
        }

        /// <summary>
        /// Write data into the buffer
        /// </summary>
        /// <param name="m_pInBuffer"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public int WriteBuffer(T[] m_pInBuffer, int count)
        {
            count = Math.Min(count, bufSize);

            var startPos = availBuf != bufSize ? availBuf : begBufOffset;
            var pass1Count = Math.Min(count, bufSize - startPos);
            var pass2Count = count - pass1Count;

            PitchDsp.CopyBuffer(m_pInBuffer, 0, buffer, startPos, pass1Count);

            if (pass2Count > 0)
                PitchDsp.CopyBuffer(m_pInBuffer, pass1Count, buffer, 0, pass2Count);

            if (pass2Count == 0)
            {
                // did not wrap around
                if (availBuf != bufSize)
                    availBuf += count;   // have never wrapped around
                else
                {
                    begBufOffset += count;
                    startPosition += count;
                }
            }
            else
            {
                // wrapped around
                if (availBuf != bufSize)
                    startPosition += pass2Count;  // first time wrap-around
                else
                    startPosition += count;

                begBufOffset = pass2Count;
                availBuf = bufSize;
            }

            return count;
        }

        /// <summary>
        /// Read from the buffer
        /// </summary>
        /// <param name="outBuffer"></param>
        /// <param name="startRead"></param>
        /// <param name="readCount"></param>
        /// <returns></returns>
        public bool ReadBuffer(T[] outBuffer, long startRead, int readCount)
        {
            var endRead = (int)(startRead + readCount);
            var endAvail = (int)(startPosition + availBuf);

            if (startRead < startPosition || endRead > endAvail)
                return false;

            var startReadPos = (int)(((startRead - startPosition) + begBufOffset) % bufSize);
            var block1Samples = Math.Min(readCount, bufSize - startReadPos);
            var block2Samples = readCount - block1Samples;

            PitchDsp.CopyBuffer(buffer, startReadPos, outBuffer, 0, block1Samples);

            if (block2Samples > 0)
                PitchDsp.CopyBuffer(buffer, 0, outBuffer, block1Samples, block2Samples);

            return true;
        }
    }
}
