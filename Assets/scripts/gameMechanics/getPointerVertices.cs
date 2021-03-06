﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Draws pointer from rock up to top of the screen showing player where their shot will go
	 * Creates 3 raycasts to find next collidable object - one in middle of rock trajectory, one lining the rock's top edge, one lining the rock's bottom edge.
	 * If top edge raycast or bottom edge raycast finds a collider not found by the center raycast, the pointer stops at this collider as it will not create a horizontal shot
	 * Otherwise first collider found by the mid ray will be added to the pointer vertices.
	 * If rock trajectory will go downwards due to the shot no pointer is cast past this point. Communicates to player their shot will not work.
	 * offset between contact point of ray and collider and the rock position on contact due to rock thickness is calculated and used when casting the next ray
	 * If offset means rock will actually not land within the collider when the ray says it will, horizontal reflection is set to false so no pointer is cast
	 * Once there are no longer any objects found ray is drawn between last object and the top of the screen where the rock will go through.
	 * Pointer also shortens as player shoots until it disappears entirely
	*/
public class getPointerVertices : MonoBehaviour {
	private float radius;
	private GameObject springAnchor;

	private bool horizontalReflection;			//determines whether shot will reflect in x axis or y axis. If set to false pointer is halted to show players their shot probably won't work.
	private Vector3 rayDirection;				//current direction of the ray
	private List<Vector3> pointerVertices;		//list of points on the pointer
	private RaycastHit2D[] collidersFound;		//colliders found from center raycast
	private RaycastHit2D[] topCollidersFound;	//colliders found from top raycast
	private RaycastHit2D[] botCollidersFound;	//colliders found from bottom raycast
	private Vector2 rayOffsets;					//x and y offsets from center of rock to bottom and top raycasts. Calculated from ray direction.

	private RaycastHit2D nextCollider;

	private Vector3 nextColliderPoint;
	private Vector2 rockOffsets;
	// Use this for initialization
	void Start () {
		radius = GetComponent<projectileShoot> ().radius;
		springAnchor = rockManager.Instance.springAnchor;
		pointerVertices = new List<Vector3> ();
		horizontalReflection = true;	//initialise horizontal reflection. Is true if ray reflects in x direction. Assume true to start with.
	}

	public void updatePointer() {
		//find direction between rock and spring anchor (pointer direction)
		rayDirection = springAnchor.transform.position - transform.position;
		rayOffsets = getRayOffsets ();

		//find colliders that ray intersects with and add initial vertex
		collidersFound = Physics2D.RaycastAll (transform.position, rayDirection);	//create center raycast
		pointerVertices.Add(collidersFound [0].point);	//add point at rock itself
		horizontalReflection = true;					//assumed true to start with

		raycastEdges (transform.position);		//find top and bottom ray colliders

		//if mid ray collider length is equal to one must have only detected the rock
		if (collidersFound.Length == 1) {
			//check for collisions on edge rays from the first collider object
			checkEdgeCollisionsFirst ();
		}

		//if ray finds another collider besides initial rock a second ray must be cast
		if (collidersFound.Length > 1) {
			checkEdgeCollisionsSecond ();
			checkEdgeCollisionsThird ();


			if (horizontalReflection) {
				rockOffsets = pointerOffsets (rayDirection, reflectsInX(collidersFound[1]));	//find offsets of pointer vertex due to rock thickness
				//add point with offsets taken into account
				nextColliderPoint = new Vector3 (collidersFound [1].point.x + rockOffsets.x, collidersFound [1].point.y + rockOffsets.y);
				pointerVertices.Add (nextColliderPoint);
			}
			//if y offset takes rock below the minimum y co-ordinate on the collider the rock will not have a horizontal reflection
			if (collidersFound [1].collider.bounds.min.y > nextColliderPoint.y) {
				horizontalReflection = false;
			}

			//find new colliders from new ray direction
			setNextCollider ();
			//if next collider is default raycasthit2D then it does not exist
			while (nextCollider != default(RaycastHit2D) && horizontalReflection) {
				//add next collider to pointer vertices
				pointerVertices.Add (nextColliderPoint);
				setNextCollider ();
			}
		}
		//only need to add final point at top of the screen if ray reflects horizontally
		if (horizontalReflection) {
			float endXPos = pointerXpos (pointerVertices [pointerVertices.Count - 1], rayDirection, ScreenVariables.worldHeight);
			pointerVertices.Add (new Vector3 (endXPos, ScreenVariables.worldHeight));
		}
		drawPointerLine (pointerVertices);
		pointerVertices.Clear ();
	}

