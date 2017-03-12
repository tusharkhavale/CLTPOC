using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pitch
{
    /// <summary>
    /// Infinite impulse response filter (old style analog filters)
    /// </summary>
    class IIRFilter
    {
        /// <summary>
        /// The type of filter
        /// </summary>
        public enum FilterType
        {
            None = 0,
            LP,
            HP,
            BP
        }

        /// <summary>
        /// The filter prototype
        /// </summary>
        public enum ProtoType
        {
            None = 0,
            Butterworth,
            Chebyshev,
        }

        const int kHistMask = 31;
        const int kHistSize = 32;

        private int order;
        private ProtoType protoType;
        private FilterType filterType;

        private float fp1;
        private float fp2;
        private float fN;
        private float ripple;
        private float sampleRate;
        private double[] real;
        private double[] imag;
        private double[] z;
        private double[] aCoeff;
        private double[] bCoeff;
        private double[] inHistory;
        private double[] outHistory;
        private int histIdx;
        private bool invertDenormal;

        public IIRFilter()
        {
        }

        /// <summary>
        /// Returns true if all the filter parameters are valid
        /// </summary>
        public bool FilterValid
        {
            get
            {
                if (order < 1 || order > 16 ||
                    protoType == ProtoType.None ||
                    filterType == FilterType.None ||
                    sampleRate <= 0.0f ||
                    fN <= 0.0f)
                    return false;

                switch (filterType)
                {
                    case FilterType.LP:
                        if (fp2 <= 0.0f)
                            return false;
                        break;

                    case FilterType.BP:
                        if (fp1 <= 0.0f || fp2 <= 0.0f || fp1 >= fp2)
                            return false;
                        break;

                    case FilterType.HP:
                        if (fp1 <= 0.0f)
                            return false;
                        break;
                }

                // For bandpass, the order must be even
                if (filterType == FilterType.BP && (order & 1) != 0)
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Set the filter prototype
        /// </summary>
        public ProtoType Proto
        {
            get { return protoType; }

            set
            {
                protoType = value;
                Design();
            }
        }

        /// <summary>
        /// Set the filter type
        /// </summary>
        public FilterType Type
        {
            get { return filterType; }

            set
            {
                filterType = value;
                Design();
            }
        }

        public int Order
        {
            get { return order; }

            set
            {
                order = Math.Min(16, Math.Max(1, Math.Abs(value)));

                if (filterType == FilterType.BP && Odd(order))
                    order++;

                Design();
            }
        }

        public float SampleRate
        {
            get { return sampleRate; }

            set
            {
                sampleRate = value;
                fN = 0.5f * sampleRate;
                Design();
            }
        }

        public float FreqLow
        {
            get { return fp1; }

            set
            {
                fp1 = value;
                Design();
            }
        }

        public float FreqHigh
        {
            get { return fp2; }

            set
            {
                fp2 = value;
                Design();
            }
        }

        public float Ripple
        {
            get { return ripple; }

            set
            {
                ripple = value;
                Design();
            }
        }

        /// <summary>
        /// Returns true if n is odd
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private bool Odd(int n)
        {
            return (n & 1) == 1;
        }

        /// <summary>
        /// Square
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        private float Sqr(float value)
        {
            return value * value;
        }

        /// <summary>
        /// Square
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        private double Sqr(double value)
        {
            return value * value;
        }

        /// <summary>
        /// Determines poles and zeros of IIR filter
        /// based on bilinear transform method
        /// </summary>
        private void LocatePolesAndZeros()
        {
            real = new double[order + 1];
            imag = new double[order + 1];
            z = new double[order + 1];
            double ln10 = Math.Log(10.0);

            // Butterworth, Chebyshev parameters
            int n = order;

            if (filterType == FilterType.BP)
                n = n / 2;

            int ir = n % 2;
            int n1 = n + ir;
            int n2 = (3 * n + ir) / 2 - 1;
            double f1;

            switch (filterType)
            {
                case FilterType.LP:
                    f1 = fp2;
                    break;

                case FilterType.HP:
                    f1 = fN - fp1;
                    break;

                case FilterType.BP:
                    f1 = fp2 - fp1;
                    break;

                default:
                    f1 = 0.0;
                    break;
            }

            double tanw1 = Math.Tan(0.5 * Math.PI * f1 / fN);
            double tansqw1 = Sqr(tanw1);

            // Real and Imaginary parts of low-pass poles
            double t, a = 1.0, r = 1.0, i = 1.0;

            for (int k = n1; k <= n2; k++)
            {
                t = 0.5 * (2 * k + 1 - ir) * Math.PI / (double)n;

                switch (protoType)
                {
                    case ProtoType.Butterworth:
                        double b3 = 1.0 - 2.0 * tanw1 * Math.Cos(t) + tansqw1;
                        r = (1.0 - tansqw1) / b3;
                        i = 2.0 * tanw1 * Math.Sin(t) / b3;
                        break;

                    case ProtoType.Chebyshev:
                        double d = 1.0 - Math.Exp(-0.05 * ripple * ln10);
                        double e = 1.0 / Math.Sqrt(1.0 / Sqr(1.0 - d) - 1.0);
                        double x = Math.Pow(Math.Sqrt(e * e + 1.0) + e, 1.0 / (double)n);
                        a = 0.5 * (x - 1.0 / x);
                        double b = 0.5 * (x + 1.0 / x);
                        double c3 = a * tanw1 * Math.Cos(t);
                        double c4 = b * tanw1 * Math.Sin(t);
                        double c5 = Sqr(1.0 - c3) + Sqr(c4);
                        r = 2.0 * (1.0 - c3) / c5 - 1.0;
                        i = 2.0 * c4 / c5;
                        break;
                }

                int m = 2 * (n2 - k) + 1;
                real[m + ir] = r;
                imag[m + ir] = Math.Abs(i);
                real[m + ir + 1] = r;
                imag[m + ir + 1] = -Math.Abs(i);
            }

            if (Odd(n))
            {
                if (protoType == ProtoType.Butterworth)
                    r = (1.0 - tansqw1) / (1.0 + 2.0 * tanw1 + tansqw1);

                if (protoType == ProtoType.Chebyshev)
                    r = 2.0 / (1.0 + a * tanw1) - 1.0;

                real[1] = r;
                imag[1] = 0.0;
            }

            switch (filterType)
            {
                case FilterType.LP:
                    for (int m = 1; m <= n; m++)
                        z[m] = -1.0;
                    break;

                case FilterType.HP:
                    // Low-pass to high-pass transformation
                    for (int m = 1; m <= n; m++)
                    {
                        real[m] = -real[m];
                        z[m] = 1.0;
                    }
                    break;

                case FilterType.BP:
                    // Low-pass to bandpass transformation
                    for (int m = 1; m <= n; m++)
                    {
                        z[m] = 1.0;
                        z[m + n] = -1.0;
                    }

                    double f4 = 0.5 * Math.PI * fp1 / fN;
                    double f5 = 0.5 * Math.PI * fp2 / fN;
                    double aa = Math.Cos(f4 + f5) / Math.Cos(f5 - f4);
                    double aR, aI, h1, h2, p1R, p2R, p1I, p2I;

                    for (int m1 = 0; m1 <= (order - 1) / 2; m1++)
                    {
                        int m = 1 + 2 * m1;
                        aR = real[m];
                        aI = imag[m];

                        if (Math.Abs(aI) < 0.0001)
                        {
                            h1 = 0.5 * aa * (1.0 + aR);
                            h2 = Sqr(h1) - aR;
                            if (h2 > 0.0)
                            {
                                p1R = h1 + Math.Sqrt(h2);
                                p2R = h1 - Math.Sqrt(h2);
                                p1I = 0.0;
                                p2I = 0.0;
                            }
                            else
                            {
                                p1R = h1;
                                p2R = h1;
                                p1I = Math.Sqrt(Math.Abs(h2));
                                p2I = -p1I;
                            }
                        }
                        else
                        {
                            double fR = aa * 0.5 * (1.0 + aR);
                            double fI = aa * 0.5 * aI;
                            double gR = Sqr(fR) - Sqr(fI) - aR;
                            double gI = 2 * fR * fI - aI;
                            double sR = Math.Sqrt(0.5 * Math.Abs(gR + Math.Sqrt(Sqr(gR) + Sqr(gI))));
                            double sI = gI / (2.0 * sR);
                            p1R = fR + sR;
                            p1I = fI + sI;
                            p2R = fR - sR;
                            p2I = fI - sI;
                        }

                        real[m] = p1R;
                        real[m + 1] = p2R;
                        imag[m] = p1I;
                        imag[m + 1] = p2I;
                    }

                    if (Odd(n))
                    {
                        real[2] = real[n + 1];
                        imag[2] = imag[n + 1];
                    }

                    for (int k = n; k >= 1; k--)
                    {
                        int m = 2 * k - 1;
                        real[m] = real[k];
                        real[m + 1] = real[k];
                        imag[m] = Math.Abs(imag[k]);
                        imag[m + 1] = -Math.Abs(imag[k]);
                    }

                    break;
            }
        }

        /// <summary>
        /// Calculate all the values
        /// </summary>
        public void Design()
        {
            if (!this.FilterValid)
                return;

            aCoeff = new double[order + 1];
            bCoeff = new double[order + 1];
            inHistory = new double[kHistSize];
            outHistory = new double[kHistSize];

            double[] newA = new double[order + 1];
            double[] newB = new double[order + 1];

            // Find filter poles and zeros
            LocatePolesAndZeros();

            // Compute filter coefficients from pole/zero values
            aCoeff[0] = 1.0;
            bCoeff[0] = 1.0;

            for (int i = 1; i <= order; i++)
            {
                aCoeff[i] = 0.0;
                bCoeff[i] = 0.0;
            }

            int k = 0;
            int n = order;
            int pairs = n / 2;

            if (Odd(order))
            {
                // First subfilter is first order
                aCoeff[1] = -z[1];
                bCoeff[1] = -real[1];
                k = 1;
            }

            for (int p = 1; p <= pairs; p++)
            {
                int m = 2 * p - 1 + k;
                double alpha1 = -(z[m] + z[m + 1]);
                double alpha2 = z[m] * z[m + 1];
                double beta1 = -2.0 * real[m];
                double beta2 = Sqr(real[m]) + Sqr(imag[m]);

                newA[1] = aCoeff[1] + alpha1 * aCoeff[0];
                newB[1] = bCoeff[1] + beta1 * bCoeff[0];

                for (int i = 2; i <= n; i++)
                {
                    newA[i] = aCoeff[i] + alpha1 * aCoeff[i - 1] + alpha2 * aCoeff[i - 2];
                    newB[i] = bCoeff[i] + beta1 * bCoeff[i - 1] + beta2 * bCoeff[i - 2];
                }

                for (int i = 1; i <= n; i++)
                {
                    aCoeff[i] = newA[i];
                    bCoeff[i] = newB[i];
                }
            }

            // Ensure the filter is normalized
            FilterGain(1000);
        }

        /// <summary>
        /// Reset the history buffers
        /// </summary>
        public void Reset()
        {
            if (inHistory != null)
                inHistory.Clear();

            if (outHistory != null)
                outHistory.Clear();

            histIdx = 0;
        }

        /// <summary>
        /// Reset the filter, and fill the appropriate history buffers with the specified value
        /// </summary>
        /// <param name="historyValue"></param>
        public void Reset(double startValue)
        {
            histIdx = 0;

            if (inHistory == null || outHistory == null)
                return;

            inHistory.Fill(startValue);

            if (inHistory != null)
            {
                switch (filterType)
                {
                    case FilterType.LP:
                        outHistory.Fill(startValue);
                        break;

                    default:
                        outHistory.Clear();
                        break;
                }
            }
        }

        /// <summary>
        /// Apply the filter to the buffer
        /// </summary>
        /// <param name="bufIn"></param>
        public void FilterBuffer(float[] srcBuf, long srcPos, float[] dstBuf, long dstPos, long nLen)
        {
            const double kDenormal = 0.000000000000001;
            double denormal = invertDenormal ? -kDenormal : kDenormal;
            invertDenormal = !invertDenormal;

            for (int sampleIdx = 0; sampleIdx < nLen; sampleIdx++)
            {
                double sum = 0.0f;

                inHistory[histIdx] = srcBuf[srcPos + sampleIdx] + denormal;

                for (int idx = 0; idx < aCoeff.Length; idx++)
                    sum += aCoeff[idx] * inHistory[(histIdx - idx) & kHistMask];

                for (int idx = 1; idx < bCoeff.Length; idx++)
                    sum -= bCoeff[idx] * outHistory[(histIdx - idx) & kHistMask];

                outHistory[histIdx] = sum;
                histIdx = (histIdx + 1) & kHistMask;
                dstBuf[dstPos + sampleIdx] = (float)sum;
            }
        }

        public float FilterSample(float inVal)
        {
            double sum = 0.0f;

            inHistory[histIdx] = inVal;

            for (int idx = 0; idx < aCoeff.Length; idx++)
                sum += aCoeff[idx] * inHistory[(histIdx - idx) & kHistMask];

            for (int idx = 1; idx < bCoeff.Length; idx++)
                sum -= bCoeff[idx] * outHistory[(histIdx - idx) & kHistMask];

            outHistory[histIdx] = sum;
            histIdx = (histIdx + 1) & kHistMask;

            return (float)sum;
        }

        /// <summary>
        /// Get the gain at the specified number of frequency points
        /// </summary>
        /// <param name="freqPoints"></param>
        /// <returns></returns>
        public float[] FilterGain(int freqPoints)
        {
            // Filter gain at uniform frequency intervals
            float[] g = new float[freqPoints];
            double theta, s, c, sac, sas, sbc, sbs;
            float gMax = -100.0f;
            float sc = 10.0f / (float)Math.Log(10.0f);
            double t = Math.PI / (freqPoints - 1);

            for (int i = 0; i < freqPoints; i++)
            {
                theta = i * t;

                if (i == 0)
                    theta = Math.PI * 0.0001;

                if (i == freqPoints - 1)
                    theta = Math.PI * 0.9999;

                sac = 0.0f;
                sas = 0.0f;
                sbc = 0.0f;
                sbs = 0.0f;

                for (int k = 0; k <= order; k++)
                {
                    c = Math.Cos(k * theta);
                    s = Math.Sin(k * theta);
                    sac += c * aCoeff[k];
                    sas += s * aCoeff[k];
                    sbc += c * bCoeff[k];
                    sbs += s * bCoeff[k];
                }

                g[i] = sc * (float)Math.Log((Sqr(sac) + Sqr(sas)) / (Sqr(sbc) + Sqr(sbs)));
                gMax = Math.Max(gMax, g[i]);
            }

            // Normalize to 0 dB maximum gain
            for (int i = 0; i < freqPoints; i++)
                g[i] -= gMax;

            // Normalize numerator (a) coefficients
            float normFactor = (float)Math.Pow(10.0, -0.05 * gMax);

            for (int i = 0; i <= order; i++)
                aCoeff[i] *= normFactor;

            return g;
        }
    }
}
