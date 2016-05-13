using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;

[CustomEditor(typeof(LowPolyTerrain))]
public class LowPolyTerrainEditor : Editor
{
	static GUIStyle _Line;

	[MenuItem("GameObject/Low Poly Terrain", false, 10)]
	static void CreateLowPolyTerrain()
	{
		var terrainObj = new GameObject();
		terrainObj.name = "Terrain";
		terrainObj.transform.position = Vector3.zero;
		terrainObj.transform.rotation = Quaternion.identity;
		terrainObj.transform.localScale = Vector3.one;
		terrainObj.AddComponent<LowPolyTerrain>();
	}

	public override void OnInspectorGUI()
	{
		if (_Line == null)
		{
			_Line = new GUIStyle("box");
			_Line.border.top = _Line.border.bottom = 1;
			_Line.margin.top = 1;
			_Line.margin.bottom = 5;
			_Line.padding.top = _Line.padding.bottom = 1;
			_Line.padding.top = _Line.padding.bottom = 1;
		}

		LowPolyTerrain terrain = (LowPolyTerrain)target;
		var terrainObj = new SerializedObject(terrain);
		var SourceHeightMapType = terrainObj.FindProperty("SourceHeightMapType");
		var SourceHeightMap = terrainObj.FindProperty("SourceHeightMap");
		var SourceRawHeightMapFile = terrainObj.FindProperty("SourceRawHeightMapFile");
		var RawHeightMapSize = terrainObj.FindProperty("RawHeightMapSize");
		var RawHeightMapOrder = terrainObj.FindProperty("RawHeightMapOrder");
		var SourceColorMap = terrainObj.FindProperty("SourceColorMap");
		var TerrainMaterial = terrainObj.FindProperty("TerrainMaterial");
		var TerrainAlphaMaterial = terrainObj.FindProperty("TerrainAlphaMaterial");
		var TerrainSize = terrainObj.FindProperty("TerrainSize");
		var TerrainHeight = terrainObj.FindProperty("TerrainHeight");
		var ChunkSize = terrainObj.FindProperty("ChunkSize");
		var BaseResolution = terrainObj.FindProperty("BaseResolution");
		var RandomYOffset = terrainObj.FindProperty("RandomYOffset");
		var RandomXZOffset = terrainObj.FindProperty("RandomXZOffset");
		var UniformTriangles = terrainObj.FindProperty("UniformTriangles");
		var LODLevels = terrainObj.FindProperty("LODLevels");
		var LODDistances = terrainObj.FindProperty("_Distances");
		var LODTransitionTime = terrainObj.FindProperty("LODTransitionTime");
		var FlipFlopPercent = terrainObj.FindProperty("FlipFlopPercent");
		var HideChunksInHierarchy = terrainObj.FindProperty("HideChunksInHierarchy");
		var GenerateVertColors = terrainObj.FindProperty("GenerateVertColors");
		var CastShadows = terrainObj.FindProperty("CastShadows");

		//var UV2Padding = terrainObj.FindProperty("UV2Padding");
		//var UV2MapSize = terrainObj.FindProperty("UV2MapSize");
		//var GenerateUV2 = terrainObj.FindProperty("GenerateUV2");

		EditorGUILayout.Separator();
		GUILayout.Label("Generation Settings");
		GUILayout.Box(GUIContent.none, _Line, GUILayout.ExpandWidth(true), GUILayout.Height(1f));

		//EditorGUI.BeginChangeCheck();
		//EditorGUILayout.PropertyField(SourceType);
		//if (EditorGUI.EndChangeCheck())
		//{
		//	if ((FacetedTerrain.TerrainSourceType)SourceType.enumValueIndex == FacetedTerrain.TerrainSourceType.UnityTerrain)
		//	{
		//		// Create the terrain if necessary
		//		var terrainCmp = terrain.GetComponent<Terrain>();
		//		if (terrainCmp == null || terrainCmp.terrainData == null)
		//		{
		//			// Ask if user wants to create a terrain component and asset
		//			if (EditorUtility.DisplayDialog("Create Terrain Asset?", "Would you like to create a Unity Terrain asset?", "Yes", "Cancel"))
		//			{
		//				// Add the terrain Component
		//				if (terrainCmp == null)
		//				{
		//					terrainCmp = terrain.gameObject.AddComponent<Terrain>();
		//				}

		//				// Add the terrain data
		//				if (terrainCmp.terrainData == null)
		//				{
		//					// Add the data
		//					terrainCmp.terrainData = new TerrainData();
		//					string terrainDataPath = terrain.GetBasePath() + "/" + terrain.name + "_data.asset";
		//					AssetDatabase.CreateAsset(terrainCmp.terrainData, terrainDataPath);
		//				}
		//			}
		//			else
		//			{
		//				// Reset the value
		//				SourceType.enumValueIndex = (int)FacetedTerrain.TerrainSourceType.HeightMap;
		//			}

		//			// And make the changes stick
		//			terrainObj.ApplyModifiedProperties();
		//		}
		//	}
		//	else
		//	{
		//		// Delete terrain compoennt and data is necessary
		//		var terrainCmp = terrain.GetComponent<Terrain>();
		//		if (terrainCmp != null)
		//		{
		//			// Ask if user wants to delete the terrain asset
		//			if (EditorUtility.DisplayDialog("Delete Terrain Asset?", "This will delete the terrain asset, are you sure?", "Yes", "Cancel"))
		//			{
		//				// Delete the terrain data
		//				if (terrainCmp.terrainData != null)
		//				{
		//					// Add the data
		//					var path = AssetDatabase.GetAssetPath(terrainCmp.terrainData);
		//					terrainCmp.terrainData = null;
		//					AssetDatabase.DeleteAsset(path);
		//				}

		//				// Delete the terrain Component
		//				GameObject.DestroyImmediate(terrainCmp);
		//			}
		//			else
		//			{
		//				// Reset the value
		//				SourceType.enumValueIndex = (int)FacetedTerrain.TerrainSourceType.UnityTerrain;
		//			}

		//			// And make the changes stick
		//			terrainObj.ApplyModifiedProperties();
		//		}
		//	}
		//}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(SourceHeightMapType);
		if (EditorGUI.EndChangeCheck())
		{
			// Clear the file path
			SourceRawHeightMapFile.stringValue = "";
		}

		bool canGenerate = true;
		if (SourceHeightMapType.enumValueIndex == (int)LowPolyTerrain.HeightmapType.Bitmap)
		{
			EditorGUILayout.PropertyField(SourceHeightMap);
			canGenerate = SourceHeightMap.objectReferenceValue != null;
		}
		else
		{
			EditorGUILayout.BeginHorizontal();
			if (SourceHeightMapType.enumValueIndex == (int)LowPolyTerrain.HeightmapType.Raw16)
			{
				EditorGUILayout.TextField("Raw-16 Heightmap", System.IO.Path.GetFileName(SourceRawHeightMapFile.stringValue));
				if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(45.0f)))
				{
					SourceRawHeightMapFile.stringValue = EditorUtility.OpenFilePanelWithFilters("Select Raw file", SourceRawHeightMapFile.stringValue, new string[] { "16-bit Raw", "r16,raw", "Allfiles", "*" });

					// Try to determine the map size
					var info = new System.IO.FileInfo(SourceRawHeightMapFile.stringValue);
					RawHeightMapSize.intValue = Mathf.RoundToInt(Mathf.Sqrt(info.Length / sizeof(System.UInt16)));
				}
				canGenerate = System.IO.File.Exists(SourceRawHeightMapFile.stringValue);
			}
			else
			{
				EditorGUILayout.TextField("Raw-32 Heightmap", System.IO.Path.GetFileName(SourceRawHeightMapFile.stringValue));
				if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(45.0f)))
				{
					SourceRawHeightMapFile.stringValue = EditorUtility.OpenFilePanelWithFilters("Select Raw file", SourceRawHeightMapFile.stringValue, new string[] { "32-bit Raw", "r32", "Allfiles", "*" });

					// Try to determine the map size
					var info = new System.IO.FileInfo(SourceRawHeightMapFile.stringValue);
					RawHeightMapSize.intValue = Mathf.RoundToInt(Mathf.Sqrt(info.Length / sizeof(System.Single)));
				}
				canGenerate = System.IO.File.Exists(SourceRawHeightMapFile.stringValue);
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Raw Map Size");
			RawHeightMapSize.intValue = EditorGUILayout.IntField(RawHeightMapSize.intValue, GUILayout.MinWidth(50.0f));
			GUILayout.Label("Byte Order");
			RawHeightMapOrder.enumValueIndex = (int)(LowPolyTerrain.ByteOrder)EditorGUILayout.EnumPopup((LowPolyTerrain.ByteOrder)RawHeightMapOrder.enumValueIndex, GUILayout.Width(80.0f));
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.PropertyField(GenerateVertColors);
		if (GenerateVertColors.boolValue)
		{
			EditorGUILayout.PropertyField(SourceColorMap);
		}
		EditorGUILayout.PropertyField(TerrainSize);
		EditorGUILayout.PropertyField(TerrainHeight);

		int chunkCount = -1;
		EditorGUILayout.PropertyField(ChunkSize);
		if (ChunkSize.intValue == 0)
		{
			EditorGUILayout.HelpBox("Chunk Size of 0 is invalid", MessageType.Warning);
			canGenerate = false;
		}
		else if (ChunkSize.intValue > TerrainSize.intValue)
		{
			EditorGUILayout.HelpBox("Chunk Size can not be larger than terrain size", MessageType.Warning);
			canGenerate = false;
		}
		else if (ChunkSize.intValue != 0)
		{
			if (TerrainSize.intValue % ChunkSize.intValue != 0)
			{
				EditorGUILayout.HelpBox("Chunk Size isn't a divisor of Terrain Size", MessageType.Warning);
				canGenerate = false;
			}
			else
			{
				chunkCount = TerrainSize.intValue / ChunkSize.intValue;
				chunkCount *= chunkCount;

				EditorGUI.BeginDisabledGroup(true);
				{
					EditorGUILayout.IntField("Chunk Count", chunkCount);
				}
				EditorGUI.EndDisabledGroup();

				if (chunkCount > 4096)
				{
					EditorGUILayout.HelpBox("Chunk Size of " + ChunkSize.intValue + " with terrain size of  " + TerrainSize.intValue + " would generate too many chunk objects (>" + chunkCount + ") and impact performance. Try to stay under a total of 4096 chunks.", MessageType.Warning);
					canGenerate = false;
				}
			}
		}

		int meshVertCount = -1;
		int colliderResolution = -1;
		EditorGUILayout.PropertyField(BaseResolution);
		if (BaseResolution.intValue == 0)
		{
			EditorGUILayout.HelpBox("Base Resolution of 0 is invalid", MessageType.Warning);
			canGenerate = false;
		}
		else if (BaseResolution.intValue > ChunkSize.intValue)
		{
			EditorGUILayout.HelpBox("Base Resolution can not be greater than the Chunk Size", MessageType.Warning);
			canGenerate = false;
		}
		else if (BaseResolution.intValue != 0)
		{
			if (ChunkSize.intValue % BaseResolution.intValue != 0)
			{
				EditorGUILayout.HelpBox("Base Resolution (size of smallest quad) isn't a divisor of Chunk Size", MessageType.Warning);
				canGenerate = false;
			}
			else
			{
				meshVertCount = (ChunkSize.intValue + 2) / BaseResolution.intValue;
				meshVertCount *= meshVertCount * 6;

				colliderResolution = TerrainSize.intValue / BaseResolution.intValue + 1;

				EditorGUI.BeginDisabledGroup(true);
				{
					EditorGUILayout.IntField("Collider Resolution", colliderResolution);
					EditorGUILayout.IntField("Highest Mesh Vert Count", meshVertCount);
				}
				EditorGUI.EndDisabledGroup();

				if (meshVertCount > 65000)
				{
					EditorGUILayout.HelpBox("Chunk Size of " + ChunkSize.intValue + " with base resolution of  " + BaseResolution.intValue + " would generate too many verts (>" + meshVertCount + "). The limit is 65000 verts per mesh.", MessageType.Warning);
					canGenerate = false;
				}

				if (!IsPowerOfTwoPlusOne(colliderResolution))
				{
					EditorGUILayout.HelpBox("Collider Resolution must be in the form (power-of-two + 1)\n(ColliderResolution = (TerrainSize/BaseResolution) + 1)", MessageType.Warning);
					canGenerate = false;
				}
			}
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(LODLevels);
		if (EditorGUI.EndChangeCheck())
		{
			// Update the distances on the terrain.
			terrainObj.ApplyModifiedProperties();
			terrain.UpdateDistances();
		}
		int resolution = BaseResolution.intValue;
		bool LODLevelsOk = true;
		for (int lod = 0; LODLevelsOk && lod < terrain.LODLevels; ++lod)
		{
			LODLevelsOk = resolution != 0 && ChunkSize.intValue % resolution == 0;
			if (LODLevelsOk)
			{
				resolution *= 2;
			}
		}
		if (!LODLevelsOk)
		{
			EditorGUILayout.HelpBox("Chunk Size of " + ChunkSize.intValue + " can't accomodate " + LODLevels.intValue + " LOD Levels with a base resolution of " + BaseResolution.intValue, MessageType.Warning);
			canGenerate = false;
		}

		EditorGUILayout.PropertyField(RandomYOffset);
		EditorGUILayout.PropertyField(UniformTriangles);
		if (!UniformTriangles.boolValue)
		{
			EditorGUILayout.PropertyField(RandomXZOffset);
			EditorGUILayout.HelpBox("Non uniform triangles and XZ Offset will make your terrain look better but cause the collision geometry to be slightly off from render geometry", MessageType.Info);
		}
		EditorGUILayout.PropertyField(HideChunksInHierarchy);

		int lodVertCount = chunkCount * meshVertCount;
		int totalVertCount = lodVertCount;
		for (int i = 1; i < LODLevels.intValue; ++i)
		{
			lodVertCount /= 4;
			totalVertCount += lodVertCount;
		}

		EditorGUI.BeginDisabledGroup(true);
		{
			EditorGUILayout.IntField("Total Vert Count", totalVertCount);
		}
		EditorGUI.EndDisabledGroup();
		if (totalVertCount > 10000000)
		{
			EditorGUILayout.HelpBox("Total Vert Count is High (>10,000,000), it may take a while to generate", MessageType.Info);
		}
		EditorGUI.BeginDisabledGroup(true);
		{
			var generator = new LowPolyTerrainGenerator(terrain);
			EditorGUILayout.ObjectField("Terrain Data Asset", generator.GetFirstMesh(), typeof(Object), false);
		}
		EditorGUI.EndDisabledGroup();

		EditorGUI.BeginDisabledGroup(!canGenerate);
		{
			if (GUILayout.Button("Generate Meshes"))
			{
				terrainObj.ApplyModifiedProperties();
				var generator = new LowPolyTerrainGenerator(terrain);
				generator.GenerateMeshes();
				EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
			}
		}
		EditorGUI.EndDisabledGroup();

		EditorGUILayout.Separator();
		GUILayout.Label("Runtime Settings");
		GUILayout.Box(GUIContent.none, _Line, GUILayout.ExpandWidth(true), GUILayout.Height(1f));

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(TerrainMaterial);
		EditorGUILayout.PropertyField(TerrainAlphaMaterial);
		if (EditorGUI.EndChangeCheck())
		{
			// Assign material to all renderers
			terrainObj.ApplyModifiedProperties();
			var generator = new LowPolyTerrainGenerator(terrain);
			generator.UpdateRenderers();
		}
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(CastShadows);
		if (EditorGUI.EndChangeCheck())
		{
			// turn on/off shadow casting
			terrainObj.ApplyModifiedProperties();
			var generator = new LowPolyTerrainGenerator(terrain);
			generator.UpdateShadowCasting();
		}
		if (CastShadows.boolValue)
		{
			EditorGUILayout.HelpBox("Shadow casting is experimental and has a few issues. Make sure that your lights have a high shadow bias, otherwise, lod levels will cast shadows on each other.", MessageType.Info);
		}
		GUILayout.Label("LOD Distances");
		EditorGUI.indentLevel++;
		for (int i = 0; i < LODDistances.arraySize; ++i)
		{
			if (i < LODDistances.arraySize - 1)
			{
				EditorGUILayout.PropertyField(LODDistances.GetArrayElementAtIndex(i), new GUIContent("LOD" + i.ToString() + "->LOD" + (i + 1).ToString()));
			}
			else
			{
				EditorGUILayout.PropertyField(LODDistances.GetArrayElementAtIndex(i), new GUIContent("LOD" + i.ToString() + "->OFF"));
			}
			if ( i > 0)
			{
				if (LODDistances.GetArrayElementAtIndex(i).floatValue <= LODDistances.GetArrayElementAtIndex(i-1).floatValue)
				{
					EditorGUILayout.HelpBox("LOD switching distances should be increasing!", MessageType.Warning);
				}
			}
		}
		EditorGUI.indentLevel--;
		if (LODDistances.GetArrayElementAtIndex(0).floatValue < ChunkSize.intValue)
		{
			EditorGUILayout.HelpBox("Your first LOD Distance is less than the chunk size, you may never get to see the lowest LOD level!", MessageType.Warning);
		}
		EditorGUILayout.PropertyField(LODTransitionTime);
		EditorGUILayout.PropertyField(FlipFlopPercent);
		terrainObj.ApplyModifiedProperties();
	}

	bool IsPowerOfTwoPlusOne(int value)
	{
		for (int i = 0; i < 31; ++i)
		{
			if (value == ((1 << i) + 1))
				return true;
		}
		return false;
	}
}
