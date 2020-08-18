using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainManager : Singleton<TerrainManager> {
	public GameObject chunkPrefab;

	public bool generateFreshMap;

	const float heightScale = 15;

	public const int mapSize = 128; // number of tiles in width
	public const int chunkSize = 32;  // number of tiles in width
	Vector3 offset;

	float[,] vertexHeightmap;
	float[,] tileHeightMap; // the heights of the tiles, not verticies

	int chunksPerSide;
	TerrainChunk[,] chunks;

    public const float UNWALKABLE_HEIGHT = 100;

	private void Awake () {
        if (generateFreshMap) {
            GenerateFreshMap();
		} else {
            MapEncoder.instance.RetrieveSavedMap();
		}
    }

    public void MapSuccessfullyDecoded (float[,] vertexHeightmap) {
        Debug.Log("Map successfully decoded");
		this.vertexHeightmap = vertexHeightmap;
		GenerateTerrain();
    }

    public void SavedMapNotFound () {
        Debug.Log("Saved map not found. Generating fresh...");
        GenerateFreshMap();
	}

	void GenerateFreshMap () {
		vertexHeightmap = HeightmapGenerator.GenerateHeightmap(mapSize);
		GenerateTerrain();

		// saving the map
		MapEncoder.instance.SetHeightMap(vertexHeightmap);
		MapEncoder.instance.SaveMapToFile();
    }

    void GenerateTerrain () {
		chunksPerSide = mapSize / chunkSize;
		chunks = new TerrainChunk[chunksPerSide, chunksPerSide];

		float offsetSize = mapSize / 2f - chunkSize / 2f + 0.5f;
		offset = new Vector3(-offsetSize, 0f, offsetSize);

		for (int y = 0; y < chunksPerSide; y++) {
			for (int x = 0; x < chunksPerSide; x++) {
				CreateChunk(x, y);
			}
		}

		GenerateTileHeightmap(true);
	}

	void CreateChunk (int x, int y) {
		Vector3 pos = new Vector3(x * chunkSize, 0f, -y * chunkSize) + offset;
		TerrainChunk newChunk = Instantiate(chunkPrefab, pos, Quaternion.identity, transform).GetComponent<TerrainChunk>();
		newChunk.name = "Chunk (" + x + "," + y + ")";
		newChunk.Initiate(x, y);
		chunks[x, y] = newChunk;
	}

	// helpers
	// returns true if is completely flat
	public bool ExtractChunkHeightmap (int chunkX, int chunkY, out float[,] map) {
		int heightmapWidth = chunkSize + 1 + 2; // the plus 2 because we need an extra set of verticies beyond terrain to caluculate normals
		map = new float[heightmapWidth, heightmapWidth];

		int startMapX = chunkX * chunkSize - 1;
		int endMapX = startMapX + heightmapWidth - 1;
		int startMapY = chunkY * chunkSize - 1;
		int endMapY = startMapY + heightmapWidth - 1;

		int retOffsetX = chunkX * -chunkSize + 1;
		int retOffsetY = chunkY * -chunkSize + 1;

		bool isFlat = true;

		for (int y = startMapY; y <= endMapY; y++) {
			for (int x = startMapX; x <= endMapX; x++) {
				float height = GetVertexHeight(x, y);
				map[x + retOffsetX, y + retOffsetY] = height;

				if (isFlat && height != 0) isFlat = false;
			}
		}

		return isFlat;
	}

	void GenerateTileHeightmap (bool initial) {
		if (initial) tileHeightMap = new float[mapSize, mapSize];

		for (int y = 0; y < mapSize; y++) {
			for (int x = 0; x < mapSize; x++) {
				Vector2Int topLeftPixel = new Vector2Int(x, y);
				Vector2Int topRightPixel = new Vector2Int(x + 1, y);
				Vector2Int bottomLeftPixel = new Vector2Int(x, y + 1);
				Vector2Int bottomRightPixel = new Vector2Int(x + 1, y + 1);
				float[] heights = new float[4];
				heights[0] = GetVertexHeight(topLeftPixel.x, topLeftPixel.y);
				heights[1] = GetVertexHeight(topRightPixel.x, topRightPixel.y);
				heights[2] = GetVertexHeight(bottomLeftPixel.x, bottomLeftPixel.y);
				heights[3] = GetVertexHeight(bottomRightPixel.x, bottomRightPixel.y);

				float height = GetTileHeightMapValueFromCornerHeights(heights);
				tileHeightMap[x, y] = height;
			}
		}
	}

	public void TileBlocked (Vector2Int tilePos) {
		tileHeightMap[tilePos.x, tilePos.y] = 10000;
	}

	const float minHeightDifferenceForUnwalkable = 1f;
	float GetTileHeightMapValueFromCornerHeights (float[] cornerHeights) {
		float sum = cornerHeights[0];
		for (int i = 0; i < 4; i++) {
			int lastIndex;
			if (i == 0) {
				lastIndex = 3;
			} else {
				lastIndex = i - 1;
				sum += cornerHeights[i];
			}
			if (Mathf.Abs(cornerHeights[i] - cornerHeights[lastIndex]) > minHeightDifferenceForUnwalkable) {
				return 10000; // tile is unwalkable
			}
		}
		return sum / 4;
	}

	float GetVertexHeight (int mapX, int mapY) {
		if (mapX < 0) mapX = 0;
		else if (mapX > mapSize) mapX = mapSize;
		if (mapY < 0) mapY = 0;
		else if (mapY > mapSize) mapY = mapSize;
		return vertexHeightmap[mapX, mapY] * heightScale;
	}

	public Vector2Int GetTilePositionFromWorld (Vector3 worldPos) {
		Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.z);
		worldPos2D -= new Vector2(-mapSize, mapSize) / 2f;
		return new Vector2Int(Mathf.FloorToInt(worldPos2D.x), Mathf.FloorToInt(-worldPos2D.y));
	}

    public Vector3 GetWorldPosFromTile(Vector2Int tilePos) {
        return new Vector3(tilePos.x - (mapSize / 2), GetHeightAtTile(tilePos), -tilePos.y + (mapSize / 2));
    }

    public float GetHeightAtTile (Vector2Int tilePos) {
		return tileHeightMap[tilePos.x, tilePos.y];
	}

    public float GetHeightAtPos(Vector3 pos) {
        return GetHeightAtTile(GetTilePositionFromWorld(pos));
    }

	// chunk helpers
	public Vector2Int GetChunkPositionFromTile (Vector2Int tilePos) {
		return tilePos / chunkSize;
	}

	public Vector2Int GetTilePositionWithinChunk (Vector2Int tilePos) {
		return new Vector2Int(tilePos.x % chunkSize, tilePos.y % chunkSize);
	}

	public TerrainChunk GetChunkAtPosition (Vector2Int chunkPosition) {
		return chunks[chunkPosition.x, chunkPosition.y];
	}

	public TerrainChunk GetChunkAtTilePosition (Vector2Int tilePos) {
		Vector2Int chunkPos = GetChunkPositionFromTile(tilePos);
		return GetChunkAtPosition(chunkPos);
	}

	public Vector2Int GetChunkPositionAtWorldPosition (Vector3 worldPos) {
		Vector2Int tilePos = GetTilePositionFromWorld(worldPos);
		return GetChunkPositionFromTile(tilePos);
	}

	public TerrainChunk GetChunkAtWorldPosition (Vector3 worldPos) {
		Vector2Int tilePos = GetTilePositionFromWorld(worldPos);
		Vector2Int chunkPos = GetChunkPositionFromTile(tilePos);
		return GetChunkAtPosition(chunkPos);
	}

	public bool DoesChunkExistAtPosition (Vector2Int chunkPosition) {
		if (chunkPosition.x < 0 || chunkPosition.y < 0) return false;
		if (chunkPosition.x >= chunksPerSide || chunkPosition.y >= chunksPerSide) return false;
		return true;
	}
}
