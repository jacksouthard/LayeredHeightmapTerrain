using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk : MonoBehaviour {
	int chunkX;
	int chunkY;

	float[,] heightMap;

	// references
	MeshFilter mf;
	MeshRenderer mr;

	public void Initiate (int _chunkX, int _chunkY) {
		// get references
		mf = GetComponent<MeshFilter>();
		mr = GetComponent<MeshRenderer>();

		chunkX = _chunkX;
		chunkY = _chunkY;

		// retreieve our assign heightmap
		TerrainManager.instance.ExtractChunkHeightmap(chunkX, chunkY, out heightMap);

		// generate our mesh
		MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap, 0, true);
		Mesh newMesh = meshData.CreateMesh(true);
		mf.mesh = newMesh;
	}

	Vector2Int GetPosition () {
		return new Vector2Int(chunkX, chunkY);
	}
}
