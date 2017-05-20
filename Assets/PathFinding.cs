using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System;

public class PathFinding : MonoBehaviour {

	PathRequestManager requestManager;
	Grid grid;

	void Awake() {
		requestManager = GetComponent<PathRequestManager> ();
		grid = GetComponent<Grid> ();

	}

	public void StartFindPath(Vector3 startPos, Vector3 endPos) {
		StartCoroutine (FindPath (startPos, endPos));
	}

	IEnumerator FindPath(Vector3 startPos, Vector3 endPos){

		Stopwatch sw = new Stopwatch ();
		sw.Start ();

		Vector3[] waypoints = new Vector3[0];
		bool pathSuccess = false;

		Node startNode = grid.NodeFromWorldPoint (startPos);
		Node endNode = grid.NodeFromWorldPoint (endPos);

		if (startNode.walkable && endNode.walkable) {
			Heap<Node> openSet = new Heap<Node> (grid.MaxSize);
			HashSet<Node> closeSet = new HashSet<Node> ();

			openSet.Add (startNode);

			while (openSet.Count > 0) {
				Node currentNode = openSet.RemoveFirst ();
				closeSet.Add (currentNode);

				if (currentNode == endNode) {
					sw.Stop ();
					print (sw.ElapsedMilliseconds);
					pathSuccess = true;

					break;
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
		yield return null;
		if (pathSuccess) {
			waypoints = RetracePath (startNode, endNode);
		}
		requestManager.FinishedProcessingPath (waypoints, pathSuccess);
	}

	Vector3[] RetracePath(Node startNode, Node endNode) {
		List<Node> path = new List<Node> ();
		Node currentNode = endNode;

		while (currentNode != startNode) {
			path.Add (currentNode);
			currentNode = currentNode.parent;
		}
		Vector3[] wayPoints = SimplifyPath (path);
		Array.Reverse (wayPoints);
		return wayPoints;
	}

	Vector3[] SimplifyPath(List<Node> path) {
		List<Vector3> waypoints = new List<Vector3> ();
		Vector2 directionOld = Vector2.zero;

		for (int i = 1; i < path.Count; i++) {
			Vector2 directionNew = new Vector2 (path [i - 1].gridX - path [i].gridX, path [i - 1].gridY - path [i].gridY);
			if (directionNew != directionOld) {
				waypoints.Add (path [i].worldPosition);
			}
			directionOld = directionNew;

		}
		return waypoints.ToArray ();
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
