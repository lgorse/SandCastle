using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using JBooth.VertexPainterPro;

public class cubeScript : MonoBehaviour {
	private float hInc = 1f;
	JBooth.VertexPainterPro.VertexInstanceStream painter;
	GameObject eventSystem;
	GazeInputModule inputModule;
	Terrain terrain;
	paintjob.Painter paintHolder;





	// Use this for initialization
	void Start () {
		 eventSystem = GameObject.Find ("EventSystem");
		 inputModule = eventSystem.GetComponent<GazeInputModule> ();
		terrain = gameObject.GetComponent<Terrain>();
		QT_PolyWorldTerrain pTerrain = gameObject.GetComponent<QT_PolyWorldTerrain> ();
		if (pTerrain != null) {
			Vector2 chunk = pTerrain.GetChunkSizes()[pTerrain.chunkIndex];
			paintHolder = new paintjob.Painter(terrain, chunk.x);
		}


	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void dumpsand(){
		Vector3 position = inputModule.GetIntersectionPosition();
		paintHolder.paintMeshesFromDictionary (position);
	
	}



}
