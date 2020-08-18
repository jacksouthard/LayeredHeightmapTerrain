using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightmapRenderer : MonoBehaviour
{
	float[,] heightmap;

	MeshFilter mf;
	MeshRenderer mr;

    public void RenderHeightmap (float[,] heightmap) {
		this.heightmap = heightmap;

		mf = GetComponent<MeshFilter>();
		mr = GetComponent<MeshRenderer>();

		MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightmap, 0, false);
		Mesh newMesh = meshData.CreateMesh(true);
		mf.sharedMesh = newMesh;
	}

	public void CombineFrom (float[,] result, float time) {
		StartCoroutine(CombineFromRoutine(result, time));
	}
	IEnumerator CombineFromRoutine (float[,] result, float time) {
		float timer = 0;
		float[,] newMap = (float[,])heightmap.Clone();
		int size = newMap.GetLength(0);
		while (timer < time) {
			timer += Time.deltaTime;
			float p = timer / time;
			p *= p;
			for (int y = 0; y < size; y++) {
				for (int x = 0; x < size; x++) {
					newMap[x, y] = Mathf.Lerp(heightmap[x, y], result[x, y], p);
				}
			}
			MeshData meshData = MeshGenerator.GenerateTerrainMesh(newMap, 0, false);
			Mesh newMesh = meshData.CreateMesh(true);
			mf.sharedMesh = newMesh;
			yield return new WaitForEndOfFrame();
		}
		heightmap = newMap;
	}

	public void CombineTo (Transform other, float time) {
		StartCoroutine(CombineFromRoutine(other, time));
	}
	IEnumerator CombineFromRoutine (Transform other, float time) {
		Material mat = mr.material;
		float timer = 0;
		Vector3 startPos = transform.position;
		while (timer < time) {
			timer += Time.deltaTime;
			float p = timer / time;
			float q = 1 - p;
			mat.SetFloat("_Alpha", q * q * q);
			//transform.position = Vector3.Lerp(startPos, other.position, p);
			yield return new WaitForEndOfFrame();
		}
		Destroy(gameObject);
		TerrainShowcaseController.instance.CombineComplete();
	}

	public void FadeInToPosition (Vector3 pos, float time) {
		StartCoroutine(FadeInToPositionRoutine(pos, time));
	}
	IEnumerator FadeInToPositionRoutine (Vector3 pos, float time) {
		Material mat = mr.material;
		float timer = 0;
		Vector3 startPos = transform.position;
		while (timer < time) {
			timer += Time.deltaTime;
			float p = timer / time;
			mat.SetFloat("_Alpha", p * p);
			transform.position = Vector3.Lerp(startPos, pos, p);
			yield return new WaitForEndOfFrame();
		}
		mat.SetFloat("_Alpha", 1);
	}
}
