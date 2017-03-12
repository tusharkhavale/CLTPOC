using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Threading;

namespace Pitch
{
    /// <summary>
    /// Tracks pitch
    /// </summary>
    public class PitchTracker
    {
        private const int kOctaveSteps = 96;
        private const int kStepOverlap = 4;
        private const float kMinFreq = 50.0f;               // A1, Midi note 33, 55.0Hz
        private const float kMaxFreq = 1600.0f;             // A#6. Midi note 92
        private const int kStartCircular = 40;		        // how far into the sample buffer do we start checking (allow for filter settling)
        private const float kDetectOverlapSec = 0.005f;
        private const float kMaxOctaveSecRate = 10.0f;

        private const float kAvgOffset = 0.005f;	        // time offset between pitch averaging values
        private const int kAvgCount = 1;			        // number of average pitch samples to take
        private const float kCircularBufSaveTime = 1.0f;    // Amount of samples to store in the history buffer

        private PitchDsp dsp;
        private CircularBuffer<float> circularBufferLo;
        private CircularBuffer<float> circularBufferHi;
        private double sampleRate;
        private float detectLevelThreshold = 0.01f;       // -40dB
        private int pitchRecordsPerSecond = 50;           // default is 50, or one record every 20ms

        private float[] pitchBufLo;
        private float[] pitchBufHi;
        private int pitchBufSize;
        private int samplesPerPitchBlock;
        private int curPitchIndex;
        private long curPitchSamplePos;

        private int detectOverlapSamples;
        private float maxOverlapDiff;

        private bool recordPitchRecords;
        private int pitchRecordHistorySize;
        private List<PitchRecord> pitchRecords = new List<PitchRecord>();
        private PitchRecord curPitchRecord = new PitchRecord();

        private IIRFilter iirFilterLoLo;
        private IIRFilter iirFilterLoHi;
        private IIRFilter iirFilterHiLo;
        private IIRFilter iirFilterHiHi;

        public delegate void PitchDetectedHandler(PitchTracker sender, PitchRecord pitchRecord);
		public event PitchDetectedHandler PitchDetected; 

        /// <summary>
        /// Constructor
        /// </summary>
        public PitchTracker()
        {
        }

        /// <summary>
        /// Set the sample rate
        /// </summary>
        public double SampleRate
        {
            set
            {
                if (sampleRate == value)
                    return;

                sampleRate = value;
                Setup();
            }
        }

        /// <summary>
        /// Set the detect level threshold, The value must be between 0.0001f and 1.0f (-80 dB to 0 dB)
        /// </summary>
        public float DetectLevelThreshold
        {
            set
            {
                var newValue = Math.Max(0.0001f, Math.Min(1.0f, value));

                if (detectLevelThreshold == newValue)
                    return;

                detectLevelThreshold = newValue;
                Setup();
            }
        }

        /// <summary>
        /// Return the samples per pitch block
        /// </summary>
        public int SamplesPerPitchBlock
        {
            get { return samplesPerPitchBlock; }
        }

        /// <summary>
        /// Get or set the number of pitch records per second (default is 50, or one record every 20ms)
        /// </summary>
        public int PitchRecordsPerSecond
        {
            get { return pitchRecordsPerSecond; }
            set
            {
                pitchRecordsPerSecond = Math.Max(1, Math.Min(100, value));
                Setup(); 
            }
        }

        /// <summary>
        /// Get or set whether pitch records should be recorded into a history buffer
        /// </summary>
        public bool RecordPitchRecords
        {
            get { return recordPitchRecords; }
            set
            {
                if (recordPitchRecords == value)
                    return;
                
                recordPitchRecords = value;

                if (!recordPitchRecords)
                    pitchRecords = new List<PitchRecord>();
            }
        }

        /// <summary>
        /// Get or set the max number of pitch records to keep. A value of 0 means no limit.
        /// Don't leave this at 0 when RecordPitchRecords is true and this is used in a realtime
        /// application since the buffer will grow indefinately!
        /// </summary>
        public int PitchRecordHistorySize
        {
            get { return pitchRecordHistorySize; }
            set 
            { 
                pitchRecordHistorySize = value;

                pitchRecords.Capacity = pitchRecordHistorySize;
            }
        }

        /// <summary>
        /// Get the current pitch records
        /// </summary>
        public IList PitchRecords
        {
            get { return pitchRecords.AsReadOnly(); }
        }

        /// <summary>
        /// Get the latest pitch record
        /// </summary>
        public PitchRecord CurrentPitchRecord
        {
            get { return curPitchRecord; }
        }

        /// <summary>
        /// Get the current pitch position
        /// </summary>
        public long CurrentPitchSamplePosition
        {
            get { return curPitchSamplePos; }
        }

