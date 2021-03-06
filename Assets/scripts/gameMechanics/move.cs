﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//sets caterpillar position onto one of six lanes randomely and finds y co-ordinate for caterpillar spawn point based on screen height and caterpillar body lenght
//sets caterpillar speed based on spawn number and min max speeds set in inspector

public class move : MonoBehaviour {
	private int laneNumber;	//must be EVEN
	private float minVelocity;
	private float maxVelocity;

	private float yOffset;	//size of caterpillar
	private float ySize;	//offset of caterpillar
	private float yheadPos;	//position of caterpillar head from body in world co-ordinates

//	public float yMin { get; set; }	//minimum y value for caterpillar to be active. must be calculated here as makes use of caterpillar body length values.

	private int totalCaterpillars;
	private float finishLine;

	private bool endless;
	private float caterpillarNumToMaxSpeed;

	// Use this for initialization
	void Awake () {
		//grab caterpillar manager values
		laneNumber = caterpillarManager.Instance.lanes;
		minVelocity = caterpillarManager.Instance.minVel;
		maxVelocity = caterpillarManager.Instance.maxVel;
		totalCaterpillars = caterpillarManager.Instance.totalCaterpillars;
		finishLine = caterpillarManager.Instance.finishLine;
		endless = caterpillarManager.Instance.endlessLevel;
		caterpillarNumToMaxSpeed = caterpillarManager.Instance.caterpillarNumToMaxSpeed;

		setupPosition ();
		setIncreasedSpeed ();
		yheadPos = transform.TransformPoint (new Vector3 (0, yOffset - ySize, 0)).y - ScreenVariables.worldHeight;	//must subtract screenheight as the caterpillar spawns above screen
	}

	// Update is called once per frame
	void Update () {
		if (transform.position.y + yheadPos < finishLine) {
			GetComponent<BoxCollider2D> ().isTrigger = true;
		}
	}

	//makes caterpillar spawn so that its head is just above the screen
	//spawns in one of the lanes
	void setupPosition() {
		yOffset = GetComponent<BoxCollider2D> ().offset.y;
		ySize = GetComponent<BoxCollider2D> ().size.y/2;

		//finding x and y pos for caterpillar
		float yWorldDim = transform.TransformPoint (new Vector3 (0, yOffset + ySize, 0)).y;	//offset from screenheight spawn pos to make caterpillar spawn off the screen
		int lane = Random.Range (-laneNumber/2, laneNumber/2);	//find lane
		float laneWidth = 2*ScreenVariables.worldWidth / laneNumber;
		float xPos = (lane + 0.5f) * laneWidth; 
		float yPos = ScreenVariables.worldHeight + yWorldDim;

		transform.position = new Vector3 (xPos, yPos, 0);
	}

	//set up speed based on the caterpillar spawn number
	//speed starts at minVel and increases linearly to maxVel with spawn number
	void setIncreasedSpeed() {
		float speed = 0;
		int currentCaterpillar = caterpillarManager.Instance.currentSpawn;

		//if on endless mode need to use caterpillar number until max speed is hit
		if (endless) {
			//if current caterpillar number is larger than caterpillar number until max speed set speed equal to max speed
			//else speed is calculated the same way as in arcade mode but with different total caterpillar number
			if (currentCaterpillar > caterpillarNumToMaxSpeed) {
				speed = maxVelocity;
			} else {
				float deltaVelocity = (maxVelocity - minVelocity) / caterpillarNumToMaxSpeed;
				speed = minVelocity + currentCaterpillar * deltaVelocity;
			}
		} else {
			float deltaVelocity = (maxVelocity - minVelocity) / totalCaterpillars;
			speed = minVelocity + currentCaterpillar * deltaVelocity;
		}
		GetComponent<Rigidbody2D> ().velocity = new Vector3 (0, -speed, 0);
	}
}
