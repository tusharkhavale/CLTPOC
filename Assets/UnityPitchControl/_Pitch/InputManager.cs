using UnityEngine;
using System;
using System.Collections.Generic;
using Pitch;
using UnityEngine.UI;

namespace UnityPitchControl.Input {
	public sealed class InputManager : MonoBehaviour {

		public String _audioDevice = ""; // name of the audio device
		public AudioClip _micInput;
		public float[] _samples;
		public PitchTracker _pitchTracker;
		public List<int> _detectedPitches;
		public Text txtPitch;
		AudioSource audioPlayer;
		int sampleRate = 44000;      // Not sure if 44000 works on device so usiing AudioSettings.outputSampleRate on line 27
		int binSize = 1024;
		float[] harmonics;

		private static InputManager _instance;
		void Start() 
		{
			_instance = UnityEngine.Object.FindObjectOfType(typeof(InputManager)) as InputManager;
			if (_instance == null) 
			{
				sampleRate = AudioSettings.outputSampleRate;
				// try to load prefab
				UnityEngine.Object managerPrefab = Resources.Load("InputManager"); // looks inside all 'Resources' folders in 'Assets'
				if (managerPrefab != null) {
					UnityEngine.Object prefab = Instantiate(managerPrefab);
					prefab.name = "InputManager"; // otherwise creates a game object with "(Clone)" appended to the name
				} else if (UnityEngine.Object.FindObjectOfType(typeof(InputManager)) == null) {
					// no prefab found, create new input manager
					GameObject gameObject = new GameObject("InputManager");
					gameObject.AddComponent<InputManager>();
					DontDestroyOnLoad(gameObject);
					gameObject.hideFlags = HideFlags.HideInHierarchy;
				}
				_instance = UnityEngine.Object.FindObjectOfType(typeof(InputManager)) as InputManager;
			}

				// start recording
				int minFreq, maxFreq;
				Microphone.GetDeviceCaps(_audioDevice, out minFreq, out maxFreq);
				if (minFreq > 0) _micInput = Microphone.Start(_audioDevice, true, 1, minFreq);
				else _micInput = Microphone.Start(_audioDevice, true, 1, sampleRate);
				audioPlayer = GetComponent<AudioSource> ();
				audioPlayer.clip = _micInput;
				audioPlayer.Play ();

				// prepare for pitch tracking
				_samples = new float[_micInput.samples * _micInput.channels];
				_pitchTracker = new PitchTracker();
				_pitchTracker.SampleRate = _micInput.samples;
				_pitchTracker.PitchDetected += new PitchTracker.PitchDetectedHandler(PitchDetectedListener);
				spectrumData = new float[binSize];
		}

		float[] spectrumData;
		public void Update() 
		{
			_detectedPitches.Clear(); // clear pitches from last update
			_micInput.GetData(_samples, 0);
			audioPlayer.GetSpectrumData (spectrumData, 0, FFTWindow.BlackmanHarris);
//			AudioVisualizer.GetInstance ().UpdateVisualizer (spectrumData);
			_pitchTracker.ProcessBuffer(_samples);
				
		}

		private void PitchDetectedListener(PitchTracker sender, PitchTracker.PitchRecord pitchRecord) 
		{
			int pitch = (int)Math.Round(pitchRecord.Pitch);
			if (!_detectedPitches.Contains(pitch)) _detectedPitches.Add(pitch);

			int lowestPitch = pitch;
			foreach(int p in _detectedPitches)
			{
				if (p > 0 && p < lowestPitch)
					lowestPitch = p;
			}
			txtPitch.text = lowestPitch +" Hz";

			float freqN = lowestPitch * binSize*2f/sampleRate;
			int index = (int)freqN;     // Not using Matf.RoundToInt because lower int value required and not nearest
			if (pitch != 0)
				ChakraLongTone.GetInstance ().UpdateChakras (GetHarmoicsAmplitude (spectrumData, index, 0, 7));
			else
				ChakraLongTone.GetInstance ().NormalizeChakras ();		
		}

		float[] GetHarmoicsAmplitude(float[] spectrum,int peakBin, int windowHalfLen, int harmonic)
		{
			float[] harmonicsData = new float[harmonic];
			for (int i = 1; i <= harmonicsData.Length; i++) 
			{
				double sumOfSquares = 0.0;
				int binValue = peakBin * i; 

				Debug.Log (i + "th harmonic amplitude : " + spectrum [binValue]);
				for (int bin = binValue-windowHalfLen; bin <= binValue+windowHalfLen; bin++)
				{
					if (bin < spectrum.Length && bin > 0) 
					{
						sumOfSquares += spectrum[bin] * spectrum[bin];
					}
				}

				harmonicsData[i-1] = Mathf.Sqrt((float)sumOfSquares);	
				//			return Mathf.Sqrt((float)sumOfSquares);	
			}
			return harmonicsData;
		}
	}
}