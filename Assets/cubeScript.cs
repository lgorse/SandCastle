using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using JBooth.VertexPainterPro;

public class cubeScript : MonoBehaviour {
	private float hInc = 1f;
	JBooth.VertexPainterPro.VertexInstanceStream painter;
	GameObject eventSystem;
	GazeInputModule inputModule;
	GameObject terrain;
	paintjob.Painter paintHolder;





	// Use this for initialization
	void Start () {
		 eventSystem = GameObject.Find ("EventSystem");
		 inputModule = eventSystem.GetComponent<GazeInputModule> ();
		terrain = GameObject.Find ("Terrain");
		paintHolder = new paintjob.Painter(terrain);	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void dumpsand(){
		Vector3 position = inputModule.GetIntersectionPosition();
		paintHolder.paintMeshesFromDictionary (position);
	
	}



}
