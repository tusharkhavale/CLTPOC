using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections;

namespace Pitch
{
    /// <summary>
    /// Pitch related DSP
    /// </summary>
    public class PitchDsp
    {
        public static readonly double InverseLog2 = 1.0 / Math.Log10(2.0);

        private const int kCourseOctaveSteps = 96;
        private const int kScanHiSize = 31;
        private const float kScanHiFreqStep = 1.005f;
        private const int kMinMidiNote = 21;  // A0
        private const int kMaxMidiNote = 108; // C8

        private float minPitch;
        private float maxPitch;
        private int minNote;
        private int maxNote;
        private int blockLen14;	   // 1/4 block len
        private int blockLen24;	   // 2/4 block len
        private int blockLen34;	   // 3/4 block len
        private int blockLen44;	   // 4/4 block len
        private double sampleRate;
        private float detectLevelThreshold;

        private int numCourseSteps;
        private float[] pCourseFreqOffset;
        private float[] pCourseFreq;
        private float[] scanHiOffset = new float[kScanHiSize];
        private float[] peakBuf = new float[kScanHiSize];
        private int prevPitchIdx;
        private float[] detectCurve;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fSampleRate"></param>
        /// <param name="minFreq"></param>
        /// <param name="maxFreq"></param>
        /// <param name="fFreqStep"></param>
        public PitchDsp(double sampleRate, float minPitch, float maxPitch, float detectLevelThreshold)
        {
            sampleRate = sampleRate;
            minPitch = minPitch;
            maxPitch = maxPitch;
            detectLevelThreshold = detectLevelThreshold;

            minNote = (int)(PitchToMidiNote(minPitch) + 0.5f) + 2;
            maxNote = (int)(PitchToMidiNote(maxPitch) + 0.5f) - 2;

            blockLen44 = (int)(sampleRate / minPitch + 0.5f);
            blockLen34 = (blockLen44 * 3) / 4;
            blockLen24 = blockLen44 / 2;
            blockLen14 = blockLen44 / 4;

            numCourseSteps = (int)((Math.Log((double)maxPitch / (double)minPitch) / Math.Log(2.0)) * kCourseOctaveSteps + 0.5) + 3;

            pCourseFreqOffset = new float[numCourseSteps + 10000];
            pCourseFreq = new float[numCourseSteps + 10000];

            detectCurve = new float[numCourseSteps];

            var freqStep = 1.0 / Math.Pow(2.0, 1.0 / kCourseOctaveSteps);
            var curFreq = maxPitch / freqStep;

            // frequency is stored from high to low
            for (int idx = 0; idx < numCourseSteps; idx++)
            {
                pCourseFreq[idx] = (float)curFreq;
                pCourseFreqOffset[idx] = (float)(sampleRate / curFreq);
                curFreq *= freqStep;
            }

            for (int idx = 0; idx < kScanHiSize; idx++)
                scanHiOffset[idx] = (float)Math.Pow(kScanHiFreqStep, (kScanHiSize / 2) - idx);
        }

        /// <summary>
        /// Get the max detected pitch
        /// </summary>
        public float MaxPitch
        {
            get { return maxPitch; }
        }

        /// <summary>
        /// Get the min detected pitch
        /// </summary>
        public float MinPitch
        {
            get { return minPitch; }
        }

        /// <summary>
        /// Get the max note
        /// </summary>
        public int MaxNote
        {
            get { return maxNote; }
        }

        /// <summary>
        /// Get the min note
        /// </summary>
        public int MinNote
        {
            get { return minNote; }
        }

        /// <summary>
        /// Detect the pitch
        /// </summary>
        public float DetectPitch(float[] samplesLo, float[] samplesHi, int numSamples)
        {
            var pitch = 0.0f;

            if (!LevelIsAbove(samplesLo, numSamples, detectLevelThreshold) &&
                !LevelIsAbove(samplesHi, numSamples, detectLevelThreshold))
            {
                // Level is too low
                return 0.0f;
            }

            pitch = DetectPitchLo(samplesLo, samplesHi);
            return pitch;
        }

