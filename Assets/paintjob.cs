﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using JBooth.VertexPainterPro;


public class paintjob : MonoBehaviour
{

	public static float brushSize = 2;
	public static float brushFlow = 0.1f;
	public static float brushFalloff = 0.1f;
	public static float deltaTime = 1;
	public static bool pull = true;
	public static float pressure = 1.0f;


	public class Painter
	{
		public PaintJob[] jobs = new PaintJob[0];
		private Dictionary<string, PaintJob> vectorsToJobName = new Dictionary<string, PaintJob> ();
		private List<Vector3> allVectorsArray = new List<Vector3> ();




		public Painter ()
		{
		}

		public void InitMeshes (GameObject terrainObject)
		{
			//RevertMat();


			List<PaintJob> pjs = new List<PaintJob> ();
			Transform[] objs = terrainObject.GetComponentsInChildren<Transform> (); 

			for (int i = 0; i < objs.Length; ++i) {
				GameObject go = objs [i].gameObject;
				if (go != null && go.gameObject.layer == 9) {
					MeshFilter mf = go.GetComponent<MeshFilter> ();
					Renderer r = go.GetComponent<Renderer> ();
					string name = go.name;
				
					if (mf != null && r != null && mf.sharedMesh.isReadable) {
						PaintJob pJob = new PaintJob (mf, r, name);
						pjs.Add (pJob);

						foreach (Vector3 position in pJob.verts) {
							Vector3 point = pJob.renderer.transform.TransformPoint (position);

							// for some reason this doesn't handle scale, seems like it should
							// we handle it poorly until I can find a better solution
							
							//Debug.Log ("now in world space: " + point.ToString () + " as opposed to local " + position.ToString());
							if (!vectorsToJobName.ContainsKey (point.ToString ())) {
								vectorsToJobName.Add (point.ToString (), pJob);
								allVectorsArray.Add (point);
							}
								
						}


					}
				}
			}

			jobs = pjs.ToArray ();
			//UpdateDisplayMode();
		}
			

		public void paintMeshes (Vector3 point)
		{
			if (jobs.Length > 0) {
				for (int i = 0; i < jobs.Length; ++i) {
					PaintMesh (jobs [i], point);

				}
				EndStroke ();
			}

		}

		public void paintMeshesFromDictionary (Vector3 point)
		{
			


			List<PaintJob> closeJobs = new List<PaintJob> ();
			//float scale = 1.0f / Mathf.Abs (j.renderer.transform.lossyScale.x);
			float bz = 1.0f * paintjob.brushSize;



			foreach (Vector3 vector in allVectorsArray) {
				float d = Vector3.Distance (point, vector);
				if (d < bz) {
					PaintJob j = vectorsToJobName [vector.ToString ()];
					if (!closeJobs.Contains (j)) {
						closeJobs.Add (j);
					}


					//PaintVertPosition (j, i, str * (float)paintjob.deltaTime * paintjob.brushFlow * paintjob.pressure);
				}
			}

			foreach (PaintJob j in closeJobs) {
				Profiler.BeginSample("Paint Mesh");
				PrepBrushMode (j);
				Vector3 localPoint = j.renderer.transform.worldToLocalMatrix.MultiplyPoint3x4 (point);
				for (int i = 0; i < j.verts.Length; ++i) {
					float d = Vector3.Distance (localPoint, j.verts [i]);	
					if (d < bz) {
						float str = 1.0f - d / bz;
						str = Mathf.Pow (str, paintjob.brushFalloff);
						PaintVertPosition (j, i, str * (float)paintjob.deltaTime * paintjob.brushFlow * paintjob.pressure);
					}
				}
				j.stream.Apply ();
				Profiler.EndSample();
				
			}

			EndStroke (closeJobs.ToArray());




			//	bool modPos = !(j.stream.positions == null || j.stream.positions.Length == 0);
			
		}


		void PaintMesh (PaintJob j, Vector3 point)
		{
			//Profiler.BeginSample("Paint Mesh");
			PrepBrushMode (j);
			// convert point into local space, so we don't have to convert every point\

			point = j.renderer.transform.worldToLocalMatrix.MultiplyPoint3x4 (point);
			// for some reason this doesn't handle scale, seems like it should
			// we handle it poorly until I can find a better solution
			float scale = 1.0f / Mathf.Abs (j.renderer.transform.lossyScale.x);

			float bz = scale * paintjob.brushSize;



			bool modPos = !(j.stream.positions == null || j.stream.positions.Length == 0);



			for (int i = 0; i < j.verts.Length; ++i) {
				float d = Vector3.Distance (point, j.verts [i]);
				if (d < bz) {



					float str = 1.0f - d / bz;
					str = Mathf.Pow (str, paintjob.brushFalloff);
					PaintVertPosition (j, i, str * (float)paintjob.deltaTime * paintjob.brushFlow * paintjob.pressure);
				}
			}
			j.stream.Apply ();
			//Profiler.EndSample();
		}

