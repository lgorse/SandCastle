using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LowPolyTerrainGenerator
{
	public class HeightmapInfo
	{
		public class ChunkSizeInfo
		{
			public class LODSizeInfo
			{
				public int quadPerSide;
				public int quadCount;
				public int triCount;
				public int vertPerSide;
				public int vertCount;
				public int stride;
			}

			public LODSizeInfo[] Sizes;

			public int originalVertPerSide;
			public int originalVertOffsetX;
			public int originalVertOffsetY;
		}

		public class LODSizeInfo
		{
			public int quadPerSide;
		}

		public ChunkSizeInfo[,] Chunks;
		public LODSizeInfo[] Sizes;

		// Sample the heightmap
		public int originalQuadPerSide;
		public int originalVertPerSide;

		public HeightmapInfo(int aTerrainSize, int aMaxResolution, int aChunkSize, int aLODLevels)
		{
			originalQuadPerSide = aTerrainSize / aMaxResolution;
			originalVertPerSide = originalQuadPerSide + 1;

			int quadPerChunk = aChunkSize / aMaxResolution;
			int originalVertPerChunk = quadPerChunk + 1;

			int chunkPerSide = aTerrainSize / aChunkSize;
			Chunks = new ChunkSizeInfo[chunkPerSide, chunkPerSide];
			for (int chunkX = 0; chunkX < chunkPerSide; ++chunkX)
			{
				int currentChunkOriginalVertOffsetX = chunkX * aChunkSize / aMaxResolution;
				for (int chunkY = 0; chunkY < chunkPerSide; ++chunkY)
				{
					int currentChunkOriginalVertOffsetY = chunkY * aChunkSize / aMaxResolution;

					var sizes = new ChunkSizeInfo.LODSizeInfo[aLODLevels];
					int currentQuadPerSide = quadPerChunk;
					int currentStride = 1;
					for (int i = 0; i < aLODLevels; ++i)
					{
						sizes[i] = new ChunkSizeInfo.LODSizeInfo()
						{
							quadPerSide = currentQuadPerSide,
							quadCount = currentQuadPerSide * currentQuadPerSide,
							triCount = currentQuadPerSide * currentQuadPerSide * 2,
							vertPerSide = currentQuadPerSide + 1,
							vertCount = currentQuadPerSide * currentQuadPerSide * 6,
							stride = currentStride,
						};

						currentQuadPerSide /= 2;
						currentStride *= 2;
					}

					Chunks[chunkX, chunkY] = new ChunkSizeInfo()
					{
						Sizes = sizes,
						originalVertPerSide = originalVertPerChunk,
						originalVertOffsetX = currentChunkOriginalVertOffsetX,
						originalVertOffsetY = currentChunkOriginalVertOffsetY
					};
				}
			}

			Sizes = new LODSizeInfo[aLODLevels];
			int lodCurrentQuadPerSide = originalQuadPerSide;
			for (int i = 0; i < aLODLevels; ++i)
			{
				Sizes[i] = new LODSizeInfo()
				{
					quadPerSide = lodCurrentQuadPerSide,
				};

				lodCurrentQuadPerSide /= 2;
			}
		}
	}

	public class Heightmap
	{
		public HeightmapInfo Info;
		public float[,] ColliderHeights;
		public Vector3[,] OriginalVerts;

		public float SampleNormalizedHeight(Vector2 pos)
		{
			return SampleBilinear(ColliderHeights, pos.y, pos.x);
		}

		public float SampleNormalizedHeight(float u, float v)
		{
			return SampleBilinear(ColliderHeights, v, u); 
		}
	}

	LowPolyTerrain _Terrain;

	/// <summary>
	/// Constructor, pass a terrain object that will be modified/generated
	/// </summary>
	public LowPolyTerrainGenerator(LowPolyTerrain aTerrain)
	{
		_Terrain = aTerrain;
	}

	/// <summary>
	/// Helper method to fetch the path to save mesh assets to
	/// </summary>
	public string GetBasePath()
	{
		string basePath = _Terrain.gameObject.scene.path;
		if (basePath == "")
		{
			EditorUtility.DisplayDialog("Save Scene First", "You need to Save the Scene before you can generate Terrain", "Ok");
			return "";
		}

		string extension = System.IO.Path.GetExtension(basePath);
		return basePath.Substring(0, basePath.Length - extension.Length);
	}

	/// <summary>
	/// Helper method used to display the asset associated with the terrain
	/// </summary>
	public Object GetFirstMesh()
	{
		Object ret = null;
		if (_Terrain.Chunks != null && _Terrain.Chunks.Length > 0)
		{
			if (_Terrain.Chunks[0].Renderers != null && _Terrain.Chunks[0].Renderers.Length > 0)
			{
				var filter = _Terrain.Chunks[0].Renderers[0].GetComponent<MeshFilter>();
				if (filter != null)
				{
					if (filter.sharedMesh != null)
					{
						var path = AssetDatabase.GetAssetPath(filter.sharedMesh);
						ret = AssetDatabase.LoadMainAssetAtPath(path);
					}
				}
			}
		}
		return ret;
	}

	public Heightmap GenerateHeightmap()
	{
		Heightmap ret = new Heightmap();

		ret.Info = new HeightmapInfo(_Terrain.TerrainSize, _Terrain.BaseResolution, _Terrain.ChunkSize, _Terrain.LODLevels);
		ret.ColliderHeights = new float[ret.Info.originalVertPerSide, ret.Info.originalVertPerSide];
		ret.OriginalVerts = new Vector3[ret.Info.originalVertPerSide, ret.Info.originalVertPerSide];

		// Import the map
		switch (_Terrain.SourceHeightMapType)
		{
			case LowPolyTerrain.HeightmapType.Bitmap:
				ReadHeightmapFromTexture(ret);
				break;
			case LowPolyTerrain.HeightmapType.Raw16:
				ReadHeightmapFromRaw16(ret);
				break;
			case LowPolyTerrain.HeightmapType.Raw32:
				ReadHeightmapFromRaw32(ret);
				break;
			default:
				throw new System.ArgumentOutOfRangeException();
		}
		EditorUtility.ClearProgressBar();
		return ret;
	}

	/// <summary>
	/// Main method: generate a terrain from all the terrain settings
	/// </summary>
	public void GenerateMeshes()
	{
		if (_Terrain.gameObject.scene.path == "")
		{
			EditorUtility.DisplayDialog("Save Scene First", "You need to Save the Scene before you can generate Terrain", "Ok");
			return;
		}

		// Make sure we HAVE a source height map
		switch (_Terrain.SourceHeightMapType)
		{
			case LowPolyTerrain.HeightmapType.Bitmap:
				if (_Terrain.SourceHeightMap == null)
				{
					EditorUtility.DisplayDialog("No height map", "Can't generate mesh with no height map", "Ok");
					return;
				}
				break;
			case LowPolyTerrain.HeightmapType.Raw16:
				goto case LowPolyTerrain.HeightmapType.Raw32;
			case LowPolyTerrain.HeightmapType.Raw32:
				if (!System.IO.File.Exists(_Terrain.SourceRawHeightMapFile))
				{
					EditorUtility.DisplayDialog("No height map", "Can't generate mesh with no height data", "Ok");
					return;
				}
				break;
		}

		// Destroy objects, if necessary
		foreach (var objects in _Terrain.GetComponents<LowPolyTerrainObjects>())
		{
			objects.ClearObjects();
		}

		// Destroy any child terrain mesh of this object
		EditorUtility.DisplayProgressBar("Generating Terrain", "Deleting previous chunk assets", 0.0f);
		if (_Terrain.Chunks != null)
		{
			foreach (var chunk in _Terrain.Chunks)
			{
				if (chunk != null)
				{
					for (int i = 0; i < chunk.Renderers.Length; ++i)
					{
						var meshFilter = chunk.Renderers[i].GetComponent<MeshFilter>();
						if (meshFilter != null)
						{
							var mesh = meshFilter.sharedMesh;
							if (mesh != null)
							{
								string assetPath = AssetDatabase.GetAssetPath(mesh);
								if (assetPath != null && assetPath != "")
								{
									// Delete the previous asset
									AssetDatabase.DeleteAsset(assetPath);
								}

								// Destroy the mesh asset itself
								meshFilter.sharedMesh = null;
								GameObject.DestroyImmediate(mesh);
							}
						}
					}
				}
			}
			_Terrain.Chunks = null;
		}

		// Destroy actual objects
		for (int i = _Terrain.transform.childCount - 1; i >= 0; --i)
		{
			Object.DestroyImmediate(_Terrain.transform.GetChild(i).gameObject);
		}

		string basePath = _Terrain.gameObject.scene.path;
		string extension = System.IO.Path.GetExtension(basePath);
		string chunksPath = basePath.Substring(0, basePath.Length - extension.Length);
		string chunksFullPath = UnityEditorUtilities.GetFullPath(chunksPath);
		System.IO.Directory.CreateDirectory(chunksFullPath);
		AssetDatabase.Refresh();

		// Prepare texture maps
		if (_Terrain.SourceHeightMapType == LowPolyTerrain.HeightmapType.Bitmap)
		{
			// Make sure the sampling is "clamped" to the edges
			EditorUtility.DisplayProgressBar("Generating Terrain", "Preparing Height Texture", 0.5f);
			UnityEditorUtilities.EnableTextureReadWrite(_Terrain.SourceHeightMap);
			EditorUtility.DisplayProgressBar("Generating Terrain", "Preparing Height Texture", 1.0f);
			UnityEditorUtilities.SetTextureImporterOptions(_Terrain.SourceHeightMap, (importer) => importer.wrapMode = TextureWrapMode.Clamp);
		}

		// Same for the color map
		if (_Terrain.GenerateVertColors)
		{
			EditorUtility.DisplayProgressBar("Generating Terrain", "Preparing Color Texture", 0.5f);
			UnityEditorUtilities.EnableTextureReadWrite(_Terrain.SourceColorMap);
			EditorUtility.DisplayProgressBar("Generating Terrain", "Preparing Color Texture", 1.0f);
			UnityEditorUtilities.SetTextureImporterOptions(_Terrain.SourceColorMap, (importer) => importer.wrapMode = TextureWrapMode.Clamp);
		}

		var heightmap = GenerateHeightmap();

		EditorUtility.DisplayProgressBar("Generating Terrain", "Generating chunks", 0.0f);
		int chunkPerSide = _Terrain.ChunkPerSide;
		_Terrain.Chunks = new LowPolyTerrain.ChunkRuntimeData[chunkPerSide * chunkPerSide];
		for (int x = 0; x < chunkPerSide; ++x)
		{
			EditorUtility.DisplayProgressBar("Generating Terrain", "Generating chunks", (float)x / chunkPerSide);
			for (int y = 0; y < chunkPerSide; ++y)
			{
				var chunkRoot = GenerateMeshForChunk(heightmap, x, y);

				// Attach the group to this object
				chunkRoot.transform.SetParent(_Terrain.transform);
				chunkRoot.transform.localPosition = Vector3.zero;
				chunkRoot.transform.localRotation = Quaternion.identity;
			}
		}

		// Create the collider
		var terrainData = new TerrainData();
		terrainData.heightmapResolution = heightmap.Info.originalVertPerSide;
		terrainData.size = new Vector3(_Terrain.TerrainSize, _Terrain.TerrainHeight, _Terrain.TerrainSize);
		terrainData.SetHeights(0, 0, heightmap.ColliderHeights);
		terrainData.name = _Terrain.name + " Heightmap Data";

		var terrainCollider = _Terrain.GetComponent<TerrainCollider>();
		if (terrainCollider == null)
		{
			terrainCollider = _Terrain.gameObject.AddComponent<TerrainCollider>();
		}
		terrainCollider.terrainData = terrainData;

		// Bake the meshes
		BakeMeshes();
		EditorUtility.ClearProgressBar();
		EditorUtility.SetDirty(_Terrain.gameObject);
		AssetDatabase.Refresh();
	}

	/// <summary>
	/// Shove all the meshes to an actual asset file, so as not to bloat the scene file
	/// </summary>
	public void BakeMeshes()
	{
		if (_Terrain.gameObject.scene.path == "")
		{
			EditorUtility.DisplayDialog("Save Scene First", "You need to Save the Scene before you can generate Terrain", "Ok");
			return;
		}

		string meshPath = GetBasePath() + "/" + _Terrain.name + "_FacetedChunks.asset";
		bool created = false;

		EditorUtility.DisplayProgressBar("Generating Terrain", "Baking chunks asset", 0.0f);
		int chunkCount = _Terrain.Chunks.Length;
		int chunkIndex = 0;
		foreach (var chunk in _Terrain.Chunks)
		{
			EditorUtility.DisplayProgressBar("Generating Terrain", "Baking chunks asset", (float)chunkIndex / chunkCount);
			Transform chunkTransform = chunk.ChunkRoot.transform;
			List<Mesh> meshes = new List<Mesh>(chunkTransform.GetComponentsInChildren<MeshFilter>().Select(mf => mf.sharedMesh));
			for (int j = 0; j < meshes.Count; ++j)
			{
				if (created)
				{
					AssetDatabase.AddObjectToAsset(meshes[j], meshPath);
				}
				else
				{
					AssetDatabase.CreateAsset(meshes[j], meshPath);
					created = true;
				}
			}
			chunkIndex++;
		}

		// Pack in the collider heightmap data as well
		var terrainCollider = _Terrain.GetComponent<TerrainCollider>();
		if (terrainCollider != null)
		{
			var heightData = terrainCollider.terrainData;
			AssetDatabase.AddObjectToAsset(heightData, meshPath);
		}

		EditorUtility.DisplayProgressBar("Generating Terrain", "Saving Chunks Asset (this may take a while)", 0.5f);
		AssetDatabase.SaveAssets();
		EditorUtility.ClearProgressBar();
	}

	/// <summary>
	/// When flipping the shadow casting flag, update all the renderers
	/// </summary>
	public void UpdateShadowCasting()
	{
		foreach (var chunk in _Terrain.Chunks)
		{
			chunk.Renderers[0].shadowCastingMode = _Terrain.CastShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
		}
	}

	/// <summary>
	/// Generate all the LOD levels for a given Terrain Chunk
	/// Pass in source data, and heightmap generation information, and return a root game object that has LOD meshes as children
	/// </summary>
	GameObject GenerateMeshForChunk(Heightmap aHeightmap, int aChunkX, int aChunkY)
	{
		// Create a root for the LODGroup
		var groupRoot = new GameObject("Chunk " + aChunkX + "," + aChunkY);

		// Create the children objects and attach them to the parent,
		// and create LOD data in the process as well
		MeshRenderer[] renderers = new MeshRenderer[_Terrain.LODLevels];
		LOD[] lods = new LOD[_Terrain.LODLevels];

		float currentHeight = 0.25f;
		for (int i = 0; i < _Terrain.LODLevels; ++i)
		{
			var lodChunk = GenerateMeshForChunkAndLOD(aHeightmap, aChunkX, aChunkY, i);

			lodChunk.transform.SetParent(groupRoot.transform);
			lodChunk.transform.localPosition = Vector3.zero;
			lodChunk.transform.localRotation = Quaternion.identity;

			renderers[i] = lodChunk.GetComponent<MeshRenderer>();
			lods[i] = new LOD(currentHeight, new Renderer[] { lodChunk.GetComponent<MeshRenderer>() });
			if (i != 0 || !_Terrain.CastShadows)
			{
				renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			}

			currentHeight /= 2.0f;
		}

		// Add a LODGroup to the group root object, and make it point to the renderers
		// This is really only for the scene view, the component gets destroyed at the start of the game!
		var unityLODGroup = groupRoot.AddComponent<LODGroup>();
		unityLODGroup.hideFlags = HideFlags.NotEditable | HideFlags.HideInInspector;
		lods[lods.Length - 1].screenRelativeTransitionHeight = 0.0f;
		unityLODGroup.SetLODs(lods);

		// Create the chunk lod data
		_Terrain.Chunks[aChunkX * _Terrain.ChunkPerSide + aChunkY] = new LowPolyTerrain.ChunkRuntimeData(groupRoot, renderers);

		// Hide chunk if desired
		if (_Terrain.HideChunksInHierarchy)
		{
			groupRoot.hideFlags = HideFlags.HideInHierarchy;
		}

		// Done
		return groupRoot;
	}

	struct AddTriangleParams
	{
		public Vector3[] Verts;
		public int[] Triangle;
		public bool FlippedTri;
		public bool UpperTri;
	}

	/// <summary>
	/// Generate a given LOD level of a given chunk of the terrain
	/// </summary>
	GameObject GenerateMeshForChunkAndLOD(Heightmap aHeightmap, int aChunkX, int aChunkY, int aLODIndex)
	{
		// Grab the size info for this LOD level
		var chunkInfo = aHeightmap.Info.Chunks[aChunkX, aChunkY];
		var sizeInfo = chunkInfo.Sizes[aLODIndex];

		string basePath = _Terrain.gameObject.scene.path;
		string extension = System.IO.Path.GetExtension(basePath);
		string imagePath = basePath.Substring(0, basePath.Length - extension.Length) + "/" + _Terrain.name + "_Chunk_" + aChunkX + "_" + aChunkY + "_" + aLODIndex + ".png";
		string imageFilePath = UnityEditorUtilities.GetFullPath(imagePath);

		Texture2D retTexture = null;
		//bool drawTexture = aChunkX == 0 && aChunkY == 0;
		bool drawTexture = false;
		if (drawTexture)
		{
			retTexture = new Texture2D(_Terrain.UV2MapSize, _Terrain.UV2MapSize, TextureFormat.ARGB32, false);
		}

		// Create the first mesh
		Mesh mesh = new Mesh();
		List<Vector3> verts = new List<Vector3>(sizeInfo.vertCount);
		List<Color> colors = new List<Color>(sizeInfo.vertCount);
		List<Vector2> uvs = new List<Vector2>(sizeInfo.vertCount);
		List<Vector2> uv2s = new List<Vector2>(sizeInfo.vertCount);
		List<Vector3> normals = new List<Vector3>(sizeInfo.vertCount);
		List<int> tris = new List<int>(sizeInfo.triCount * 3);

		Vector3[] edgeNormalXM = new Vector3[sizeInfo.quadPerSide];
		Vector3[] edgeNormalXP = new Vector3[sizeInfo.quadPerSide];
		Vector3[] edgeNormalYM = new Vector3[sizeInfo.quadPerSide];
		Vector3[] edgeNormalYP = new Vector3[sizeInfo.quadPerSide];

		Vector2[][] edgeUV2XM = null;
		Vector2[][] edgeUV2XP = null;
		Vector2[][] edgeUV2YM = null;
		Vector2[][] edgeUV2YP = null;

		if (_Terrain.GenerateUV2)
		{
			edgeUV2XM = new Vector2[sizeInfo.quadPerSide][];
			edgeUV2XP = new Vector2[sizeInfo.quadPerSide][];
			edgeUV2YM = new Vector2[sizeInfo.quadPerSide][];
			edgeUV2YP = new Vector2[sizeInfo.quadPerSide][];
		}

		int uv2padding = _Terrain.UV2Padding;
		int skirtQuad = 0;
		int uvQuadPerSide = (sizeInfo.quadPerSide + skirtQuad);// / uvMapCount;
		int uv2paddingTotal = _Terrain.UV2Padding * 2 + uv2padding * (2 * uvQuadPerSide - 1);
		float uv2perQuad = (float)(_Terrain.UV2MapSize - uv2paddingTotal) / uvQuadPerSide;
		float uv2QuadStride = uv2perQuad + uv2padding * 2;

		int vertIndex = 0;
		System.Action<int, int, AddTriangleParams> AddTriangle = (x, y, triparams) =>
		{
			// Verts
			var corner0 = triparams.Verts[triparams.Triangle[0]];
			var corner1 = triparams.Verts[triparams.Triangle[1]];
			var corner2 = triparams.Verts[triparams.Triangle[2]];
			verts.Add(corner0);
			verts.Add(corner1);
			verts.Add(corner2);

			// Compute the u,v barycenter
			float u = (corner0.x + corner1.x + corner2.x) / (_Terrain.TerrainSize * 3.0f);
			float v = (corner0.z + corner1.z + corner2.z) / (_Terrain.TerrainSize * 3.0f);

			Vector2 triUVs = new Vector2(u, v);
			uvs.Add(triUVs);
			uvs.Add(triUVs);
			uvs.Add(triUVs);

			if (_Terrain.GenerateVertColors)
			{
				// Sample the colormap at that location
				Color triColor = _Terrain.SourceColorMap.GetPixelBilinear(u, v);
				colors.Add(triColor);
				colors.Add(triColor);
				colors.Add(triColor);
			}

			// lightmap uvs
			Vector2[] cornerUV2s = null;
			if (_Terrain.GenerateUV2)
			{
				float uv2x = 0.0f;
				float uv2y = 0.0f;
				float uv2xp1 = 0.0f;
				float uv2yp1 = 0.0f;

				int padX = _Terrain.UV2Padding;
				int padY = _Terrain.UV2Padding;
				if (x >= 0 && x < sizeInfo.quadPerSide && y >= 0 && y < sizeInfo.quadPerSide)
				{
					padX += ((triparams.UpperTri && !triparams.FlippedTri || !triparams.UpperTri && triparams.FlippedTri) ? uv2padding : 0);
					padY += ((triparams.UpperTri && !triparams.FlippedTri || triparams.UpperTri && triparams.FlippedTri) ? uv2padding : 0);
				}
				int chunkX = aChunkX * sizeInfo.quadPerSide;
				int chunkY = aChunkY * sizeInfo.quadPerSide;

				uv2x = padX + ((x + chunkX) % uvQuadPerSide) * uv2QuadStride;
				uv2xp1 = uv2x + uv2perQuad;
				uv2y = padY + ((y + chunkY) % uvQuadPerSide) * uv2QuadStride;
				uv2yp1 = uv2y + uv2perQuad;

				cornerUV2s = new Vector2[4]
				{
					new Vector2(uv2x / _Terrain.UV2MapSize, uv2y / _Terrain.UV2MapSize),
					new Vector2(uv2x / _Terrain.UV2MapSize, uv2yp1 / _Terrain.UV2MapSize),
					new Vector2(uv2xp1 / _Terrain.UV2MapSize, uv2y / _Terrain.UV2MapSize),
					new Vector2(uv2xp1 / _Terrain.UV2MapSize, uv2yp1 / _Terrain.UV2MapSize),
				};

				var uv20 = cornerUV2s[triparams.Triangle[0]];
				var uv21 = cornerUV2s[triparams.Triangle[1]];
				var uv22 = cornerUV2s[triparams.Triangle[2]];
				uv2s.Add(uv20);
				uv2s.Add(uv21);
				uv2s.Add(uv22);
			}

			// Normal
			Vector3 triNormal = Vector3.Cross(corner1 - corner0, corner2 - corner0).normalized;
			normals.Add(triNormal);
			normals.Add(triNormal);
			normals.Add(triNormal);

			if (x == 0 && y >= 0 && y < sizeInfo.quadPerSide && triparams.FlippedTri == triparams.UpperTri)
			{
				edgeNormalXM[y] = triNormal;
			}

			if (x == sizeInfo.quadPerSide - 1 && y >= 0 && y < sizeInfo.quadPerSide && triparams.FlippedTri != triparams.UpperTri)
			{
				edgeNormalXP[y] = triNormal;
			}

			if (y == 0 && x >= 0 && x < sizeInfo.quadPerSide && !triparams.UpperTri)
			{
				edgeNormalYM[x] = triNormal;
			}

			if (y == sizeInfo.quadPerSide - 1 && x >= 0 && x < sizeInfo.quadPerSide && triparams.UpperTri)
			{
				edgeNormalYP[x] = triNormal;
			}

			if (_Terrain.GenerateUV2)
			{
				if (x == 0 && y >= 0 && y < sizeInfo.quadPerSide && triparams.FlippedTri == triparams.UpperTri)
				{
					cornerUV2s[2] = cornerUV2s[0];
					cornerUV2s[3] = cornerUV2s[1];
					edgeUV2XM[y] = cornerUV2s;
				}

				if (x == sizeInfo.quadPerSide - 1 && y >= 0 && y < sizeInfo.quadPerSide && triparams.FlippedTri != triparams.UpperTri)
				{
					cornerUV2s[0] = cornerUV2s[2];
					cornerUV2s[1] = cornerUV2s[3];
					edgeUV2XP[y] = cornerUV2s;
				}

				if (y == 0 && x >= 0 && x < sizeInfo.quadPerSide && !triparams.UpperTri)
				{
					cornerUV2s[1] = cornerUV2s[0];
					cornerUV2s[3] = cornerUV2s[2];
					edgeUV2YM[x] = cornerUV2s;
				}

				if (y == sizeInfo.quadPerSide - 1 && x >= 0 && x < sizeInfo.quadPerSide && triparams.UpperTri)
				{
					cornerUV2s[0] = cornerUV2s[1];
					cornerUV2s[2] = cornerUV2s[3];
					edgeUV2YP[x] = cornerUV2s;
				}
			}

			// And make the triangle
			tris.Add(vertIndex + 0);
			tris.Add(vertIndex + 1);
			tris.Add(vertIndex + 2);

			vertIndex += 3;
		};

		Vector3[] currentCorners = new Vector3[4];
		int[] topLeftBottomRightLower = new int[3] { 0, 1, 2 };
		int[] topLeftBottomRightUpper = new int[3] { 2, 1, 3 };
		int[] topRightBottomLeftLower = new int[3] { 0, 3, 2 };
		int[] topRightBottomLeftUpper = new int[3] { 0, 1, 3 };

		float skirtHeight = _Terrain.BaseResolution * Mathf.Pow(2.0f, aLODIndex);
		for (int x = -1; x < sizeInfo.quadPerSide + 1; ++x)
		{
			for (int y = -1; y < sizeInfo.quadPerSide + 1; ++y)
			{
				int lowerX = (x == -1) ? chunkInfo.originalVertOffsetX : x * sizeInfo.stride + chunkInfo.originalVertOffsetX;
				int lowerY = (y == -1) ? chunkInfo.originalVertOffsetY : y * sizeInfo.stride + chunkInfo.originalVertOffsetY;
				int upperX = (x == sizeInfo.quadPerSide) ? sizeInfo.quadPerSide * sizeInfo.stride + chunkInfo.originalVertOffsetX : (x + 1) * sizeInfo.stride + chunkInfo.originalVertOffsetX;
				int upperY = (y == sizeInfo.quadPerSide) ? sizeInfo.quadPerSide * sizeInfo.stride + chunkInfo.originalVertOffsetY : (y + 1) * sizeInfo.stride + chunkInfo.originalVertOffsetY;

				// Fixup coordinates for skirts
				Vector3 corner0 = aHeightmap.OriginalVerts[lowerX, lowerY];
				if (x == -1 || y == -1)
				{
					corner0.y -= skirtHeight;
				}
				currentCorners[0] = corner0;

				Vector3 corner1 = aHeightmap.OriginalVerts[lowerX, upperY];
				if (x == -1 || y == sizeInfo.quadPerSide)
				{
					corner1.y -= skirtHeight;
				}
				currentCorners[1] = corner1;

				Vector3 corner2 = aHeightmap.OriginalVerts[upperX, lowerY];
				if (x == sizeInfo.quadPerSide || y == -1)
				{
					corner2.y -= skirtHeight;
				}
				currentCorners[2] = corner2;

				Vector3 corner3 = aHeightmap.OriginalVerts[upperX, upperY];
				if (x == sizeInfo.quadPerSide || y == sizeInfo.quadPerSide)
				{
					corner3.y -= skirtHeight;
				}
				currentCorners[3] = corner3;

				bool topLeftToBottomRight = false;
				if (!_Terrain.UniformTriangles && x >= 0 && x < sizeInfo.quadPerSide && y >= 0 && y < sizeInfo.quadPerSide)
				{
					// Figure out which edge is best to use as mid-edge
					Vector2 center = new Vector2((corner1.x + corner2.x) * 0.5f, (corner1.z + corner2.z) * 0.5f);
					float centerHeight = aHeightmap.SampleNormalizedHeight(center);

					Vector2 toCorner0 = new Vector2(corner0.x - center.x, corner0.z - center.y);
					Vector2 toCorner1 = new Vector2(corner1.x - center.x, corner1.z - center.y);
					float pastCorner0Height = aHeightmap.SampleNormalizedHeight(center + toCorner0 * 4.0f) - centerHeight;
					float pastCorner1Height = aHeightmap.SampleNormalizedHeight(center + toCorner1 * 4.0f) - centerHeight;
					float pastCorner2Height = aHeightmap.SampleNormalizedHeight(center - toCorner1 * 4.0f) - centerHeight;
					float pastCorner3Height = aHeightmap.SampleNormalizedHeight(center - toCorner0 * 4.0f) - centerHeight;

					float heightDelta0 = Mathf.Abs(pastCorner0Height - pastCorner3Height);
					float heightDelta1 = Mathf.Abs(pastCorner1Height - pastCorner2Height);

					topLeftToBottomRight = heightDelta0 < heightDelta1;
				}

				if (topLeftToBottomRight)
				{
					// Mid edge goes from top-left to bottom-right
					AddTriangle(x, y, new AddTriangleParams()
					{
						Verts = currentCorners,
						Triangle = topLeftBottomRightLower,
						FlippedTri = false,
						UpperTri = false
					});
					AddTriangle(x, y, new AddTriangleParams()
					{
						Verts = currentCorners,
						Triangle = topLeftBottomRightUpper,
						FlippedTri = false,
						UpperTri = true
					});
				}
				else
				{
					AddTriangle(x, y, new AddTriangleParams()
					{
						Verts = currentCorners,
						Triangle = topRightBottomLeftLower,
						FlippedTri = true,
						UpperTri = false
					});
					AddTriangle(x, y, new AddTriangleParams()
					{
						Verts = currentCorners,
						Triangle = topRightBottomLeftUpper,
						FlippedTri = true,
						UpperTri = true
					});
				}
			}
		}

		System.Action<int, int, Vector3> SetNormal = (x, y, normal) =>
		{
			for (int i = 0; i < 6; ++i)
			{
				normals[((x + 1) * (sizeInfo.quadPerSide + 2) + (y + 1)) * 6 + i] = normal;
			}
		};

		System.Action<int, int, Vector2[]> SetUV2 = (x, y, corners) =>
		{
			int quadIndex = ((x + 1) * (sizeInfo.quadPerSide + 2) + (y + 1)) * 6;

			uv2s[quadIndex + 0] = corners[0];
			uv2s[quadIndex + 1] = corners[1];
			uv2s[quadIndex + 2] = corners[2];

			uv2s[quadIndex + 3] = corners[2];
			uv2s[quadIndex + 4] = corners[1];
			uv2s[quadIndex + 5] = corners[3];
		};

		// Fixup skirt normals
		for (int i = 0; i < sizeInfo.quadPerSide; ++i)
		{
			SetNormal(-1, i, edgeNormalXM[i]);
			SetNormal(sizeInfo.quadPerSide, i, edgeNormalXP[i]);
			SetNormal(i, -1, edgeNormalYM[i]);
			SetNormal(i, sizeInfo.quadPerSide, edgeNormalYP[i]);
		}

		mesh.vertices = verts.ToArray();
		mesh.normals = normals.ToArray();
		if (_Terrain.GenerateVertColors)
		{
			mesh.colors = colors.ToArray();
		}
		else
		{
			mesh.uv = uvs.ToArray();
		}
		if (_Terrain.GenerateUV2)
		{
			for (int i = -1; i < sizeInfo.quadPerSide + 1; ++i)
			{
				int uvIndex = Mathf.Clamp(i, 0, sizeInfo.quadPerSide - 1);
				SetUV2(-1, i, edgeUV2XM[uvIndex]);
				SetUV2(sizeInfo.quadPerSide, i, edgeUV2XP[uvIndex]);
				SetUV2(i, -1, edgeUV2YM[uvIndex]);
				SetUV2(i, sizeInfo.quadPerSide, edgeUV2YP[uvIndex]);
			}
			mesh.uv2 = uv2s.ToArray();
		}
		mesh.triangles = tris.ToArray();
		mesh.name = "LOD" + aLODIndex;
		mesh.RecalculateBounds();

		// Now create a gameobject, with renderer and all and assign it the mesh
		var lodObject = new GameObject(mesh.name);

		var meshFilter = lodObject.AddComponent<MeshFilter>();
		meshFilter.mesh = mesh;

		var meshRenderer = lodObject.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = _Terrain.TerrainMaterial;

		// This is a terrain mesh object, s oflag it as static
		GameObjectUtility.SetStaticEditorFlags(lodObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);

		if (drawTexture)
		{
			for (int i = 0; i < uv2s.Count; i += 3)
			{
				TextureDraw.DrawLine(retTexture,
					Mathf.RoundToInt(uv2s[i + 0].x * _Terrain.UV2MapSize), Mathf.RoundToInt(uv2s[i + 0].y * _Terrain.UV2MapSize),
					Mathf.RoundToInt(uv2s[i + 1].x * _Terrain.UV2MapSize), Mathf.RoundToInt(uv2s[i + 1].y * _Terrain.UV2MapSize),
					Color.red);

				TextureDraw.DrawLine(retTexture,
					Mathf.RoundToInt(uv2s[i + 1].x * _Terrain.UV2MapSize), Mathf.RoundToInt(uv2s[i + 1].y * _Terrain.UV2MapSize),
					Mathf.RoundToInt(uv2s[i + 2].x * _Terrain.UV2MapSize), Mathf.RoundToInt(uv2s[i + 2].y * _Terrain.UV2MapSize),
					Color.red);

				TextureDraw.DrawLine(retTexture,
					Mathf.RoundToInt(uv2s[i + 2].x * _Terrain.UV2MapSize), Mathf.RoundToInt(uv2s[i + 2].y * _Terrain.UV2MapSize),
					Mathf.RoundToInt(uv2s[i + 0].x * _Terrain.UV2MapSize), Mathf.RoundToInt(uv2s[i + 0].y * _Terrain.UV2MapSize),
					Color.red);
			}

			byte[] bytes = retTexture.EncodeToPNG();
			System.IO.File.WriteAllBytes(imageFilePath, bytes);
		}
		return lodObject;
	}

	/// <summary>
	/// When changing the material, update all the renderers
	/// </summary>
	public void UpdateRenderers()
	{
		foreach (var chunk in _Terrain.Chunks)
		{
			for (int i = 0; i < chunk.Renderers.Length; ++i)
			{
				chunk.Renderers[i].sharedMaterial = _Terrain.TerrainMaterial;
			}
		}
	}

	/// <summary>
	/// Grab the height map information from a texture's greyscale
	/// </summary>
	void ReadHeightmapFromTexture(Heightmap heightmap)
	{
		System.Func<float, float, float> readFromTexture = (u,v) =>
			{
				return _Terrain.SourceHeightMap.GetPixelBilinear(u, v).grayscale;
			};

		ReadHeightmap(heightmap, readFromTexture);
	}

	/// <summary>
	/// Grab the height map from a raw floating point file
	/// </summary>
	void ReadHeightmapFromRaw32(Heightmap heightmap)
	{
		var rawBytes = System.IO.File.ReadAllBytes(_Terrain.SourceRawHeightMapFile);
		bool reverseBytes = System.BitConverter.IsLittleEndian == (_Terrain.RawHeightMapOrder == LowPolyTerrain.ByteOrder.Mac);
		float[,] rawFloats = new float[_Terrain.RawHeightMapSize, _Terrain.RawHeightMapSize];
		int index = 0;

		EditorUtility.DisplayProgressBar("Generating Terrain", "Reading Heightmap", 0.0f);
		for (int y = 0; y < _Terrain.RawHeightMapSize; ++y)
		{
			EditorUtility.DisplayProgressBar("Generating Terrain", "Reading Heightmap", (float)y / _Terrain.RawHeightMapSize);
			for (int x = 0; x < _Terrain.RawHeightMapSize; ++x)
			{
				if (reverseBytes)
				{
					System.Array.Reverse(rawBytes, index, sizeof(System.Single));
				}
				rawFloats[x, _Terrain.RawHeightMapSize - y - 1] = System.BitConverter.ToSingle(rawBytes, index);
				index += sizeof(float);
			}
		}
		EditorUtility.ClearProgressBar();

		ReadHeightmap(heightmap, (u, v) => SampleBilinear(rawFloats, u, v));
	}

	/// <summary>
	/// Grab the height map from a raw16 (uint16) file
	/// </summary>
	void ReadHeightmapFromRaw16(Heightmap heightmap)
	{
		var rawBytes = System.IO.File.ReadAllBytes(_Terrain.SourceRawHeightMapFile);
		bool reverseBytes = System.BitConverter.IsLittleEndian == (_Terrain.RawHeightMapOrder == LowPolyTerrain.ByteOrder.Mac);
		float[,] rawFloats = new float[_Terrain.RawHeightMapSize, _Terrain.RawHeightMapSize];
		int index = 0;

		EditorUtility.DisplayProgressBar("Generating Terrain", "Reading Heightmap", 0.0f);
		for (int y = 0; y < _Terrain.RawHeightMapSize; ++y)
		{
			EditorUtility.DisplayProgressBar("Generating Terrain", "Reading Heightmap", (float)y / _Terrain.RawHeightMapSize);
			for (int x = 0; x < _Terrain.RawHeightMapSize; ++x)
			{
				if (reverseBytes)
				{
					System.Array.Reverse(rawBytes, index, sizeof(System.UInt16));
				}
				rawFloats[x, _Terrain.RawHeightMapSize - y - 1] = (float)System.BitConverter.ToUInt16(rawBytes, index) / System.UInt16.MaxValue;

				index += sizeof(System.UInt16);
			}
		}
		EditorUtility.ClearProgressBar();

		ReadHeightmap(heightmap, (u, v) => SampleBilinear(rawFloats, u, v));
	}

	/// <summary>
	/// Method to read the height map into the initial array of vertices used to generate the terrain meshes
	/// </summary>
	void ReadHeightmap(Heightmap heightmap, System.Func<float, float, float> sampleFunc)
	{
		EditorUtility.DisplayProgressBar("Generating Terrain", "Generating Base Heightmap", 0.0f);
		for (int x = 0; x < heightmap.Info.originalVertPerSide; ++x)
		{
			EditorUtility.DisplayProgressBar("Generating Terrain", "Generating Base Heightmap", (float)x / heightmap.Info.originalVertPerSide);
			for (int y = 0; y < heightmap.Info.originalVertPerSide; ++y)
			{
				float u = (float)x / (heightmap.Info.originalVertPerSide - 1);
				float v = (float)y / (heightmap.Info.originalVertPerSide - 1);
				float greyscale = sampleFunc(u, v);
				float height = greyscale * _Terrain.TerrainHeight + Random.Range(-_Terrain.RandomYOffset, _Terrain.RandomYOffset);
				float xCoord = u * _Terrain.TerrainSize;
				float zCoord = v * _Terrain.TerrainSize;
				heightmap.ColliderHeights[y, x] = height / _Terrain.TerrainHeight;
				Vector2 xyOffset = Vector2.zero;
				if (!_Terrain.UniformTriangles)
				{
					xyOffset = Random.insideUnitCircle * _Terrain.RandomXZOffset;
				}
				heightmap.OriginalVerts[x, y] = new Vector3(xCoord + xyOffset.x, height, zCoord + xyOffset.y);
			}
		}
		EditorUtility.ClearProgressBar();
	}

	/// <summary>
	/// Helper method to sample a raw file with floating point coordinates
	/// </summary>
	static float SampleBilinear(float[,] rawFloats, float u, float v)
	{
		int xLength = rawFloats.GetLength(0);
		float x = u * xLength;
		int xPrev = Mathf.FloorToInt(x);
		float xLerp = x - xPrev;
		int xNext = xPrev + 1;

		if (xNext <= 0)
		{
			xPrev = 0;
			xNext = 1;
			xLerp = 0.0f;
		}

		if (xPrev >= xLength - 1)
		{
			xPrev = xLength - 2;
			xNext = xLength - 1;
			xLerp = 1.0f;
		}

		int yLength = rawFloats.GetLength(1);
		float y = v * yLength;
		int yPrev = Mathf.FloorToInt(y);
		float yLerp = y - yPrev;
		int yNext = yPrev + 1;

		if (yNext <= 0)
		{
			yPrev = 0;
			yNext = 1;
			yLerp = 0.0f;
		}

		if (yPrev >= yLength - 1)
		{
			yPrev = yLength - 2;
			yNext = yLength - 1;
			yLerp = 1.0f;
		}

		float prevY = Mathf.Lerp(rawFloats[xPrev, yPrev], rawFloats[xNext, yPrev], xLerp);
		float nextY = Mathf.Lerp(rawFloats[xPrev, yNext], rawFloats[xNext, yNext], xLerp);
		return Mathf.Lerp(prevY, nextY, yLerp);
	}
}