	//Difference between rock center co-ordinates and point on rock edge at which top and bottom rays are cast
	Vector2 getRayOffsets() {
		float sinAngle = Mathf.Abs(rayDirection.y) / Mathf.Pow (Mathf.Pow(rayDirection.y, 2.0f) + Mathf.Pow(rayDirection.x, 2.0f), 0.5f);
		float cosAngle = Mathf.Abs(rayDirection.x) / Mathf.Pow (Mathf.Pow(rayDirection.y, 2.0f) + Mathf.Pow(rayDirection.x, 2.0f), 0.5f);

		float deltaX = radius * sinAngle;
		float deltaY = radius * cosAngle;
		return new Vector2 (deltaX, deltaY);
	}

	//raycast at top and bottom edges of rock to have the ability to detect all things the rock will collide with, not just things the middle of the rock will collide with
	void raycastEdges(Vector3 midRayPoint) {
		if (rayDirection.x > 0) {
			topCollidersFound = Physics2D.RaycastAll (new Vector3 (midRayPoint.x - rayOffsets.x, midRayPoint.y + rayOffsets.y), rayDirection);
			botCollidersFound = Physics2D.RaycastAll (new Vector3 (midRayPoint.x + rayOffsets.x, midRayPoint.y - rayOffsets.y), rayDirection);
		} else {
			topCollidersFound = Physics2D.RaycastAll (new Vector3 (midRayPoint.x + rayOffsets.x, midRayPoint.y + rayOffsets.y), rayDirection);
			botCollidersFound = Physics2D.RaycastAll (new Vector3 (midRayPoint.x - rayOffsets.x, midRayPoint.y - rayOffsets.y), rayDirection);
		}
		List<RaycastHit2D> topColliderList = new List<RaycastHit2D> ();
		List<RaycastHit2D> botColliderList = new List<RaycastHit2D> ();

		foreach (RaycastHit2D collider in topCollidersFound) {
			if (!collider.collider.gameObject.CompareTag ("rock")) {
				topColliderList.Add (collider);
			}
		}
		foreach (RaycastHit2D collider in botCollidersFound) {
			if (!collider.collider.gameObject.CompareTag ("rock")) {
				botColliderList.Add (collider);
			}
		}

		//redefine both top and bottom colliders array without rock collision. This is because rock collider is sometimes detected by edge rays but not always.
		//thus must be removed to produce reliable results.
		topCollidersFound = topColliderList.ToArray ();
		botCollidersFound = botColliderList.ToArray ();
	}

	/*checks for bottom and top colliders for when center detects no colliders (besides rock itself)
	 * if both bottom and top have a collider they are compared and the collider with a lower y position is used to end the pointer (as this is the collider the pointer finds first) */
	void checkEdgeCollisionsFirst () {
		if (botCollidersFound.Length == 1 && topCollidersFound.Length == 1) {
			if (topCollidersFound [0].point.y < botCollidersFound [0].point.y) {
				endPointerFromEdgeRays (topCollidersFound [0], transform.position);
			} else {
				endPointerFromEdgeRays (botCollidersFound [0], transform.position);
			}
		} else if (botCollidersFound.Length == 1) {
			endPointerFromEdgeRays (botCollidersFound [0], transform.position);
		} else if (topCollidersFound.Length == 1) {
			endPointerFromEdgeRays (topCollidersFound [0], transform.position);
		}
	}

	//check for collisions on edge rays when center ray has same number of colliders as top or bottom ray
	//as center ray always includes rock itself and other rays don't there must be an extra collider detected by edge rays
	//if first collider in bottom or top ray does not match second collider of center ray pointer must stop
	void checkEdgeCollisionsSecond() {
		if (topCollidersFound.Length == collidersFound.Length) {
			if (topCollidersFound [0].collider != collidersFound[1].collider) {
				endPointerFromEdgeRays (topCollidersFound [0], transform.position);
			}
		}
		if (botCollidersFound.Length == collidersFound.Length) {
			if (botCollidersFound [0].collider != collidersFound[1].collider) {
				endPointerFromEdgeRays (botCollidersFound [0], transform.position);
			}
		}
	}