        /// <summary>
        /// Low resolution pitch detection
        /// </summary>
        /// <param name="dataIdx"></param>
        /// <param name="begFreqIdx"></param>
        /// <param name="endFreqIdx"></param>
        /// <param name="blockLen"></param>
        /// <param name="stepSize"></param>
        /// <returns></returns>
        private float DetectPitchLo(float[] samplesLo, float[] samplesHi)
        {
            detectCurve.Clear();

            const int skipSize = 8;
            const int peakScanSize = 23;
            const int peakScanSizeHalf = peakScanSize / 2;

            var peakThresh1 = 200.0f;
            var peakThresh2 = 600.0f;
            var bufferSwitched = false;

            for (int idx = 0; idx < numCourseSteps; idx += skipSize)
            {
                var blockLen = Math.Min(blockLen44, (int)pCourseFreqOffset[idx] * 2);
                float[] curSamples = null;

                // 258 is at 250 Hz, which is the switchover frequency for the two filters
                var loBuffer = idx >= 258;

                if (loBuffer)
                {
                    if (!bufferSwitched)
                    {
                        detectCurve.Clear(258 - peakScanSizeHalf, 258 + peakScanSizeHalf);
                        bufferSwitched = true;
                    }

                    curSamples = samplesLo;
                }
                else
                {
                    curSamples = samplesHi;
                }

                var stepSizeLoRes = blockLen / 10;
                var stepSizeHiRes = Math.Max(1, Math.Min(5, idx * 5 / numCourseSteps));

                float fValue = RatioAbsDiffLinear(curSamples, idx, blockLen, stepSizeLoRes, false);

                if (fValue > peakThresh1)
                {
                    // Do a closer search for the peak
                    var peakIdx = -1;
                    var peakVal = 0.0f;
                    var prevVal = 0.0f;
                    var dir = 4;		 // start going forward
                    var curPos = idx;	 // start at center of the scan range
                    var begSearch = Math.Max(idx - peakScanSizeHalf, 0);
                    var endSearch = Math.Min(idx + peakScanSizeHalf, numCourseSteps - 1);

                    while (curPos >= begSearch && curPos < endSearch)
                    {
                        var curVal = RatioAbsDiffLinear(curSamples, curPos, blockLen, stepSizeHiRes, true);

                        if (peakVal < curVal)
                        {
                            peakVal = curVal;
                            peakIdx = curPos;
                        }

                        if (prevVal > curVal)
                        {
                            dir = -dir >> 1;

                            if (dir == 0)
                            {
                                if (peakVal > peakThresh2 && peakIdx >= 6 && peakIdx <= numCourseSteps - 7)
                                {
                                    var fValL = RatioAbsDiffLinear(curSamples, peakIdx - 5, blockLen, stepSizeHiRes, true);
                                    var fValR = RatioAbsDiffLinear(curSamples, peakIdx + 5, blockLen, stepSizeHiRes, true);
                                    var fPointy = peakVal / (fValL + fValR) * 2.0f;

                                    var minPointy = (prevPitchIdx > 0 && Math.Abs(prevPitchIdx - peakIdx) < 10) ? 1.2f : 1.5f;

                                    if (fPointy > minPointy)
                                    {
                                        var pitchHi = DetectPitchHi(curSamples, peakIdx);

                                        if (pitchHi > 1.0f)
                                        {
                                            prevPitchIdx = peakIdx;
                                            return pitchHi;
                                        }
                                    }
                                }

                                break;
                            }
                        }

                        prevVal = curVal;
                        curPos += dir;
                    }
                }
            }

            prevPitchIdx = 0;
            return 0.0f;
        }

