using UnityEngine;
using System.Collections;

public static class MeshGenerator {
	// CREDIT: Sebastian Lague: https://www.youtube.com/channel/UCmtyQOKKmrMVaKuRXz02jbQ

	public static MeshData GenerateTerrainMesh(float[,] heightMap, int levelOfDetail, bool useFlatShading) {
		int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2; // 2

		int borderedSize = heightMap.GetLength (0); // 7
		int meshSize = borderedSize - 2*meshSimplificationIncrement; // 3
		int meshSizeUnsimplified = borderedSize - 2; // 5

		float topLeftX = (meshSizeUnsimplified - 1) / -2f;
		float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

		
		int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;
		// if full (1): 5     4 + 1
		// if low (2): 3      2 + 1
		// if lower (4): 2    1 + 1
		int tilesPerSide = borderedSize - 3;
		for (int i = 0; i < levelOfDetail; i++) {
			tilesPerSide /= 2;
			if (tilesPerSide < 1) {
				tilesPerSide = 1;
				break;
			}
		}
		verticesPerLine = tilesPerSide + 1;
		//Debug.Log("Generating mesh with " + verticesPerLine + " verticies per line");

		MeshData meshData = new MeshData (verticesPerLine,useFlatShading);

		int[,] vertexIndicesMap = new int[borderedSize,borderedSize];
		int meshVertexIndex = 0;
		int borderVertexIndex = -1;

		int curIncrement = 1;
		//for (int y = 0; y < borderedSize; y++) {
		//	for (int x = 0; x < borderedSize; x++) {
		//		vertexIndicesMap[x, y] = 100;
		//	}
		//}
		for (int y = 0; y < borderedSize; y += curIncrement) {
			for (int x = 0; x < borderedSize; x += curIncrement) {
				if (x == 0 || x == borderedSize - 2) curIncrement = 1;
				else curIncrement = meshSimplificationIncrement;

				bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;
				//Debug.Log(new Vector2Int(x, y) + " border: " + isBorderVertex);
				if (isBorderVertex) {
					vertexIndicesMap [x, y] = borderVertexIndex;
					borderVertexIndex--;
				} else {
					vertexIndicesMap [x, y] = meshVertexIndex;
					meshVertexIndex++;
				}
			}
			if (y == 0 || y == borderedSize - 2) curIncrement = 1;
			else curIncrement = meshSimplificationIncrement;
		}
		//for (int y = 0; y < borderedSize; y++) {
		//	string lineStr = y + " | ";
		//	for (int x = 0; x < borderedSize; x++) {
		//		int val = vertexIndicesMap[x, y];
		//		lineStr += val == 100 ? "N " : val + " ";
		//	}
		//	Debug.Log(lineStr);
		//}

		int curIncrementX;
		int curIncrementY;
		for (int y = 0; y < borderedSize; y += curIncrementY) {
			if (y == 0 || y == borderedSize - 2) curIncrementY = 1;
			else curIncrementY = meshSimplificationIncrement;
			for (int x = 0; x < borderedSize; x += curIncrementX) {
				if (x == 0 || x == borderedSize - 2) curIncrementX = 1;
				else curIncrementX = meshSimplificationIncrement;

				int vertexIndex = vertexIndicesMap [x, y];
				//Vector2 percent = new Vector2 ((x- curIncrement) / (float)meshSize, (y- curIncrement) / (float)meshSize);
				float height = heightMap [x, y];
				//Vector3 vertexPosition = new Vector3 (topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);
				Vector3 vertexPosition = new Vector3(topLeftX + x - 1, height, topLeftZ - y + 1);

				meshData.AddVertex (vertexPosition, Vector2.zero, vertexIndex);

				if (x < borderedSize - 1 && y < borderedSize - 1) {
					int a = vertexIndicesMap [x, y];
					int b = vertexIndicesMap [x + curIncrementX, y];
					int c = vertexIndicesMap [x, y + curIncrementY];
					int d = vertexIndicesMap [x + curIncrementX, y + curIncrementY];
					//Debug.Log(new Vector2Int(x, y) + " | Inc: " + new Vector2Int(curIncrementX, curIncrementY) + " | a: " + a + " b: " + b + " c: " + c + " d: " + d);
					meshData.AddTriangle (a,d,c);
					meshData.AddTriangle (d,a,b);
				}

				vertexIndex++;
			}
		}

		meshData.ProcessMesh ();

		return meshData;

	}

	// OPTIMIZED MESH
	public static Mesh GetFlatMesh (int size) {
		float vertDist = (float)size / 2f;
		Vector3[] verts = new Vector3[4];
		verts[0] = new Vector3(-vertDist, 0f, vertDist); // top left
		verts[1] = new Vector3(vertDist, 0f, vertDist); // top right
		verts[2] = new Vector3(-vertDist, 0f, -vertDist); // bottom left
		verts[3] = new Vector3(vertDist, 0f, -vertDist); // bottom right

		int[] triangles = new int[6];
		triangles[0] = 0;
		triangles[1] = 1;
		triangles[2] = 2;
		triangles[3] = 3;
		triangles[4] = 2;
		triangles[5] = 1;

		Vector2[] uvs = new Vector2[4];
		uvs[0] = new Vector2(0, 1);
		uvs[1] = new Vector2(1, 1);
		uvs[2] = new Vector2(0, 0);
		uvs[3] = new Vector2(1, 0);

		Vector3[] normals = new Vector3[4];
		for (int i = 0; i < 4; i++) {
			normals[i] = Vector3.up;
		}

		Mesh mesh = new Mesh();
		mesh.SetVertices(verts);
		mesh.SetTriangles(triangles, 0);
		mesh.SetUVs(0, uvs);
		mesh.SetNormals(normals);
		return mesh;
	}
}

