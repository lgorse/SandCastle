using UnityEngine;
using System.Collections;

public class VertexEditor : MonoBehaviour {

	private JBooth.VertexPainterPro.VertexInstanceStream vStream;
	private float hInc = 1f;
	private int pIndex = 0;




	// Use this for initialization
	void Start () {
		vStream = gameObject.GetComponent<JBooth.VertexPainterPro.VertexInstanceStream> ();
		Cardboard.SDK.OnTrigger += editTerrain;

		//		Vector3 vertex = new Vector3(0.0f, 20f, 0.0f);
		//		Debug.Log (vStream.positions [pIndex].ToString());
		//		vStream.positions [0] = vertex;
		//		Debug.Log (vStream.positions [pIndex].ToString());
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void editTerrain(){
		
				Vector3 vertex = vStream.positions [pIndex];
		Debug.Log (vertex.ToString() + " before transform");
		float height = vertex.y + hInc;
		vertex.Set (vertex.x, height, vertex.z);
		Debug.Log (vertex.ToString() + " after transform");
		vStream.positions [pIndex] = vertex;
		vStream.Apply ();
	}
}
