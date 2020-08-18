using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightmapGenerator : MonoBehaviour
{
	public static float[,] GenerateHeightmap (int mapSize) {
		int size = mapSize + 1;// heightmaps need to be 1 bigger than the tile size

		bool showCase = TerrainShowcaseController.instance != null;

		float[,] borderMask = GenerateBorderMask(size, 0f);
		if (showCase) TerrainShowcaseController.instance.AddHeightmapToStack(borderMask);
		float[,] borderHills = GenerateHillMask(size, 1f);
		if (showCase) TerrainShowcaseController.instance.AddHeightmapToStack(borderHills);
		borderMask = MultiplyHeightMaps(borderMask, borderHills);
		if (showCase) TerrainShowcaseController.instance.QueCombine(borderMask);

		float[,] heightmap = GenerateHillMask(size, 0.4f);
		if (showCase) TerrainShowcaseController.instance.AddHeightmapToStack(heightmap);
		heightmap = MaxAddHeightmaps(borderMask, heightmap);
		if (showCase) TerrainShowcaseController.instance.QueCombine(heightmap);
		float[,] details = GenerateHillDetails(size);
		if (showCase) TerrainShowcaseController.instance.AddHeightmapToStack(details);
		heightmap = CombineHeightmaps(heightmap, details, 0.2f, true);
		if (showCase) TerrainShowcaseController.instance.QueCombine(heightmap);
		heightmap = CreateTerrace(heightmap, 0.3f, 0.3f);
		heightmap = CreateTerrace(heightmap, 0.6f, 0.4f);

		if (showCase) TerrainShowcaseController.instance.AddHeightmapToStack(heightmap);
		if (showCase) TerrainShowcaseController.instance.QueCombine(heightmap);

		MapEncoder.instance.SetHeightMap(heightmap);
		return heightmap;
	}

	static float[,] GenerateBorderMask (int size, float steepness) {
		float rampStart = size * Mathf.Lerp(0.3f, 0.46f, steepness);
		float rampEnd = size * 0.48f;
		float rampLength = rampEnd - rampStart;
		float centerPos = (float)size / 2f;

		float[,] noise = new float[size, size];

		for (int y = 0; y < size; y++) {
			for (int x = 0; x < size; x++) {
				float val;
				float dist = Mathf.Max(Mathf.Abs(x - centerPos), Mathf.Abs(y - centerPos));
				if (dist < rampStart) val = 0;
				else if (dist > rampEnd) val = 1;
				else val = (dist - rampStart) / rampLength;

				val--;
				val = 1 - val * val;
				noise[x, y] = val;
			}
		}
		return noise;
	}

	static float [,] GenerateHillMask (int size, float amount) {
		const float scale = 60;
		const int octaves = 2;
		const float persistance = 0.5f;
		const float lacunarity = 2f;
		float[,] noise = Noise.GenerateNoiseMap(size, size, (int)(Random.value * 1000f), scale, octaves, persistance, lacunarity, Vector2.zero, Noise.NormalizeMode.Local);

		amount = 1 - amount;
		float scaleRatio = 1f / (1f - amount);
		for (int y = 0; y < size; y++) {
			for (int x = 0; x < size; x++) {
				float val = noise[x, y];
				val -= amount;
				val *= scaleRatio;
				if (val < 0) val = 0;
				else {
					val--;
					val = 1 - val * val;
				}
				noise[x, y] = val;
			}
		}
		return noise;
	}

	static float[,] GenerateHillDetails (int size) {
		const float scale = 7;
		const int octaves = 3;
		const float persistance = 0.5f;
		const float lacunarity = 2f;
		return Noise.GenerateNoiseMap(size, size, (int)(Random.value * 1000f), scale, octaves, persistance, lacunarity, Vector2.zero, Noise.NormalizeMode.Local);
	}

	// helpers
	static float[,] CombineHeightmaps (float[,] source, float[,] addition, float additionPower, bool maintainZeros) {
		int size = source.GetLength(0);
		float sourcePower = 1f - additionPower;
		for (int y = 0; y < size; y++) {
			for (int x = 0; x < size; x++) {
				float val = source[x, y];
				if (!maintainZeros || val > 0)
					val = (val * sourcePower + addition[x, y] * additionPower) * val;
				source[x, y] = val;
			}
		}
		return source;
	}

	static float[,] MaxAddHeightmaps (float[,] source, float[,] addition) {
		int size = source.GetLength(0);
		for (int y = 0; y < size; y++) {
			for (int x = 0; x < size; x++) {
				float val = source[x, y];
				source[x, y] = Mathf.Max(val, addition[x,y]);
			}
		}
		return source;
	}

	static float[,] MinAddHeightmaps (float[,] source, float[,] addition) {
		int size = source.GetLength(0);
		for (int y = 0; y < size; y++) {
			for (int x = 0; x < size; x++) {
				float val = source[x, y];
				source[x, y] = Mathf.Min(val, addition[x, y]);
			}
		}
		return source;
	}

	static float[,] MultiplyHeightMaps (float[,] source, float[,] addition) {
		int size = source.GetLength(0);
		for (int y = 0; y < size; y++) {
			for (int x = 0; x < size; x++) {
				source[x, y] = source[x, y] * addition[x,y];
			}
		}
		return source;
	}

	static float[,] CreateTerrace (float[,] source, float terraceHeight, float terracePull) {
		int size = source.GetLength(0);
		for (int y = 0; y < size; y++) {
			for (int x = 0; x < size; x++) {
				float val = source[x, y];
				float diff = val - terraceHeight;
				if (diff > 0) {
					//if (diff < terracePull) val = terraceHeight;
					//else {
					val -= terracePull;
					val *= 1f / (1f - terracePull);
					if (val < terraceHeight) val = terraceHeight;
					//}
					source[x, y] = val;
				}
			}
		}
		return source;
	}

	// BASE FLATTENING
	public static float[,] FlattenAreas (float[,] heightMap, Vector2Int[] locations, float flatRadius, float smoothDistance) {
		int size = heightMap.GetLength(0);

		float normalStartRadius = flatRadius + smoothDistance;
		float diff = normalStartRadius - flatRadius;

		float[,] flattenMask = new float[size, size];
		for (int y = 0; y < size; y++) {
			for (int x = 0; x < size; x++) {
				Vector2Int curOrigin = new Vector2Int(x, y);
				float closestDist = GetSquareDistanceToClosestPoint(curOrigin, locations);
				float val;
				if (closestDist < flatRadius) val = 0;
				else if (closestDist > normalStartRadius) val = 1;
				else val = (closestDist - flatRadius) / diff;
				flattenMask[x, y] = val;
			}
		}
		//heightMap = MinAddHeightmaps(heightMap, flattenMask);
		//heightMap = CombineHeightmaps(heightMap, flattenMask, 0.5f, true);
		heightMap = MultiplyHeightMaps(heightMap, flattenMask);
		return heightMap;
	}

	static float GetSquareDistanceToClosestPoint (Vector2Int origin, Vector2Int[] points) {
		float closestDist = Mathf.Infinity;
		for (int i = 0; i < points.Length; i++) {
			float dist = GetSquareDistance(origin, points[i]);
			if (dist < closestDist) {
				closestDist = dist;
			}
		}
		return closestDist;
	}

	static float GetSquareDistance (Vector2 point1, Vector2 point2) {
		Vector2 diff = point2 - point1;
		return Mathf.Max(Mathf.Abs(diff.x), Mathf.Abs(diff.y));
	}
}
