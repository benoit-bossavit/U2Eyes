﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class InputEyeParams : MonoBehaviour {

	SynthesEyesServer synthesEyesServer;
	
	void Start () {
		
		synthesEyesServer = GameObject.Find ("SynthesEyes").GetComponent<SynthesEyesServer>();
		
		InputField input = this.GetComponent<InputField>() as InputField;
		input.onEndEdit.AddListener(SubmitCameraParams); 
	}
	
	private void SubmitCameraParams(string arg0)
	{
		string[] args = arg0.Split (',');
		synthesEyesServer.defaultEyePitch = float.Parse (args[0]);
		synthesEyesServer.defaultEyeYaw =   float.Parse (args[1]);
		synthesEyesServer.eyePitchNoise =   float.Parse (args[2]);
		synthesEyesServer.eyeYawNoise =     float.Parse (args[3]);
	}
}