	//if any colliders are found by top or bottom ray checks if second collider in center ray matches first collider in top or bottom ray
	//if a match is not made the ray with the collider with a lower y position is used to end the pointer as this is the first collision the pointer will meet
	void checkEdgeCollisionsThird() {
		if (topCollidersFound.Length > 0 && botCollidersFound.Length > 0) {
			if (collidersFound [1].collider != topCollidersFound [0].collider || collidersFound [1].collider != botCollidersFound [0].collider) {
				if (botCollidersFound [0].point.y < topCollidersFound [0].point.y) {
					if (botCollidersFound [0].point.y < collidersFound [1].point.y) {
						endPointerFromEdgeRays (botCollidersFound [0], transform.position);
					}
				} else {
					if (topCollidersFound [0].point.y < collidersFound [1].point.y) {
						endPointerFromEdgeRays (topCollidersFound [0], transform.position);
					}
				}
			}
		} else if (topCollidersFound.Length > 0) {
			if (collidersFound [1].collider != topCollidersFound [0].collider && topCollidersFound [0].point.y < collidersFound [1].point.y) {
				endPointerFromEdgeRays (topCollidersFound [0], transform.position);
			}
		} else if (botCollidersFound.Length > 0) {
			if (collidersFound [1].collider != botCollidersFound [0].collider && botCollidersFound [0].point.y < collidersFound [1].point.y) {
				endPointerFromEdgeRays (botCollidersFound [0], transform.position);
			}
		}
	}

	//ends pointer by setting horizontal reflection to false so that no other points are added to the pointer
	//called when rock is about to hit a collider on its edge
	void endPointerFromEdgeRays(RaycastHit2D collider, Vector3 midRayPoint) {
		horizontalReflection = false;
		rockOffsets = pointerOffsets (rayDirection, horizontalReflection);	//find offsets of pointer vertex due to rock thickness
		float colliderXpos = pointerXpos(midRayPoint, rayDirection, collider.point.y);
		//add point with offsets taken into account
		nextColliderPoint = new Vector3 (colliderXpos + rockOffsets.x, collider.point.y + rockOffsets.y);
		pointerVertices.Add (nextColliderPoint);

	}
		
