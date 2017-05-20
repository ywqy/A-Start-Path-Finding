using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinding : MonoBehaviour {

	public Transform seek,target;

	Grid grid;

	void Awake() {
		grid = GetComponent<Grid> ();
	}

	void Update() {
		FindPath (seek.position, target.position);
	}

	void FindPath(Vector3 startPos, Vector3 endPos){
		Node startNode = grid.NodeFromWorldPoint (startPos);
		Node endNode = grid.NodeFromWorldPoint (endPos);

		List<Node> openSet = new List<Node> ();
		HashSet<Node> closeSet = new HashSet<Node> ();

		openSet.Add (startNode);

		while (openSet.Count > 0) {
			Node currentNode = openSet [0];
			for (int i = 0; i < openSet.Count; i++) {
				if (openSet [i].fCost < currentNode.fCost ||
					openSet [i].fCost == currentNode.fCost && openSet [i].hCost < currentNode.hCost) {
					currentNode = openSet [i];
				}
			}
			openSet.Remove (currentNode);
			closeSet.Add (currentNode);

			if (currentNode == endNode) {
				RetracePath (startNode, endNode);
				return;
			}

			foreach (Node neighbourNode in grid.GetNeighbours(currentNode)) {
				if (!neighbourNode.walkable || closeSet.Contains (neighbourNode))
					continue;
				int newMovementCostToNeighbour = currentNode.gCost + GetDistance (currentNode, neighbourNode);
				if (newMovementCostToNeighbour < neighbourNode.gCost || !openSet.Contains (neighbourNode)) {
					neighbourNode.gCost = newMovementCostToNeighbour;
					neighbourNode.hCost = GetDistance (neighbourNode, endNode);
					neighbourNode.parent = currentNode;

					if (!openSet.Contains (neighbourNode)) {
						openSet.Add (neighbourNode);
					}
				}

			}

		}

	}

	void RetracePath(Node startNode, Node endNode) {
		List<Node> path = new List<Node> ();
		Node currentNode = endNode;

		while (currentNode != startNode) {
			path.Add (currentNode);
			currentNode = currentNode.parent;
		}
		path.Reverse ();

		grid.path = path;
	}

	int GetDistance(Node nodeA,Node nodeB) {
		int dstX = Mathf.Abs (nodeA.gridX - nodeB.gridX);
		int dstY = Mathf.Abs (nodeA.gridY - nodeB.gridY);

		if (dstX > dstY) {
			return (14 * dstY + 10 * (dstX - dstY));
		}
		return (14 * dstX + 10 * (dstY - dstX));
	}
}