        /// <summary>
        /// Get the minimum frequency that can be detected
        /// </summary>
        public static float MinDetectFrequency
        {
            get { return kMinFreq; }
        }

        /// <summary>
        /// Get the maximum frequency that can be detected
        /// </summary>
        public static float MaxDetectFrequency
        {
            get { return kMaxFreq; }
        }

        /// <summary>
        /// Get the frequency step
        /// </summary>
        public static double FrequencyStep
        {
            get { return Math.Pow(2.0, 1.0 / kOctaveSteps); }
        }

        /// <summary>
        /// Get the number of samples that the detected pitch is offset from the input buffer.
        /// This is just an estimate to sync up the samples and detected pitch
        /// </summary>
        public int DetectSampleOffset
        {
            get { return (pitchBufSize + detectOverlapSamples) / 2; }
        }

        /// <summary>
        /// Reset the pitch tracker. Call this when the sample position is
        /// not consecutive from the previous position
        /// </summary>
        public void Reset()
        {
            curPitchIndex = 0;
            curPitchSamplePos = 0;
            pitchRecords.Clear();
            iirFilterLoLo.Reset();
            iirFilterLoHi.Reset();
            iirFilterHiLo.Reset();
            iirFilterHiHi.Reset();
            circularBufferLo.Reset();
            circularBufferLo.Clear();
            circularBufferHi.Reset();
            circularBufferHi.Clear();
            pitchBufLo.Clear();
            pitchBufHi.Clear();

            circularBufferLo.StartPosition = -detectOverlapSamples;
            circularBufferLo.Available = detectOverlapSamples;
            circularBufferHi.StartPosition = -detectOverlapSamples;
            circularBufferHi.Available = detectOverlapSamples;
        }

        /// <summary>
        /// Process the passed in buffer of data. During this call, the PitchDetected event will
        /// be fired zero or more times, depending how many pitch records will fit in the new
        /// and previously cached buffer.
        ///
        /// This means that there is no size restriction on the buffer that is passed into ProcessBuffer.
        /// For instance, ProcessBuffer can be called with one very large buffer that contains all of the
        /// audio to be processed (many PitchDetected events will be fired), or just a small buffer at
        /// a time which is more typical for realtime applications. In the latter case, the PitchDetected
        /// event might not be fired at all since additional calls must first be made to accumulate enough
        /// data do another pitch detect operation.
        /// </summary>
        /// <param name="inBuffer">Input buffer. Samples must be in the range -1.0 to 1.0</param>
        /// <param name="sampleCount">Number of samples to process. Zero means all samples in the buffer</param>
        public void ProcessBuffer(float[] inBuffer, int sampleCount = 0)
        {
            if (inBuffer == null)
                throw new ArgumentNullException("inBuffer", "Input buffer cannot be null");

            var samplesProcessed = 0;
            var srcLength = sampleCount == 0 ? inBuffer.Length : Math.Min(sampleCount, inBuffer.Length);

            while (samplesProcessed < srcLength)
            {
                int frameCount = Math.Min(srcLength - samplesProcessed, pitchBufSize + detectOverlapSamples);

                iirFilterLoLo.FilterBuffer(inBuffer, samplesProcessed, pitchBufLo, 0, frameCount);
                iirFilterLoHi.FilterBuffer(pitchBufLo, 0, pitchBufLo, 0, frameCount);

                iirFilterHiLo.FilterBuffer(inBuffer, samplesProcessed, pitchBufHi, 0, frameCount);
                iirFilterHiHi.FilterBuffer(pitchBufHi, 0, pitchBufHi, 0, frameCount);

                circularBufferLo.WriteBuffer(pitchBufLo, frameCount);
                circularBufferHi.WriteBuffer(pitchBufHi, frameCount);

                // Loop while there is enough samples in the circular buffer
                while (circularBufferLo.ReadBuffer(pitchBufLo, curPitchSamplePos, pitchBufSize + detectOverlapSamples))
                {
                    float pitch1;
                    float pitch2 = 0.0f;
                    float detectedPitch = 0.0f;

                    circularBufferHi.ReadBuffer(pitchBufHi, curPitchSamplePos, pitchBufSize + detectOverlapSamples);

                    pitch1 = dsp.DetectPitch(pitchBufLo, pitchBufHi, pitchBufSize);

                    if (pitch1 > 0.0f)
                    {
                        // Shift the buffers left by the overlapping amount
                        pitchBufLo.Copy(pitchBufLo, detectOverlapSamples, 0, pitchBufSize);
                        pitchBufHi.Copy(pitchBufHi, detectOverlapSamples, 0, pitchBufSize);

                        pitch2 = dsp.DetectPitch(pitchBufLo, pitchBufHi, pitchBufSize);

                        if (pitch2 > 0.0f)
                        {
                            float fDiff = Math.Max(pitch1, pitch2) / Math.Min(pitch1, pitch2) - 1.0f;

                            if (fDiff < maxOverlapDiff)
                                detectedPitch = (pitch1 + pitch2) * 0.5f;
                        }
                    }

                    // Log the pitch record
                    AddPitchRecord(detectedPitch);

                    curPitchSamplePos += samplesPerPitchBlock;
                    curPitchIndex++;
                }

                samplesProcessed += frameCount;
            }
        }