public class MeshData {
	Vector3[] vertices;
	int[] triangles;
	Vector2[] uvs;
	Vector3[] bakedNormals;

	Vector3[] borderVertices;
	int[] borderTriangles;

	int triangleIndex;
	int borderTriangleIndex;

	bool useFlatShading;

	public MeshData(int verticesPerLine, bool useFlatShading) {
		this.useFlatShading = useFlatShading;

		vertices = new Vector3[verticesPerLine * verticesPerLine];
		uvs = new Vector2[verticesPerLine * verticesPerLine];
		triangles = new int[(verticesPerLine-1)*(verticesPerLine-1)*6];

		borderVertices = new Vector3[verticesPerLine * 4 + 4];
		borderTriangles = new int[24 * verticesPerLine];
	}

	public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
		if (vertexIndex < 0) {
			borderVertices [-vertexIndex - 1] = vertexPosition;
		} else {
			vertices [vertexIndex] = vertexPosition;
			uvs [vertexIndex] = uv;
		}
	}

//	public void MoveVertex (float newHieght, int vertexIndex)
//	{
//		vertices[vertexIndex].y = newHieght;
//	}

	public void AddTriangle(int a, int b, int c) {
		if (a < 0 || b < 0 || c < 0) {
			borderTriangles [borderTriangleIndex] = a;
			borderTriangles [borderTriangleIndex + 1] = b;
			borderTriangles [borderTriangleIndex + 2] = c;
			borderTriangleIndex += 3;
			//Debug.Log("Add border tris");
		} else {
			triangles [triangleIndex] = a;
			triangles [triangleIndex + 1] = b;
			triangles [triangleIndex + 2] = c;
			triangleIndex += 3;
			//Debug.Log("Add normal tris");
		}
	}

	Vector3[] CalculateNormals() {

		Vector3[] vertexNormals = new Vector3[vertices.Length];
		int triangleCount = triangles.Length / 3;
		for (int i = 0; i < triangleCount; i++) {
			int normalTriangleIndex = i * 3;
			int vertexIndexA = triangles [normalTriangleIndex];
			int vertexIndexB = triangles [normalTriangleIndex + 1];
			int vertexIndexC = triangles [normalTriangleIndex + 2];

			Vector3 triangleNormal = SurfaceNormalFromIndices (vertexIndexA, vertexIndexB, vertexIndexC);
			vertexNormals [vertexIndexA] += triangleNormal;
			vertexNormals [vertexIndexB] += triangleNormal;
			vertexNormals [vertexIndexC] += triangleNormal;
		}

		int borderTriangleCount = borderTriangles.Length / 3;
		for (int i = 0; i < borderTriangleCount; i++) {
			int normalTriangleIndex = i * 3;
			int vertexIndexA = borderTriangles [normalTriangleIndex];
			int vertexIndexB = borderTriangles [normalTriangleIndex + 1];
			int vertexIndexC = borderTriangles [normalTriangleIndex + 2];

			Vector3 triangleNormal = SurfaceNormalFromIndices (vertexIndexA, vertexIndexB, vertexIndexC);
			if (vertexIndexA >= 0) {
				vertexNormals [vertexIndexA] += triangleNormal;
			}
			if (vertexIndexB >= 0) {
				vertexNormals [vertexIndexB] += triangleNormal;
			}
			if (vertexIndexC >= 0) {
				vertexNormals [vertexIndexC] += triangleNormal;
			}
		}


		for (int i = 0; i < vertexNormals.Length; i++) {
			vertexNormals [i].Normalize ();
		}

		return vertexNormals;
	}

	Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
		Vector3 pointA = (indexA < 0)?borderVertices[-indexA-1] : vertices [indexA];
		Vector3 pointB = (indexB < 0)?borderVertices[-indexB-1] : vertices [indexB];
		Vector3 pointC = (indexC < 0)?borderVertices[-indexC-1] : vertices [indexC];

		Vector3 sideAB = pointB - pointA;
		Vector3 sideAC = pointC - pointA;
		return Vector3.Cross (sideAB, sideAC).normalized;
	}

	public void ProcessMesh() {
		if (useFlatShading) {
			FlatShading ();
		} else {
			BakeNormals ();
		}
	}

	void BakeNormals() {
		bakedNormals = CalculateNormals ();
	}

	void FlatShading() {
		Vector3[] flatShadedVertices = new Vector3[triangles.Length];
		Vector2[] flatShadedUvs = new Vector2[triangles.Length];

		for (int i = 0; i < triangles.Length; i++) {
			flatShadedVertices [i] = vertices [triangles [i]];
			flatShadedUvs [i] = uvs [triangles [i]];
			triangles [i] = i;
		}

		vertices = flatShadedVertices;
		uvs = flatShadedUvs;
	}

	public Mesh CreateMesh (bool needNewMesh, Mesh mesh = null)
	{
		if (needNewMesh) {
			mesh = new Mesh ();
		}
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		if (useFlatShading) {
			mesh.RecalculateNormals ();
		} else {
			mesh.normals = bakedNormals;
		}
		return mesh;
	}
}