        /// <summary>
        /// High resolution pitch detection
        /// </summary>
        /// <param name="dataIdx"></param>
        /// <param name="lowFreqIdx"></param>
        /// <returns></returns>
        private float DetectPitchHi(float[] samples, int lowFreqIdx)
        {
            var peakIdx = -1;
            var prevVal = 0.0f;
            var dir = 4;     // start going forward
            var curPos = kScanHiSize >> 1;	 // start at center of the scan range

            peakBuf.Clear();

            float offset = pCourseFreqOffset[lowFreqIdx];

            while (curPos >= 0 && curPos < kScanHiSize)
            {
                if (peakBuf[curPos] == 0.0f)
                    peakBuf[curPos] = SumAbsDiffHermite(samples, offset * scanHiOffset[curPos], blockLen44, 1);

                if (peakIdx < 0 || peakBuf[peakIdx] < peakBuf[curPos])
                    peakIdx = curPos;

                if (prevVal > peakBuf[curPos])
                {
                    dir = -dir >> 1;

                    if (dir == 0)
                    {
                        // found the peak
                        var minVal = Math.Min(peakBuf[peakIdx - 1], peakBuf[peakIdx + 1]);

                        minVal -= minVal * (1.0f / 32.0f);

                        var y1 = (float)Math.Log10(peakBuf[peakIdx - 1] - minVal);
                        var y2 = (float)Math.Log10(peakBuf[peakIdx] - minVal);
                        var y3 = (float)Math.Log10(peakBuf[peakIdx + 1] - minVal);

                        var fIdx = (float)peakIdx + (y3 - y1) / (2.0f * (2.0f * y2 - y1 - y3));

                        return (float)Math.Pow(kScanHiFreqStep, fIdx - (kScanHiSize / 2)) * pCourseFreq[lowFreqIdx];
                    }
                }

                prevVal = peakBuf[curPos];
                curPos += dir;
            }

            return 0.0f;
        }

        /// <summary>
        /// Create a sine wave with the specified frequency, amplitude and starting angle.
        /// Returns the updated angle.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="numSamples"></param>
        /// <param name="freq"></param>
        /// <param name="amplitude"></param>
        /// <param name="startAngle"></param>
        /// <returns></returns>
        public static double CreateSineWave(float[] buffer, int numSamples, float sampleRate,
                                            float freq, float amplitude, double startAngle)
        {
            var angleStep = freq / sampleRate * Math.PI * 2.0;
            var curAngle = startAngle;

            for (int idx = 0; idx < numSamples; idx++)
            {
                buffer[idx] = (float)Math.Sin(curAngle) * amplitude;

                curAngle += angleStep;

                while (curAngle > Math.PI)
                    curAngle -= Math.PI * 2.0;
            }

            return curAngle;
        }

