using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class MapEncoder : Singleton<MapEncoder> {
	float[,] vertexHeightMap;

	// called by terrain manager on start if not generating fresh
	public void RetrieveSavedMap () {
        Texture2D savedTexture = Resources.Load<Texture2D>("SavedMaps/map");
		if (savedTexture == null) {
			TerrainManager.instance.SavedMapNotFound();
			return;
		}
		DecodeMap(savedTexture);
		TerrainManager.instance.MapSuccessfullyDecoded(vertexHeightMap);
	}

	public void SetHeightMap (float[,] heightMap) {
		vertexHeightMap = heightMap;
	}

	const float max8BitValues = 256;
	public void SaveMapToFile () {
		int width = vertexHeightMap.GetLength(0) - 1;
		Texture2D texture = new Texture2D(width, width, TextureFormat.RGB24, false);

		if (vertexHeightMap.GetLength(0) < width) {
			Debug.LogError("Vertex height map not large enough to encode. Min width: " + width);
			return;
		}

		Color32[] color32s = new Color32[width * width];
		for (int y = 0; y < width; y++) {
			for (int x = 0; x < width; x++) {
				byte r = (byte)(vertexHeightMap[x, y] * 255f); // heightmap on red channel
				byte g = r;
				byte b = r;
				byte a = 1;
				color32s[y * width + x] = new Color32(r, g, b, a);
			}
		}
		texture.SetPixels32(color32s);
		texture.Apply();

		Debug.Log("Successfully encoded map. Saving...");

		SaveTextureAsPNG(texture);
	}

	public void DecodeMap (Texture2D texture) {
		int width = texture.width; // the width of the prop map. the vertex map needs to be one bigger
		vertexHeightMap = new float[width + 1, width + 1];
		
		Color32[] color32s = texture.GetPixels32();
		for (int y = 0; y < width; y++) {
			for (int x = 0; x < width; x++) {
				Color32 val = color32s[y * width + x];
				float heightVal = (float)val.r / 255f;
				vertexHeightMap[x, y] = heightVal;

				// since the vertex heightmap extends one beyond the texture we need to extend the border verticies
				bool yBorder = y == width - 1;
				bool xBorder = x == width - 1;
				if (yBorder && xBorder) {
					vertexHeightMap[x + 1, y + 1] = heightVal;
				} else if (yBorder) {
					vertexHeightMap[x, y + 1] = heightVal;
				} else if (xBorder) {
					vertexHeightMap[x + 1, y] = heightVal;
				}
			}
		}
	}

	// FILE SAVING
	public static Texture2D GetTextureFromHeightmap (float[,] heightmap) {
		// assume square
		int width = heightmap.GetLength(0);
		Debug.Log("Getting texture from heightmap with dimension " + width + ". Transforming to " + (width - 1));
		width--;

		Texture2D texture = new Texture2D(width, width, TextureFormat.RGB24, false);

		for (int y = 0; y < width; y++) {
			for (int x = 0; x < width; x++) {
				Color color = GetColorFromHeight(heightmap[x, y]);
				texture.SetPixel(x, y, color);
			}
		}

		texture.Apply(false, false);
		return texture;
	}

	static Color GetColorFromHeight (float height) {
		return Color.Lerp(Color.black, Color.white, height);
	}

	const string savePath = "/Resources/SavedMaps/";
	public static void SaveTextureAsPNG (Texture2D texture, string fileName = "map") {
		//first Make sure you're using RGB24 as your texture format
		//Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);

		//then Save To Disk as PNG
		byte[] bytes = texture.EncodeToPNG();
		string dirPath = Application.dataPath + savePath;
		if (!Directory.Exists(dirPath)) {
			Directory.CreateDirectory(dirPath);
		}
		File.WriteAllBytes(dirPath + fileName + ".png", bytes);
	}
}
