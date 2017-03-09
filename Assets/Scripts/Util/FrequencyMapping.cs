using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrequencyMapping : MonoBehaviour {

	private static FrequencyMapping instance;
	private float[,] octave = {
								{ 32.7f, 34.6f, 36.7f, 38.9f, 41.2f, 43.7f, 46.2f, 49.0f, 51.9f, 55.0f, 58.3f, 61.7f },
								{ 65.4f, 69.3f, 73.4f, 77.8f, 82.4f, 87.3f, 92.5f, 98.0f, 103.8f, 110.0f, 116.5f, 123.5f },
								{ 130.8f, 138.6f, 146.8f, 155.6f, 164.8f, 174.6f, 185.0f, 196.0f, 207.7f, 220.0f, 233.1f, 246.9f },
								{ 261.6f, 277.2f, 293.7f, 311.1f, 329.6f, 349.2f, 370.0f, 392.0f, 415.3f, 440.0f, 466.2f, 493.9f },
								{ 523.3f, 554.4f, 587.3f, 622.3f, 659.3f, 698.5f, 740.0f, 784.0f, 830.6f, 880.0f, 932.3f, 987.8f },
								{ 1046.5f, 1108.7f, 1174.7f, 1244.5f, 1318.5f, 1396.9f, 1480.0f, 1568.0f, 1661.2f, 1760.0f, 1864.7f, 1975.5f },
								{ 2093.0f, 2217.5f, 2349.3f, 2489.0f, 2637.0f, 2793.8f, 2960.0f, 3136.0f, 3322.4f, 3520.0f, 3729.3f, 3951.1f }
							   };
	private float C8 = 4186.0f;
	private Dictionary <float,string> noteLookUpDict = new Dictionary<float,string>();

	public static FrequencyMapping GetInstance()
	{
		if (instance != null) 
		{
			return instance;
		}
		return null;
	}

	void Awake()
	{
		instance = this;
	}

	/// <summary>
	/// Add all frequencies to look up dictionary.
	/// </summary>
	private void Start()
	{
		noteLookUpDict.Add (octave[0,0],"C1");
		noteLookUpDict.Add (octave[0,1],"C#1");
		noteLookUpDict.Add (octave[0,2],"D1");
		noteLookUpDict.Add (octave[0,3],"D#1");
		noteLookUpDict.Add (octave[0,4],"E1");
		noteLookUpDict.Add (octave[0,5],"F1");
		noteLookUpDict.Add (octave[0,6],"F#1");
		noteLookUpDict.Add (octave[0,7],"G1");
		noteLookUpDict.Add (octave[0,8],"G#1");
		noteLookUpDict.Add (octave[0,9],"A1");
		noteLookUpDict.Add (octave[0,10],"A#1");
		noteLookUpDict.Add (octave[0,11],"B1");

		noteLookUpDict.Add (octave[1,0],"C2");
		noteLookUpDict.Add (octave[1,1],"C#2");
		noteLookUpDict.Add (octave[1,2],"D2");
		noteLookUpDict.Add (octave[1,3],"D#2");
		noteLookUpDict.Add (octave[1,4],"E2");
		noteLookUpDict.Add (octave[1,5],"F2");
		noteLookUpDict.Add (octave[1,6],"F#2");
		noteLookUpDict.Add (octave[1,7],"G2");
		noteLookUpDict.Add (octave[1,8],"G#2");
		noteLookUpDict.Add (octave[1,9],"A2");
		noteLookUpDict.Add (octave[1,10],"A#2");
		noteLookUpDict.Add (octave[1,11],"B2");

		noteLookUpDict.Add (octave[2,0],"C3");
		noteLookUpDict.Add (octave[2,1],"C#3");
		noteLookUpDict.Add (octave[2,2],"D3");
		noteLookUpDict.Add (octave[2,3],"D#3");
		noteLookUpDict.Add (octave[2,4],"E3");
		noteLookUpDict.Add (octave[2,5],"F3");
		noteLookUpDict.Add (octave[2,6],"F#3");
		noteLookUpDict.Add (octave[2,7],"G3");
		noteLookUpDict.Add (octave[2,8],"G#3");
		noteLookUpDict.Add (octave[2,9],"A3");
		noteLookUpDict.Add (octave[2,10],"A#3");
		noteLookUpDict.Add (octave[2,11],"B3");

		noteLookUpDict.Add (octave[3,0],"C4");
		noteLookUpDict.Add (octave[3,1],"C#4");
		noteLookUpDict.Add (octave[3,2],"D4");
		noteLookUpDict.Add (octave[3,3],"D#4");
		noteLookUpDict.Add (octave[3,4],"E4");
		noteLookUpDict.Add (octave[3,5],"F4");
		noteLookUpDict.Add (octave[3,6],"F#4");
		noteLookUpDict.Add (octave[3,7],"G4");
		noteLookUpDict.Add (octave[3,8],"G#4");
		noteLookUpDict.Add (octave[3,9],"A4");
		noteLookUpDict.Add (octave[3,10],"A#4");
		noteLookUpDict.Add (octave[3,11],"B4");

		noteLookUpDict.Add (octave[4,0],"C5");
		noteLookUpDict.Add (octave[4,1],"C#5");
		noteLookUpDict.Add (octave[4,2],"D5");
		noteLookUpDict.Add (octave[4,3],"D#5");
		noteLookUpDict.Add (octave[4,4],"E5");
		noteLookUpDict.Add (octave[4,5],"F5");
		noteLookUpDict.Add (octave[4,6],"F#5");
		noteLookUpDict.Add (octave[4,7],"G5");
		noteLookUpDict.Add (octave[4,8],"G#5");
		noteLookUpDict.Add (octave[4,9],"A5");
		noteLookUpDict.Add (octave[4,10],"A#5");
		noteLookUpDict.Add (octave[4,11],"B5");

		noteLookUpDict.Add (octave[5,0],"C6");
		noteLookUpDict.Add (octave[5,1],"C#6");
		noteLookUpDict.Add (octave[5,2],"D6");
		noteLookUpDict.Add (octave[5,3],"D#6");
		noteLookUpDict.Add (octave[5,4],"E6");
		noteLookUpDict.Add (octave[5,5],"F6");
		noteLookUpDict.Add (octave[5,6],"F#6");
		noteLookUpDict.Add (octave[5,7],"G6");
		noteLookUpDict.Add (octave[5,8],"G#6");
		noteLookUpDict.Add (octave[5,9],"A6");
		noteLookUpDict.Add (octave[5,10],"A#6");
		noteLookUpDict.Add (octave[5,11],"B6");

		noteLookUpDict.Add (octave[6,0],"C7");
		noteLookUpDict.Add (octave[6,1],"C#7");
		noteLookUpDict.Add (octave[6,2],"D7");
		noteLookUpDict.Add (octave[6,3],"D#7");
		noteLookUpDict.Add (octave[6,4],"E7");
		noteLookUpDict.Add (octave[6,5],"F7");
		noteLookUpDict.Add (octave[6,6],"F#7");
		noteLookUpDict.Add (octave[6,7],"G7");
		noteLookUpDict.Add (octave[6,8],"G#7");
		noteLookUpDict.Add (octave[6,9],"A7");
		noteLookUpDict.Add (octave[6,10],"A#7");
		noteLookUpDict.Add (octave[6,11],"B7");

		noteLookUpDict.Add (C8, "C8");
	}

	/// <summary>
	/// Gets the nearest Note .
	/// </summary>
	/// <returns>The note.</returns>
	/// <param name="freq">Freq.</param>
	public string GetNote(float freq)
	{
		// Limit check
		if (freq > (C8 + 100) || freq <= 0) 
		{
			return "";
		}

		// Frequency Greater than 7 octaves
		if (freq > octave[6,11]) 
		{
			float midpoint = (octave [6, 11] + C8)/2;
			float nearestFreq = freq <= midpoint ? octave [6, 11] : C8;
			return noteLookUpDict [nearestFreq];
		}


		for (int i = 6; i >= 0; i--) // Iterating all Octaves
		{
			if (freq > octave [i, 0]) 	// Check if frequency is present in octave
			{
				for (int j = 1; j < 12; j++) // Iterate selected Octave
				{
					if (freq < octave [i, j]) 	// Get frequency range
					{
						// find the nearest note frequency
						float midpoint = (octave [i, j] + octave [i, j - 1])/2;
						float nearestFreq = freq <= midpoint ? octave [i, j - 1] : octave [i, j];
						return noteLookUpDict [nearestFreq];
					}
				}
			}	
		}
		return "";    // if no note found
	}
}
