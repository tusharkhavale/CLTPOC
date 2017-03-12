using UnityEngine;
using System;
using System.Collections.Generic;
using Pitch;
using UnityEngine.UI;

namespace UnityPitchControl.Input {
	public sealed class InputManager : MonoBehaviour {
		private static InputManager instance;
		public String audioDevice = ""; // name of the audio device
		public AudioClip micInput;
		public float[] samples;
		public PitchTracker pitchTracker;
		public List<int> detectedPitches;
		public float spectralPitch;
		public Text txtFrequency;
		public Text txtPitch;
		AudioSource audioPlayer;
		int sampleRate = 44000;      // Not sure if 44000 works on device so usiing AudioSettings.outputSampleRate on line 27
		int binSize = 1024;
		float[] harmonics;
		bool isPlaying;
		float[] spectrumData;



		/// <summary>
		/// Callback for start button
		/// Starts the recording.
		/// start Mic and assign clip to audio source
		/// initialize PitchTracker class
		/// </summary>
		public void StartRecording() 
		{
			instance = this;
			// Start recording

			// Not dependable GetDeviceCaps
			int minFreq, maxFreq;
			Microphone.GetDeviceCaps(audioDevice, out minFreq, out maxFreq);
			if (minFreq > 0) micInput = Microphone.Start(audioDevice, true, 1, minFreq);
			micInput = Microphone.Start(audioDevice, true, 1, sampleRate);
			audioPlayer = GetComponent<AudioSource> ();
			audioPlayer.clip = micInput;
			audioPlayer.Play ();

			// prepare for pitch tracking
			samples = new float[micInput.samples * micInput.channels];
			pitchTracker = new PitchTracker();
			pitchTracker.SampleRate = micInput.samples;
			pitchTracker.PitchDetected += new PitchTracker.PitchDetectedHandler(PitchDetectedListener);
			spectrumData = new float[binSize];
			isPlaying = true;
			AnalyticsManager.GetInstance ().SetStartRecordingTime ();
		}

		/// <summary>
		/// Get output data for pitch detection
		/// GetSpectrum Data for spectrum analysis
		/// Update Spectrum visualizer
		/// </summary>
		public void Update() 
		{
			// Game not startted or paused
			if (!isPlaying)
				return;

			detectedPitches.Clear(); // clear pitches from last update
			micInput.GetData(samples, 0);
			audioPlayer.GetSpectrumData (spectrumData, 0, FFTWindow.BlackmanHarris);
			FindPeakHarmonic ();
			AudioVisualizer.GetInstance ().UpdateVisualizer (spectrumData);
			pitchTracker.ProcessBuffer(samples);
				
		}

		/// <summary>
		/// Finds the peak harmonic to detect pitch
		/// Works for frequencies above 3K
		/// </summary>
		void FindPeakHarmonic()
		{
			float bin = 0;
			int index = 0;
			for (int i = 0; i < spectrumData.Length; i++) 
			{
				if (spectrumData [i] > bin) 
				{
					bin = spectrumData [i];
					index = i;
				}
			}

			if (bin > 0.0009)
				AnalyticsManager.GetInstance().AudioInputDetected();

			float maxV = spectrumData[index];
			int maxN = index;
			float freqN = maxN; // pass the index to a float variable
			if (maxN > 0 && maxN < binSize - 1)
			{ // interpolate index using neighbours
				var dL = spectrumData[maxN - 1] / spectrumData[maxN];
				var dR = spectrumData[maxN + 1] / spectrumData[maxN];
				freqN += 0.5f * (dR * dR - dL * dL);
			}
		
			spectralPitch = freqN * (sampleRate / 2f) / binSize;
		}

		/// <summary>
		/// Stops the recording.
		/// Toggle isPlaying 
		/// </summary>
		public void StopRecording()
		{
			isPlaying = false;
			Microphone.End(audioDevice);
			AnalyticsManager.GetInstance ().EndRecordingTime();
		}

		/// <summary>
		/// Callback for End button
		/// UI transition to GameSelection Page
		/// </summary>
		public void EndGame()
		{
			StopRecording ();
			pitchTracker = null;
			GameController.gameController.uiManager.UIScreenTransition (EScreen.SelectGame);
		}

		/// <summary>
		/// Pitchs detected delegate.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="pitchRecord">Pitch record.</param>
		private void PitchDetectedListener(PitchTracker sender, PitchTracker.PitchRecord pitchRecord) 
		{
			int pitch = (int)Math.Round(pitchRecord.Pitch);
			if (!detectedPitches.Contains(pitch)) detectedPitches.Add(pitch);

			int lowestPitch = pitch;
			foreach(int p in detectedPitches)
			{
				if (p > 0 && p < lowestPitch)
					lowestPitch = p;
			}

			// Check against spectral pitch
			if(lowestPitch == 0)
			lowestPitch = spectralPitch > 3000 ? (int)spectralPitch : lowestPitch; 

			// Render pitch and Frequency on screen
			txtFrequency.text = lowestPitch +" Hz";
			txtPitch.text = FrequencyMapping.GetInstance().GetNote(lowestPitch);
				
			// calculate fundamental frequency bin
			float freqN = lowestPitch * binSize*2f/sampleRate;
			int index = (int)freqN;     // Not using Matf.RoundToInt because lower int value required and not nearest
			if (pitch != 0)
				ChakraLongTone.GetInstance ().UpdateChakras (GetHarmoicsAmplitude (spectrumData, index, 0, 7));  
			else
				ChakraLongTone.GetInstance ().NormalizeChakras ();		
		}


		/// <summary>
		/// Gets the harmoics amplitude.
		/// </summary>
		/// <returns>The harmoics amplitude.</returns>
		/// <param name="spectrum">Spectrum.</param>
		/// <param name="peakBin">Peak bin.</param>
		/// <param name="windowHalfLen">Window half length.</param>
		/// <param name="harmonic">Harmonic.</param>
		float[] GetHarmoicsAmplitude(float[] spectrum,int peakBin, int windowHalfLen, int harmonic)
		{
			float[] harmonicsData = new float[harmonic];
			for (int i = 1; i <= harmonicsData.Length; i++) 
			{
				double sumOfSquares = 0.0;
				int binValue = peakBin * i; 

				for (int bin = binValue-windowHalfLen; bin <= binValue+windowHalfLen; bin++)
				{
					if (bin < spectrum.Length && bin > 0) 
					{
						sumOfSquares += spectrum[bin] * spectrum[bin];
					}
				}

				harmonicsData[i-1] = Mathf.Sqrt((float)sumOfSquares);	
			}
			return harmonicsData;
		}
	}
}