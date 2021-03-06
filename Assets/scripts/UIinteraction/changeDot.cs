﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class changeDot : MonoBehaviour {
	public Sprite whiteDot;
	public Sprite darkDot;
	private Image[] allDots;
	// Use this for initialization
	void Start () {
		allDots = GetComponentsInChildren<Image>();

		//make white dot correspond to whichever level player is focussing on
		allDots[currentLevelSelectLevel.currentLevel - 1].sprite = whiteDot;
	}
	
	// Update is called once per frame
	public void switchDotImage(int nextLevelNumber, int lastLevelNumber) {
		allDots[nextLevelNumber - 1].sprite = whiteDot;
		allDots[lastLevelNumber - 1].sprite = darkDot;
	}
}
