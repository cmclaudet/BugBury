﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class getPointerVertices : MonoBehaviour {
	private float radius;
	private GameObject springAnchor;
	// Use this for initialization
	void Start () {
		radius = GetComponent<projectileShoot> ().radius;
		springAnchor = rockManager.Instance.springAnchor;
	}

	/* Draws pointer from rock up to top of the screen showing player where their shot will go
	 * Creates raycast to find next collidable object. Adds this object point to the pointer vertices.
	 * If rock trajectory will go downwards due to the shot no pointer is cast past this point. Communicates to player their shot will not work.
	 * Once there are no longer any objects found ray is drawn between last object and the top of the screen where the rock will go through.
	*/
	public void updatePointer() {
		//find direction between rock and spring anchor (pointer direction)
		Vector3 rayDirection = springAnchor.transform.position - transform.position;

		//find colliders that ray intersects with and add initial vertex
		RaycastHit2D[] collidersFound = Physics2D.RaycastAll (transform.position, rayDirection);
		List<Vector3> pointerVertices = new List<Vector3>();
		pointerVertices.Add(collidersFound [0].point);
		bool horizontalReflection = true;	//initialise horizontal reflection. Is true if ray reflects in x direction. Assume true to start with.

		//if ray finds another collider besides initial rock a second ray must be cast
		if (collidersFound.Length > 1) {
			//add point from last collider found to pointer vertices
			horizontalReflection = reflectsInX (collidersFound [1]);	//find out if this is a horizontal reflection
			Vector2 offsets = pointerOffsets (rayDirection, horizontalReflection);	//find offsets of pointer vertex due to rock thickness

			//add point with offsets taken into account
			Vector3 lastPoint = new Vector3 (collidersFound [1].point.x + offsets.x, collidersFound [1].point.y + offsets.y);
			pointerVertices.Add (lastPoint);


			if (collidersFound [1].collider.bounds.min.y > lastPoint.y) {
				horizontalReflection = false;
			}

			//find new colliders from new ray direction
			if (horizontalReflection) {
				rayDirection = new Vector3 (-rayDirection.x, rayDirection.y);
			} else {
				rayDirection = new Vector3 (rayDirection.x, -rayDirection.y);
			}
			RaycastHit2D[] colliders = Physics2D.RaycastAll (lastPoint, new Vector3 (rayDirection.x, rayDirection.y));
			RaycastHit2D nextCollider = getNextCollider (colliders, lastPoint, rayDirection);
			Vector3 nextColliderPoint = getNextColliderPoint (nextCollider, rayDirection, lastPoint);

			if (nextCollider != default(RaycastHit2D) && nextCollider.collider.bounds.min.y > nextColliderPoint.y) {
				horizontalReflection = false;
			}

			//if next collider y position is different to last one, need to cast another ray onto another collider
			while (nextColliderPoint.y != lastPoint.y && horizontalReflection) {
				//add next collider to pointer vertices
				pointerVertices.Add (nextColliderPoint);
				rayDirection = new Vector3 (-rayDirection.x, rayDirection.y);
				colliders = Physics2D.RaycastAll (nextColliderPoint, new Vector3 (rayDirection.x, rayDirection.y));
				lastPoint = nextColliderPoint;
				nextCollider = getNextCollider (colliders, lastPoint, rayDirection);
				nextColliderPoint = getNextColliderPoint (nextCollider, rayDirection, lastPoint);

				if (nextCollider != default(RaycastHit2D) && nextCollider.collider.bounds.min.y > nextColliderPoint.y) {
					horizontalReflection = false;
				}
			}
		}

		if (horizontalReflection) {
			//lastXPos will be inside screen bounds if rock's next destination is off the screen from the top or bottom
			float finalYpos = ScreenVariables.worldHeight;
			if (rayDirection.y < 0) {
				finalYpos = -ScreenVariables.worldHeight;
			}
			float endXPos = pointerXpos (pointerVertices [pointerVertices.Count - 1], rayDirection, finalYpos);
			pointerVertices.Add (new Vector3 (endXPos, finalYpos));
		}
		drawPointerLine (pointerVertices);

	}

	//finds x position at certain y value given the equation of a line
	float pointerXpos(Vector3 point, Vector3 dir, float yPos) {
		Vector3 p1 = point;
		Vector3 p2 = point + dir;
		float m = (p2.y - p1.y) / (p2.x - p1.x);
		float c = p1.y - m * p1.x;
		float XPos = (yPos - c) / m;
		return XPos;
	}

	//find whether ray is reflected by a collider in x or y direction
	bool reflectsInX(RaycastHit2D collider) {
		if (collider.normal.y == 0) {
			return true;
		} else {
			return false;
		}
	}

	//find point of next collider
	RaycastHit2D getNextCollider(RaycastHit2D[] colliders, Vector3 lastPoint, Vector3 rayDirection) {
		RaycastHit2D nextCollider;
		switch (colliders.Length) {
		case 0:
			//			nextColliderPoint = lastPoint;
			nextCollider = default(RaycastHit2D);
			break;
		case 1:
			if (colliders [0].point.y != lastPoint.y) {
				nextCollider = colliders [0];
			} else {
				nextCollider = default(RaycastHit2D);
			}
			break;
		default:
			if (colliders [0].point.y != lastPoint.y) {
				nextCollider = colliders [0];
			} else {
				nextCollider = colliders [1];
			}
			break;
		}
		return nextCollider;
	}

	//find x and y offsets of vertex at collision due to thickness of rock. Must be shifted so that the next ray cast goes to correct place.
	Vector2 pointerOffsets(Vector3 rayDirection, bool reflectsInX) {
		float deltax = radius;
		float deltay = -radius;
		float tanAngle = Mathf.Abs (rayDirection.x) / rayDirection.y;

		switch (reflectsInX) {
		case true:
			if (rayDirection.x > 0) {
				deltax = -radius;
			}
			if (rayDirection.y > 0) {
				deltay = -radius / tanAngle;
			} else {
				deltay = radius / tanAngle;
			}
			break;
		case false:
			tanAngle = rayDirection.y / Mathf.Abs (rayDirection.x);
			deltax = radius / tanAngle;
			if (rayDirection.x > 0) {
				deltax = -radius / tanAngle;
			}
			if (rayDirection.y < 0) {
				deltay = radius;
			}
			break;
		}
		return new Vector2 (deltax, deltay);
	}

	Vector3 getNextColliderPoint(RaycastHit2D collider, Vector3 rayDirection, Vector3 lastPoint) {
		Vector3 nextColliderPoint = lastPoint;
		if (collider != default(RaycastHit2D)) {
			bool reflectsHorizontally = reflectsInX (collider);
			Vector2 offsets = pointerOffsets (rayDirection, reflectsHorizontally);
			nextColliderPoint = collider.point;
			nextColliderPoint = new Vector3 (nextColliderPoint.x + offsets.x, nextColliderPoint.y + offsets.y);
		}
		return nextColliderPoint;
	}

	//adds points on pointer to line renderer so they can be drawn out
	void drawPointerLine(List<Vector3> pointerVertices) {
		//set pointer indices equal to points at which ray intersects walls
		LineRenderer[] allRenderers = GetComponentsInChildren<LineRenderer> ();
		LineRenderer pointer;
		foreach (LineRenderer renderer in allRenderers) {
			if (renderer.gameObject != this.gameObject) {
				pointer = renderer;
				pointer.numPositions = pointerVertices.Count;
				pointer.SetPositions (pointerVertices.ToArray());

			}
		}
	}
}