	//find whether ray is reflected by a collider in x or y direction
	bool reflectsInX(RaycastHit2D collider) {
		if (collider.normal.y == 0) {
			return true;
		} else {
			return false;
		}
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

	//update last collider point, next collider and next collider point
	//ends pointer if the edge rays collide with something the center ray doesn't
	//ends pointer if rock offsets cause next collision point to end up outside of collider bounds (will cause an unpredictable collision)
	void setNextCollider() {
		setRayDirection();
		rayOffsets = getRayOffsets ();
		raycastEdges (nextColliderPoint);
		collidersFound = Physics2D.RaycastAll (nextColliderPoint, new Vector3 (rayDirection.x, rayDirection.y));

		checkCollidersMatch(collidersFound, topCollidersFound);
		setNextColliderPoint ();

		if (horizontalReflection) {
			checkCollidersMatch (collidersFound, botCollidersFound);
		}
		setNextColliderPoint ();

	}

	//if first collider found on edge ray is not equal to first collider found on center ray pointer ends
	void checkCollidersMatch(RaycastHit2D[] centerRayColliders, RaycastHit2D[] edgeRayColliders) {
		if (edgeRayColliders.Length > 0) {
			if (centerRayColliders.Length > 0) {
				if (edgeRayColliders[0].collider != centerRayColliders [0].collider) {
					if (edgeRayColliders [0].point.y < centerRayColliders [0].point.y) {
						endPointerFromEdgeRays (edgeRayColliders [0], nextColliderPoint);
					}
				}
			} else {
				endPointerFromEdgeRays (edgeRayColliders [0], nextColliderPoint);
			}
		}
	}

	//if rock will not hit anything at its edges next collider point is set
	void setNextColliderPoint() {
		if (horizontalReflection) {
			nextCollider = getNextCollider (collidersFound, nextColliderPoint, rayDirection);
			nextColliderPoint = getNextColliderPoint (nextCollider, rayDirection, nextColliderPoint);
			checkRockIsWithinColliderBounds (nextCollider, nextColliderPoint);
		}
	}

	void setRayDirection() {
		if (horizontalReflection) {
			rayDirection = new Vector3 (-rayDirection.x, rayDirection.y);
		} else {
			rayDirection = new Vector3 (rayDirection.x, -rayDirection.y);
		}
	}

	//find if there is another collider
	RaycastHit2D getNextCollider(RaycastHit2D[] colliders, Vector3 lastColliderPoint, Vector3 rayDirection) {
		RaycastHit2D nextCollider;
		switch (colliders.Length) {
		//if there is no collider next collider does not exist
		case 0:
			nextCollider = default(RaycastHit2D);
			break;
		//if there is a collider next one should be the first one ray contacts
		default:
			nextCollider = colliders [0];
			break;
		}
		return nextCollider;
	}



	//next collider point is point at which ray hit next collider plus offsets from the thickness of the rock
	Vector3 getNextColliderPoint(RaycastHit2D collider, Vector3 rayDirection, Vector3 thisColliderPoint) {
		Vector3 nextColliderPoint = thisColliderPoint;
		if (collider != default(RaycastHit2D)) {
			//here horizontal reflection is only used to calculate offsets. Not meant to update global for finding if rock goes downwards
			bool horizontalReflection = reflectsInX (collider);
			Vector2 offsets = pointerOffsets (rayDirection, horizontalReflection);
			nextColliderPoint = collider.point;
			nextColliderPoint = new Vector3 (nextColliderPoint.x + offsets.x, nextColliderPoint.y + offsets.y);
		}
		return nextColliderPoint;
	}

	//if there is a valid collider (not default collider as this means there is no collider) and rock is not within collider bounds there cannot be a horizontal reflection
	//point at the collider is then added but no further points will be as the ray must stop at this point
	void checkRockIsWithinColliderBounds (RaycastHit2D nextCollider, Vector3 nextColliderPoint) {
		if (nextCollider != default(RaycastHit2D) && nextCollider.collider.bounds.min.y > nextColliderPoint.y) {
			horizontalReflection = false;
			pointerVertices.Add (nextColliderPoint);
		}
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

	//adds points on pointer to line renderer so they can be drawn out
	void drawPointerLine(List<Vector3> pointerVertices) {
		//set pointer indices equal to points at which ray intersects walls
		LineRenderer[] allRenderers = GetComponentsInChildren<LineRenderer> ();
		LineRenderer pointer;
		foreach (LineRenderer renderer in allRenderers) {
			if (renderer.gameObject != this.gameObject) {
				pointer = renderer;
				if (rockManager.Instance.rockNumber != 1) {
					fadePointer ();
				}

				pointer.numPositions = pointerVertices.Count;

				//offset texture with time so that it dashed line moves
				//scale texture with pointer magnitude so that individual dashes do not change size
				float pointerMagnitude = getPointerMagnitude ();
				pointer.material.SetTextureOffset("_MainTex", new Vector2(-Time.timeSinceLevelLoad, 0f));
				pointer.material.SetTextureScale("_MainTex", new Vector2(pointerMagnitude, 1f));

				pointer.SetPositions (pointerVertices.ToArray());

			}
		}
	}
		
	float getPointerMagnitude() {
		float pointerMagnitude = 0;
		for (int i = 0; i < pointerVertices.Count; i++) {
			if (i != 0) {
				pointerMagnitude += (pointerVertices [i] - pointerVertices [i - 1]).magnitude;
			}
		}
		return pointerMagnitude;
	}

	//make pointer shorten the more shots the player has fired
	void fadePointer() {
		//get y position of final vertex on pointer
		float yPos = pointerEndYpos ();

		int totalVertices = pointerVertices.Count;
		bool xPosNotFoundYet = true;
		float xPos = 0;

		//removes all pointer vertices larger than yPos
		//Using the direction along the point at which yPos is and the final point under yPos, x position of final pointer point can be found
		for (int i = 0; i < totalVertices; i++) {
			if (pointerVertices[i].y > yPos) {
				if (xPosNotFoundYet) {
					xPos = pointerXpos (pointerVertices [i], pointerVertices [i] - pointerVertices [i - 1], yPos);
					xPosNotFoundYet = false;
				}
				pointerVertices.Remove (pointerVertices[i]);
				totalVertices -= 1;
				i -= 1;

			}
		}

		//if x position is found add final point to vertices
		//if x position is not found this is not necessary as all points in the pointer were below the maximum y point anyway
		if (!xPosNotFoundYet) {
			Vector3 pointerEnd = new Vector3 (xPos, yPos);
			pointerVertices.Add (pointerEnd);
		}
	}

	float pointerEndYpos() {
		int currentShot = rockManager.Instance.rockNumber;
		float finishLine = caterpillarManager.Instance.finishLine;
		float maxHeightFraction = 1;
		//find fraction of total shooting area height the pointer will reach. The higher the current shot the lower the pointer will go.
		maxHeightFraction = 1.0f - ((float)currentShot - 1.0f) / GetComponentInParent<projectileShoot> ().shotsWithPointer;
		float yPos = finishLine + maxHeightFraction * (ScreenVariables.worldHeight - finishLine);
		return yPos;
	}
}
