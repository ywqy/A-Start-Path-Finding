using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour {

	public bool displayGridGizmos;
	public LayerMask unwalkableMask;
	public Vector2 gridWorldSize;
	public float nodeRadius;
	public TerrainType[] walkableRegions;
	public int obstacleProximityPenalty = 10;
	LayerMask walkableMask;
	Dictionary<int,int> walkableRegionsDictionary = new Dictionary<int, int>();
	Node[,] grid;


	float nodeDiameter;
	int gridSizeX, gridSizeY;

	int peraltyMin = int.MaxValue;
	int peraltyMax = int.MinValue;


	void Awake() {
		nodeDiameter = 2 * nodeRadius;
		gridSizeX = Mathf.RoundToInt (gridWorldSize.x / nodeDiameter);
		gridSizeY = Mathf.RoundToInt (gridWorldSize.y / nodeDiameter);

		foreach (TerrainType region in walkableRegions) {
			walkableMask.value |= region.terrainMask.value;
			walkableRegionsDictionary.Add ((int)Mathf.Log (region.terrainMask.value,2), region.terrainPenlty);
		}

		CreateGrid ();
	}

	public int MaxSize {
		get{ 
			return gridSizeX * gridSizeY;
		}
	}

	void CreateGrid() {
		grid = new Node[gridSizeX, gridSizeY];
		Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;


		for (int x = 0; x < gridSizeX; x++) {
			for (int y = 0; y < gridSizeY; y++) {
				Vector3 worldPoint = worldBottomLeft + new Vector3 (x * nodeDiameter + nodeRadius, 0, y * nodeDiameter + nodeRadius);
				bool walkable = !(Physics.CheckSphere (worldPoint, nodeRadius, unwalkableMask));
				int movementPenalty = 0;

				Ray ray = new Ray (worldPoint + Vector3.up * 50, Vector3.down);
				RaycastHit hit;
				if (Physics.Raycast (ray, out hit, 100, walkableMask)) {
					walkableRegionsDictionary.TryGetValue (hit.collider.gameObject.layer, out movementPenalty);

				}
				if (!walkable) {
					movementPenalty += obstacleProximityPenalty;
				}

				grid [x, y] = new Node (walkable, worldPoint, x, y, movementPenalty);
			}
		}
		BlurPenaltyMap (3);
	}

	void BlurPenaltyMap(int blurSize) {
		int kernelSize = blurSize * 2 + 1;
		int kernelExtents = (kernelSize - 1) / 2;

		int[,] penaltiesHorizontalPass = new int[gridSizeX, gridSizeY];
		int[,] penaltiesVertialPass = new int[gridSizeX, gridSizeY];

		for (int y = 0; y < gridSizeY; y++) {
			for (int x = -kernelExtents; x <= kernelExtents; x++) {
				int sampleX = Mathf.Clamp (x, 0, kernelExtents);
				penaltiesHorizontalPass [0, y] += grid [sampleX, y].movementPenlty;
			}
			for (int x = 1; x < gridSizeX; x++) {
				int removeIndex = Mathf.Clamp (x - kernelExtents - 1, 0, gridSizeX);
				int addIndex = Mathf.Clamp (x + kernelExtents, 0, gridSizeX - 1);

				penaltiesHorizontalPass [x, y] = penaltiesHorizontalPass [x - 1, y] - grid [removeIndex, y].movementPenlty + grid [addIndex, y].movementPenlty;
			}
		}

		for (int x = 0; x < gridSizeX; x++) {
			for (int y = -kernelExtents; y <= kernelExtents; y++) {
				int sampleY = Mathf.Clamp (y, 0, kernelExtents);
				penaltiesVertialPass [x, 0] += penaltiesHorizontalPass [x, sampleY];
			}

			int bluredPenalty = Mathf.RoundToInt ((float)penaltiesVertialPass [x, 0] / (kernelSize * kernelSize));
			grid [x, 0].movementPenlty = bluredPenalty;

			for (int y = 1; y < gridSizeX; y++) {
				int removeIndex = Mathf.Clamp (y - kernelExtents - 1, 0, gridSizeY);
				int addIndex = Mathf.Clamp (y + kernelExtents, 0, gridSizeY - 1);

				penaltiesVertialPass [x, y] = penaltiesVertialPass [x, y - 1] - penaltiesHorizontalPass [x, removeIndex] + penaltiesHorizontalPass [x, addIndex];
				bluredPenalty = Mathf.RoundToInt ((float)penaltiesVertialPass [x, y] / (kernelSize * kernelSize));
				grid [x, y].movementPenlty = bluredPenalty;

				if (bluredPenalty > peraltyMax) {
					peraltyMax = bluredPenalty;
				}
				if (bluredPenalty < peraltyMin) {
					peraltyMin = bluredPenalty;
				}
			}
		}
	}

	public List<Node> GetNeighbours(Node node) {
		List<Node> neighbours = new List<Node> ();

		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				if (x == 0 && y == 0) continue;
				int checkX = node.gridX + x;
				int checkY = node.gridY + y;

				if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) {
					neighbours.Add (grid [checkX, checkY]);
				}
			}
		}
		return neighbours;
	}

	public Node NodeFromWorldPoint(Vector3 worldPosition) {
		float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
		float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;

		percentX = Mathf.Clamp01 (percentX);
		percentY = Mathf.Clamp01 (percentY);

		int x = Mathf.RoundToInt ((gridSizeX - 1) * percentX);
		int y = Mathf.RoundToInt ((gridSizeY - 1) * percentY);

		return grid [x, y];
	}
		
	void OnDrawGizmos() {
		Gizmos.DrawWireCube (transform.position, new Vector3 (gridWorldSize.x, 1, gridWorldSize.y));
		if (grid != null && displayGridGizmos) {
			foreach (Node node in grid) {
				Gizmos.color = Color.Lerp (Color.white, Color.black, Mathf.InverseLerp (peraltyMin, peraltyMax, node.movementPenlty));
				Gizmos.color = node.walkable ? Gizmos.color : Color.red;
				Gizmos.DrawCube (node.worldPosition, Vector3.one * (nodeDiameter));
			}
		}		
	}

	[System.Serializable]
	public class TerrainType {
		public LayerMask terrainMask;
		public int terrainPenlty;
	}

}
