﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//calculates the score thresholds for getting stars
public class starThreshold : MonoBehaviour {
	//fractions are the necessary fraction of caterpillars out of the total which player needs to get a streak from
	//ie, star1Frac = 3, player (on average!!) needs to get at least 3 straight streaks. Thus average top streak will be 1/3 of total caterpillars.
	//number of far shots necessary will also depend on this (frac = 3, need 1/3 far shots)
	//Therefore frac cannot be less than 1 (need an impossible score to get the star)
	public float star1Frac;
	public float star2Frac;
	public float star3Frac;

	private int totalCaterpillars;
	private int farShotBonus;

	public int threshold1{ get; set; }
	public int threshold2{ get; set; }
	public int threshold3{ get; set; }

	// Use this for initialization
	void Start () {
		totalCaterpillars = GetComponent<caterpillarManager> ().totalCaterpillars;
		farShotBonus = GetComponent<scoreCount> ().farShotBonus;

		//ensure threhsolds are only set if they are attainable
		if (checkFrac ()) {
			threshold1 = calcThreshold (star1Frac);
			threshold2 = calcThreshold (star2Frac);
			threshold3 = calcThreshold (star3Frac);
		} else {
			Debug.Log ("Required score threshold is too high for star!");
		}
	}
		
	bool checkFrac() {
		if (star1Frac < 1 || star2Frac < 1 || star3Frac < 1) {
			return false;
		} else {
			return true;
		}
	}

	int calcThreshold(float frac) {
		float AC = (float)totalCaterpillars / frac;
		float apxScore = (AC * (AC + 1.0f) / 2.0f) * frac + AC*farShotBonus;
		int scoreThreshold = Mathf.FloorToInt(apxScore);
		return scoreThreshold;
	}
}