        /// <summary>
        /// Returns true if the level is above the specified value
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startIdx"></param>
        /// <param name="len"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public bool LevelIsAbove(float[] buffer, int len, float level)
        {
            if (buffer == null || buffer.Length == 0)
                return false;

            var endIdx = Math.Min(buffer.Length, len);

            for (int idx = 0; idx < endIdx; idx++)
            {
                if (Math.Abs(buffer[idx]) >= level)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Copy the data from the source to the destination buffer
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="startPos"></param>  
        /// <param name="length"></param>
        public static void CopyBuffer<T>(T[] source, int srcStart, T[] destination, int dstStart, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");

            if (source == null || source.Length < srcStart + length)
                throw new Exception("Source buffer is null or not large enough");

            if (destination == null || destination.Length < dstStart + length)
                throw new Exception("Destination buffer is null or not large enough");

            var srcIdx = srcStart;
            var dstIdx = dstStart;

            for (int idx = 0; idx < length; idx++)
                destination[dstIdx++] = source[srcIdx++];
        }

        /// <summary>
        /// // 4-point, 3rd-order Hermite (x-form)
        /// </summary>
        private float InterpolateHermite(float fY0, float fY1, float fY2, float fY3, float frac)
        {
            var c1 = 0.5f * (fY2 - fY0);
            var c3 = 1.5f * (fY1 - fY2) + 0.5f * (fY3 - fY0);
            var c2 = fY0 - fY1 + c1 - c3;

            return ((c3 * frac + c2) * frac + c1) * frac + fY1;
        }

        /// <summary>
        /// Linear interpolation
        /// nFrac is based on 1.0 = 256
        /// </summary>
        private float InterpolateLinear(float y0, float y1, float frac)
        {
            return y0 * (1.0f - frac) + y1 * frac;
        }

        /// <summary>
        /// Medium Low res SumAbsDiff
        /// </summary>
        private float RatioAbsDiffLinear(float[] samples, int freqIdx, int blockLen, int stepSize, bool hiRes)
        {
            if (hiRes && detectCurve[freqIdx] > 0.0f)
                return detectCurve[freqIdx];

            var offsetInt = (int)pCourseFreqOffset[freqIdx];
            var offsetFrac = pCourseFreqOffset[freqIdx] - offsetInt;
            var rect = 0.0f;
            var absDiff = 0.01f;   // prevent divide by zero
            var count = 0;
            float interp;
            float sample;

            // Do a scan using linear interpolation and the specified step size
            for (int idx = 0; idx < blockLen; idx += stepSize, count++)
            {
                sample = samples[idx];
                interp = InterpolateLinear(samples[offsetInt + idx], samples[offsetInt + idx + 1], offsetFrac);
                absDiff += Math.Abs(sample - interp);
                rect += Math.Abs(sample) + Math.Abs(interp);
            }

            var finalVal = rect / absDiff * 100.0f;

            if (hiRes)
                detectCurve[freqIdx] = finalVal;

            return finalVal;
        }

        /// <summary>
        /// Medium High res SumAbsDiff
        /// </summary>
        private float SumAbsDiffHermite(float[] samples, float fOffset, int blockLen, int stepSize)
        {
            var offsetInt = (int)fOffset;
            var offsetFrac = fOffset - offsetInt;
            var value = 0.001f;   // prevent divide by zero
            var count = 0;

            // do a scan using linear interpolation and the specified step size
            for (int idx = 0; idx < blockLen; idx += stepSize, count++)
            {
                var offsetIdx = offsetInt + idx;

                value += Math.Abs(samples[idx] - InterpolateHermite(samples[offsetIdx - 1],
                                                                    samples[offsetIdx],
                                                                    samples[offsetIdx + 1],
                                                                    samples[offsetIdx + 2],
                                                                    offsetFrac));
            }

            return count / value;
        }

        /// <summary>
        /// Get the MIDI note and cents of the pitch 
        /// </summary>
        /// <param name="pitch"></param>
        /// <param name="note"></param>
        /// <param name="cents"></param>
        /// <returns></returns>
        public static bool PitchToMidiNote(float pitch, out int note, out int cents)
        {
            if (pitch < 20.0f)
            {
                note = 0;
                cents = 0;
                return false;
            }

            var fNote = (float)((12.0 * Math.Log10(pitch / 55.0) * InverseLog2)) + 33.0f;
            note = (int)(fNote + 0.5f);
            cents = (int)((note - fNote) * 100);
            return true;
        }

        /// <summary>
        /// Get the pitch from the MIDI note
        /// </summary>
        /// <param name="pitch"></param>
        /// <returns></returns>
        public static float PitchToMidiNote(float pitch)
        {
            if (pitch < 20.0f)
                return 0.0f;

            return (float)(12.0 * Math.Log10(pitch / 55.0) * InverseLog2) + 33.0f;
        }

        /// <summary>
        /// Get the pitch from the MIDI note
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        public float MidiNoteToPitch(float note)
        {
            if (note < 33.0f)
                return 0.0f;

            var pitch = (float)Math.Pow(10.0, (note - 33.0f) / InverseLog2 / 12.0f) * 55.0f;
            return pitch <= maxPitch ? pitch : 0.0f;
        }

        /// <summary>
        /// Format a midi note to text
        /// </summary>
        /// <param name="note"></param>
        /// <param name="sharps"></param>
        /// <param name="showOctave"></param>
        /// <returns></returns>
        public static string GetNoteName(int note, bool sharps, bool showOctave)
        {
            if (note < kMinMidiNote || note > kMaxMidiNote)
                return null;

            note -= kMinMidiNote;

            int octave = (note + 9) / 12;
            note = note % 12;
            string noteText = null;

            switch (note)
            {
                case 0:
                    noteText = "A";
                    break;

                case 1:
                    noteText = sharps ? "A#" : "Bb";
                    break;

                case 2:
                    noteText = "B";
                    break;

                case 3:
                    noteText = "C";
                    break;

                case 4:
                    noteText = sharps ? "C#" : "Db";
                    break;

                case 5:
                    noteText = "D";
                    break;

                case 6:
                    noteText = sharps ? "D#" : "Eb";
                    break;

                case 7:
                    noteText = "E";
                    break;

                case 8:
                    noteText = "F";
                    break;

                case 9:
                    noteText = sharps ? "F#" : "Gb";
                    break;

                case 10:
                    noteText = "G";
                    break;

                case 11:
                    noteText = sharps ? "G#" : "Ab";
                    break;
            }

            if (showOctave)
                noteText += " " + octave.ToString();

            return noteText;
        }

    }
}
