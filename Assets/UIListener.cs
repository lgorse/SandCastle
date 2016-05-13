using UnityEngine;
using System.Collections;

public class UIListener : MonoBehaviour {
	
	ParticleSystem particles;

	// Use this for initialization
	void Start () {
		//Cardboard.SDK.OnTrigger += toggleParticles;
		particles = gameObject.GetComponent <ParticleSystem> ();

	
	}
	
	// Update is called once per frame
	void Update () {
		
	
	}

	void toggleParticles(){
		Debug.Log ("Tapped");
		if (particles.isPlaying) {
			particles.Stop();
		} else {
			particles.Play();
		}
	}
}
