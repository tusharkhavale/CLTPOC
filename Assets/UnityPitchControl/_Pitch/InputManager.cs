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
		public Text pitch;
		
		private static InputManager _instance;
		private void Awake() {
			_instance = UnityEngine.Object.FindObjectOfType(typeof(InputManager)) as InputManager;
			if (_instance == null) {
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
			else _micInput = Microphone.Start(_audioDevice, true, 1, 44000);

			// prepare for pitch tracking
			_samples = new float[_micInput.samples * _micInput.channels];
			_pitchTracker = new PitchTracker();
			_pitchTracker.SampleRate = _micInput.samples;
			_pitchTracker.PitchDetected += new PitchTracker.PitchDetectedHandler(PitchDetectedListener);
		}
		
		public void Update() {
			_detectedPitches.Clear(); // clear pitches from last update
			_micInput.GetData(_samples, 0);
			_pitchTracker.ProcessBuffer(_samples);
			if (_detectedPitches.Count > 0)
				pitch.text = ""+ _detectedPitches [0];
		}

		private void PitchDetectedListener(PitchTracker sender, PitchTracker.PitchRecord pitchRecord) {
			int pitch = (int)Math.Round(pitchRecord.Pitch);
			if (!_detectedPitches.Contains(pitch)) _detectedPitches.Add(pitch);
		}
	}
}