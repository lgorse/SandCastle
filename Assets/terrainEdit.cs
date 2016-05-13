using UnityEngine;
using System.Collections;

public class terrainEdit : MonoBehaviour {
//	private TerrainData tData;
//	private QT_PolyWorldTerrain terrain;
//
//	private int xRes;
//	private int yRes;
//	private float hInc = 0.01f;
//	private float dHeight = 0f;
//
//	// Use this for initialization
//	void Start () {
//		terrain = gameObject.GetComponent <QT_PolyWorldTerrain> ();
//		tData = terrain.terrainData;
//		xRes = tData.heightmapWidth;
//		yRes = tData.heightmapHeight;
//
//		Cardboard.SDK.OnTrigger += editTerrain;
//	
//	}
//	
//	// Update is called once per frame
//	void Update () {
//	
//	}
//
//	void editTerrain(){
//		float currentHeight = tData.GetHeights(5,5,1,1)[0,0];
//		currentHeight += hInc;
//		float[,] heights = new float[1,1]{{currentHeight}};
//		tData.SetHeights(5,5, heights);
//		
//	}
//
//	private void OnDestroy(){
//		this.terrain.terrainData.SetHeights (5, 5, new[,]{ { 0.0f} });
//	}
}
