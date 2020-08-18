using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainShowcaseController : Singleton<TerrainShowcaseController>
{
	public GameObject heightmapRendererPrefab;
	public Transform cameraPivot;

	const float pauseTime = 1f;
	const float transitionTime = 2f;
	const float spacing = 50;

	private void Start () {
		//StartShowcase();
	}

	private void Update () {
		if (Input.GetKeyDown(KeyCode.Space)) {
			StartShowcase();
		}
	}

	float startTime;
	float duration;
	void StartShowcase () {
		startTime = Time.time;
		duration = (quedCombines.Count * (transitionTime + pauseTime)) + pauseTime + transitionTime * 3f;
		UpdateStackPositions(transitionTime);
		StartCoroutine(RotateCameraRoutine(1, duration));
	}

	List<HeightmapRenderer> heightmapStack = new List<HeightmapRenderer>();

	public void AddHeightmapToStack (float[,] heightmap) {
		heightmap = (float[,])heightmap.Clone();

		HeightmapRenderer newRenderer = Instantiate(heightmapRendererPrefab, transform).GetComponent<HeightmapRenderer>();
		newRenderer.RenderHeightmap(heightmap);
		heightmapStack.Insert(0, newRenderer);
	}

	// combine the top of the stack with the second from top
	List<float[,]> quedCombines = new List<float[,]>();
	public void QueCombine (float[,] result) {
		result = (float[,])result.Clone();
		quedCombines.Insert(0, result);
	}

	void StepShowcase () {
		if (quedCombines.Count == 0) {
			if (heightmapStack.Count == 0) {
				print("Finished in " + (Time.time - startTime) + "s | Predicted: " + duration + "s");
			} else {
				DoFinalHeightmap();
			}
		} else {
			int index = quedCombines.Count - 1; // oldest first
			CombineHeightmaps(quedCombines[index]);
			quedCombines.RemoveAt(index);
		}
	}
	IEnumerator StepOnDelay (float delay) {
		yield return new WaitForSeconds(delay);
		StepShowcase();
	}
	IEnumerator MoveCameraVerticallyRoutine (float yDelta, float time) {
		float timer = 0f;
		Vector3 startPos = cameraPivot.position;
		Vector3 endPos = startPos + Vector3.up * yDelta;
		while (timer < time) {
			timer += Time.deltaTime;
			float p = timer / time;
			cameraPivot.position = Vector3.Slerp(startPos, endPos, p);
			yield return new WaitForEndOfFrame();
		}
	}
	void MoveCameraToYLevel (float yLevel, float time) {
		float delta = yLevel - cameraPivot.position.y;
		StartCoroutine(MoveCameraVerticallyRoutine(delta, time));
	}
	IEnumerator RotateCameraRoutine (int numRevolutions, float time) {
		float timer = 0f;
		float targetAngle = 360f * numRevolutions;
		while (timer < time) {
			timer += Time.deltaTime;
			cameraPivot.eulerAngles = Vector3.up * Mathf.SmoothStep(0f, targetAngle, timer / time);
			yield return new WaitForEndOfFrame();
		}
	}

	void CombineHeightmaps (float[,] result) {
		if (heightmapStack.Count < 2) {
			print("Cant combine as there are less than 2 heightmaps in the stack");
			return;
		}

		HeightmapRenderer top = heightmapStack[heightmapStack.Count - 1];
		HeightmapRenderer newTop = heightmapStack[heightmapStack.Count - 2];
		top.CombineTo(newTop.transform, transitionTime);
		newTop.CombineFrom(result, transitionTime);

		heightmapStack.Remove(top);

		//StartCoroutine(MoveCameraVerticallyRoutine(-spacing, transitionTime));
	}

	public void CombineComplete () {
		StartCoroutine(StepOnDelay(pauseTime));
		//StepShowcase();
	}

	void DoFinalHeightmap () {
		heightmapStack[0].CombineTo(transform, transitionTime);
		heightmapStack.Clear();
		//StartCoroutine(MoveCameraVerticallyRoutine(-spacing, transitionTime));
	}

	void UpdateStackPositions (float time) {
		StartCoroutine(UpdateStackPositionsRoutine(time));
	}
	IEnumerator UpdateStackPositionsRoutine (float time) {
		float curHeight = 0;
		foreach (var renderer in heightmapStack) {
			curHeight += spacing;
			renderer.FadeInToPosition(Vector3.up * curHeight, time);
		}
		StartCoroutine(MoveCameraVerticallyRoutine(spacing * heightmapStack.Count, transitionTime));
		float delay = time * 2f;
		yield return new WaitForSeconds(delay);
		MoveCameraToYLevel(0, duration - delay);
		StepShowcase();
	}
}