		void PrepBrushMode (PaintJob j)
		{
			Vector3[] pos = j.stream.positions;
			if (pos == null || pos.Length != j.verts.Length) {
				int vc = j.meshFilter.sharedMesh.vertexCount;
				if (j.stream.positions == null || j.stream.positions.Length != vc) {
					j.stream.positions = new Vector3[j.meshFilter.sharedMesh.vertices.Length];
					j.meshFilter.sharedMesh.vertices.CopyTo (j.stream.positions, 0);
				}
				if (j.stream.normals == null || j.stream.normals.Length != vc) {
					j.stream.normals = new Vector3[j.meshFilter.sharedMesh.vertices.Length];
					j.meshFilter.sharedMesh.normals.CopyTo (j.stream.normals, 0);
				}
				if (j.stream.tangents == null || j.stream.tangents.Length != vc) {
					j.stream.tangents = new Vector4[j.meshFilter.sharedMesh.vertices.Length];
					j.meshFilter.sharedMesh.tangents.CopyTo (j.stream.tangents, 0);
				}
			}
		}

		void PaintVertPosition (PaintJob j, int i, float strength)
		{
			Vector3 cur = j.stream.positions [i];
			//Vector3 oldWorldCur = j.renderer.transform.TransformPoint (cur);
			Vector3 dir = new Vector3 (0, 1, 0);
			dir *= strength;
			cur += paintjob.pull ? dir : -dir;
			j.stream.positions [i] = cur;
//			Vector3 worldCur = j.renderer.transform.TransformPoint (cur);
//			vectorsToJobName.Remove (oldWorldCur.ToString());
//			if (!vectorsToJobName.ContainsKey(worldCur.ToString())){
//				vectorsToJobName.Add (worldCur.ToString(), j);
//			}

		}

		void EndStroke(PaintJob[] closeJobs)
		{

			// could possibly make this faster by avoiding the double apply..

			Profiler.BeginSample("Recalculate Normals and Tangents");
			for (int i = 0; i < closeJobs.Length; ++i)
			{
				PaintJob j = closeJobs[i];
				if (j.stream.positions != null && j.stream.normals != null && j.stream.tangents != null)
				{
					Mesh m = j.stream.Apply(false);
					m.triangles = j.meshFilter.sharedMesh.triangles;

					m.RecalculateNormals();
					if (j.stream.normals == null)
					{
						j.stream.normals = new Vector3[m.vertexCount];
					}
					j.stream.normals = m.normals;
					//m.normals.CopyTo(j.stream.normals, 0);

					m.uv = j.meshFilter.sharedMesh.uv;
					CalculateMeshTangents(m);
					if (j.stream.tangents == null)
					{
						j.stream.tangents = new Vector4[m.vertexCount];
					}
					j.stream.tangents = m.tangents;
					m.tangents.CopyTo(j.stream.tangents, 0);

					m.RecalculateBounds();
					j.stream.Apply();
				}

				Profiler.EndSample();
			}
			//Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
		}




		void EndStroke()
		{

			// could possibly make this faster by avoiding the double apply..

			Profiler.BeginSample("Recalculate Normals and Tangents");
			for (int i = 0; i < jobs.Length; ++i)
			{
				PaintJob j = jobs[i];
				if (j.stream.positions != null && j.stream.normals != null && j.stream.tangents != null)
				{
					Mesh m = j.stream.Apply(false);
					m.triangles = j.meshFilter.sharedMesh.triangles;

					m.RecalculateNormals();
					if (j.stream.normals == null)
					{
						j.stream.normals = new Vector3[m.vertexCount];
					}
					j.stream.normals = m.normals;
					//m.normals.CopyTo(j.stream.normals, 0);

					m.uv = j.meshFilter.sharedMesh.uv;
					CalculateMeshTangents(m);
					if (j.stream.tangents == null)
					{
						j.stream.tangents = new Vector4[m.vertexCount];
					}
					j.stream.tangents = m.tangents;
					m.tangents.CopyTo(j.stream.tangents, 0);

					m.RecalculateBounds();
					j.stream.Apply();
				}

				Profiler.EndSample();
			}
			//Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
		}