        /// <summary>
        /// Setup
        /// </summary>
        private void Setup()
        {
            if (sampleRate < 1.0f)
                return;

            dsp = new PitchDsp(sampleRate, kMinFreq, kMaxFreq, detectLevelThreshold);

            iirFilterLoLo = new IIRFilter();
            iirFilterLoLo.Proto = IIRFilter.ProtoType.Butterworth;
            iirFilterLoLo.Type = IIRFilter.FilterType.HP;
            iirFilterLoLo.Order = 5;
            iirFilterLoLo.FreqLow = 45.0f;
            iirFilterLoLo.SampleRate = (float)sampleRate;

            iirFilterLoHi = new IIRFilter();
            iirFilterLoHi.Proto = IIRFilter.ProtoType.Butterworth;
            iirFilterLoHi.Type = IIRFilter.FilterType.LP;
            iirFilterLoHi.Order = 5;
            iirFilterLoHi.FreqHigh = 280.0f;
            iirFilterLoHi.SampleRate = (float)sampleRate;

            iirFilterHiLo = new IIRFilter();
            iirFilterHiLo.Proto = IIRFilter.ProtoType.Butterworth;
            iirFilterHiLo.Type = IIRFilter.FilterType.HP;
            iirFilterHiLo.Order = 5;
            iirFilterHiLo.FreqLow = 45.0f;
            iirFilterHiLo.SampleRate = (float)sampleRate;

            iirFilterHiHi = new IIRFilter();
            iirFilterHiHi.Proto = IIRFilter.ProtoType.Butterworth;
            iirFilterHiHi.Type = IIRFilter.FilterType.LP;
            iirFilterHiHi.Order = 5;
            iirFilterHiHi.FreqHigh = 1500.0f;
            iirFilterHiHi.SampleRate = (float)sampleRate;

            detectOverlapSamples = (int)(kDetectOverlapSec * sampleRate);
            maxOverlapDiff = kMaxOctaveSecRate * kDetectOverlapSec;

            pitchBufSize = (int)(((1.0f / (float)kMinFreq) * 2.0f + ((kAvgCount - 1) * kAvgOffset)) * sampleRate) + 16;
            pitchBufLo = new float[pitchBufSize + detectOverlapSamples];
            pitchBufHi = new float[pitchBufSize + detectOverlapSamples];
            samplesPerPitchBlock = (int)Math.Round(sampleRate / pitchRecordsPerSecond); 

            circularBufferLo = new CircularBuffer<float>((int)(kCircularBufSaveTime * sampleRate + 0.5f) + 10000);
            circularBufferHi = new CircularBuffer<float>((int)(kCircularBufSaveTime * sampleRate + 0.5f) + 10000);
        }

        /// <summary>
        /// The pitch was detected - add the record
        /// </summary>
        /// <param name="pitch"></param>
        private void AddPitchRecord(float pitch)
        {
            var midiNote = 0;
            var midiCents = 0;

            PitchDsp.PitchToMidiNote(pitch, out midiNote, out midiCents);

            var record = new PitchRecord();

            record.RecordIndex = curPitchIndex;
            record.Pitch = pitch;
            record.MidiNote = midiNote;
            record.MidiCents = midiCents;

            curPitchRecord = record;

            if (recordPitchRecords)
            {
                if (pitchRecordHistorySize > 0 && pitchRecords.Count >= pitchRecordHistorySize)
                    pitchRecords.RemoveAt(0);

                pitchRecords.Add(record);
            }
            
            if (this.PitchDetected != null)
                this.PitchDetected(this, record);
        }

        /// <summary>
        /// Stores one record
        /// </summary>
        public struct PitchRecord
        {
            /// <summary>
            /// The index of the pitch record since the last Reset call
            /// </summary>
            public int RecordIndex { get; set; }

            /// <summary>
            /// The detected pitch
            /// </summary>
            public float Pitch { get; set; }

            /// <summary>
            /// The detected MIDI note, or 0 for no pitch
            /// </summary>
            public int MidiNote { get; set; }

            /// <summary>
            /// The offset from the detected MIDI note in cents, from -50 to +50.
            /// </summary>
            public int MidiCents { get; set; }
        }
    }
}

