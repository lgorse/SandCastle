using UnityEngine;
using System.Collections;

public class lowpolyEdit : MonoBehaviour {

	private TerrainData tData;
	private TerrainCollider terrain;

	private int xRes;
	private int yRes;
	private float hInc = 1f;
	private float dHeight = 0f;

	// Use this for initialization
	void Start () {
		terrain = gameObject.GetComponent <TerrainCollider> ();
		tData = terrain.terrainData;
		Debug.Log (tData.heightmapWidth);
		xRes = tData.heightmapWidth;
		yRes = tData.heightmapHeight;

		Cardboard.SDK.OnTrigger += editTerrain;

	}

	// Update is called once per frame
	void Update () {

	}

	void editTerrain(){
		Debug.Log ("hello");
		float currentHeight = tData.GetHeights(50,50,1,1)[0,0];
		currentHeight += hInc;
		float[,] heights = new float[1,1]{{currentHeight}};
		tData.SetHeights(50,50, heights);

	}

	private void OnDestroy(){
		this.terrain.terrainData.SetHeights (5, 5, new[,]{ { 0.0f} });
	}
}
