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
		paintHolder = new paintjob.Painter();
		 eventSystem = GameObject.Find ("EventSystem");
		 inputModule = eventSystem.GetComponent<GazeInputModule> ();
		terrain = GameObject.Find ("Terrain");
		paintHolder.InitMeshes (terrain);

	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void dumpsand(){
		Vector3 position = inputModule.GetIntersectionPosition();
		//paintHolder.paintMeshes (position);
		paintHolder.paintMeshesFromDictionary (position);


		
	}

	public void hello(){
		MeshFilter mf = terrain.GetComponent<MeshFilter>();
		Renderer r = terrain.GetComponent<Renderer>();
		mf.sharedMesh.vertices.ToString ();
		
//		Vector3 position = inputModule.GetIntersectionPosition();
//		int x = (int) Mathf.Floor (position.x/8);
//		int y = (int) Mathf.Floor (position.z/8);	
//		Debug.Log ("SEEKING Terrain_x" + x + "y" + y);
//		//terrain = GameObject.Find ("Terrain_x"+x+"y"+y);
//
//		painter = terrain.GetComponent<JBooth.VertexPainterPro.VertexInstanceStream> ();
//
//
//		Debug.Log ("terrain: " + terrain.transform.ToString());
//		Debug.Log ("painter " + painter.positions.Length); 
//		Debug.Log ("position 0" + painter.positions [0]);
//		Debug.Log ("position 1" + painter.positions [1]);
//		Debug.Log ("position 1535" + painter.positions [painter.positions.Length - 1]);
//		for (int i = 0; i < painter.positions.Length; i++) {
//			Debug.Log("position " + i + " = " + painter.positions[i]);
//		}
//
//		Debug.Log (position);
//		//search (position, painter.positions);
	}



}
