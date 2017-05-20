using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class PathFinding : MonoBehaviour {

	public Transform seek,target;

	Grid grid;

	void Awake() {
		grid = GetComponent<Grid> ();
	}

	void Update() {
		if (Input.GetButtonDown ("Jump")) {
			FindPath (seek.position, target.position);
		}
	}

	void FindPath(Vector3 startPos, Vector3 endPos){

		Stopwatch sw = new Stopwatch ();
		sw.Start ();

		Node startNode = grid.NodeFromWorldPoint (startPos);
		Node endNode = grid.NodeFromWorldPoint (endPos);

		Heap<Node> openSet = new Heap<Node> (grid.MaxSize);
		HashSet<Node> closeSet = new HashSet<Node> ();

		openSet.Add (startNode);

		while (openSet.Count > 0) {
			Node currentNode = openSet.RemoveFirst ();
			closeSet.Add (currentNode);

			if (currentNode == endNode) {
				sw.Stop ();
				print (sw.ElapsedMilliseconds);
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
					openSet.UpdateItem (neighbourNode);
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