		void CalculateMeshTangents (Mesh mesh)
		{
			//speed up math by copying the mesh arrays
			int[] triangles = mesh.triangles;
			Vector3[] vertices = mesh.vertices;
			Vector2[] uv = mesh.uv;
			Vector3[] normals = mesh.normals;

			//variable definitions
			int triangleCount = triangles.Length;
			int vertexCount = vertices.Length;

			Debug.Log ("triangles " + triangleCount + " vertexes "+ vertexCount);

			Vector3[] tan1 = new Vector3[vertexCount];
			Vector3[] tan2 = new Vector3[vertexCount];

			Vector4[] tangents = new Vector4[vertexCount];

			for (long a = 0; a < triangleCount; a += 3) {
				long i1 = triangles [a + 0];
				long i2 = triangles [a + 1];
				long i3 = triangles [a + 2];

				Vector3 v1 = vertices [i1];
				Vector3 v2 = vertices [i2];
				Vector3 v3 = vertices [i3];

				Vector2 w1 = uv [i1];
				Vector2 w2 = uv [i2];
				Vector2 w3 = uv [i3];

				float x1 = v2.x - v1.x;
				float x2 = v3.x - v1.x;
				float y1 = v2.y - v1.y;
				float y2 = v3.y - v1.y;
				float z1 = v2.z - v1.z;
				float z2 = v3.z - v1.z;

				float s1 = w2.x - w1.x;
				float s2 = w3.x - w1.x;
				float t1 = w2.y - w1.y;
				float t2 = w3.y - w1.y;

				float div = s1 * t2 - s2 * t1;
				float r = div == 0.0f ? 0.0f : 1.0f / div;

				Vector3 sdir = new Vector3 ((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
				Vector3 tdir = new Vector3 ((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

				tan1 [i1] += sdir;
				tan1 [i2] += sdir;
				tan1 [i3] += sdir;

				tan2 [i1] += tdir;
				tan2 [i2] += tdir;
				tan2 [i3] += tdir;
			}


			for (long a = 0; a < vertexCount; ++a) {
				Vector3 n = normals [a];
				Vector3 t = tan1 [a];

				//Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
				//tangents[a] = new Vector4(tmp.x, tmp.y, tmp.z);
				Vector3.OrthoNormalize (ref n, ref t);
				tangents [a].x = t.x;
				tangents [a].y = t.y;
				tangents [a].z = t.z;

				tangents [a].w = (Vector3.Dot (Vector3.Cross (n, t), tan2 [a]) < 0.0f) ? -1.0f : 1.0f;
			}

			mesh.tangents = tangents;
		}



		public class PaintJob
		{

			public MeshFilter meshFilter;
			public Renderer renderer;
			public JBooth.VertexPainterPro.VertexInstanceStream _stream;
			// cache of data we often need so we don't have to cross the c#->cpp bridge often
			public Vector3[] verts;
			public Vector3[] normals;
			public Vector4[] tangents;
			public List<int>[] vertexConnections;
			public string name;



			public bool HasStream ()
			{
				return _stream != null;
			}

			public JBooth.VertexPainterPro.VertexInstanceStream stream {
				get {
					if (_stream == null) {
						if (meshFilter == null) { // object has been deleted
							return null;
						}
						_stream = meshFilter.gameObject.GetComponent<VertexInstanceStream> ();
						if (_stream == null) {
							_stream = meshFilter.gameObject.AddComponent<VertexInstanceStream> ();
						} else {
							_stream.Apply ();
						}
					}
					return _stream;
				}

			}

			public void InitMeshConnections ()
			{
				Profiler.BeginSample ("Generate Mesh Connections");
				// a half edge representation would be nice, but really just care about adjacentcy for now.. 
				vertexConnections = new List<int>[meshFilter.sharedMesh.vertexCount];
				for (int i = 0; i < vertexConnections.Length; ++i) {
					vertexConnections [i] = new List<int> ();
				}
				int[] tris = meshFilter.sharedMesh.triangles;
				for (int i = 0; i < tris.Length; i = i + 3) {
					int c0 = tris [i];
					int c1 = tris [i + 1];
					int c2 = tris [i + 2];

					List<int> l = vertexConnections [c0];
					if (!l.Contains (c1)) {
						l.Add (c1);
					}
					if (!l.Contains (c2)) {
						l.Add (c2);
					}

					l = vertexConnections [c1];
					if (!l.Contains (c2)) {
						l.Add (c2);
					}
					if (!l.Contains (c0)) {
						l.Add (c0);
					}

					l = vertexConnections [c2];
					if (!l.Contains (c1)) {
						l.Add (c1);
					}
					if (!l.Contains (c0)) {
						l.Add (c0);
					}
				}
				Profiler.EndSample ();
			}

			public PaintJob (MeshFilter mf, Renderer r, string n)
			{
				meshFilter = mf;
				renderer = r;
				name = n;
//			if (r.sharedMaterials != null && r.sharedMaterials.Length > 1)
//			{
//					Debug.Log(stream);
//				stream.originalMaterial = new Material[r.sharedMaterials.Length];
//				for (int i = 0; i < r.sharedMaterials.Length; ++i)
//				{
//					stream.originalMaterial[i] = r.sharedMaterials[i];
//				}
//			}
//			else
//			{
//					Debug.Log(stream);
//				stream.originalMaterial = new Material[1];
//				stream.originalMaterial[0] = r.sharedMaterial;
//			}
				verts = mf.sharedMesh.vertices;
				normals = mf.sharedMesh.normals;
				tangents = mf.sharedMesh.tangents;
				// optionally defer this unless the brush is set to position..
				InitMeshConnections ();
			}
				
		}
			
	}

	// Use this for initialization
	void Start ()
	{

	}

	// Update is called once per frame
	void Update ()
	{

	}
}
