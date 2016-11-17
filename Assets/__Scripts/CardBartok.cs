using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// CBState includes both states for the game and to___ states for movement 
public enum CBState {
	drawpile,
	toHand,
	hand,
	toTarget,
	target,
	discard,
	to,
	idle
}


public class CardBartok : Card {
	static public float MOVE_DURATION=0.5f;
	static public string MOVE_EASING = Easing.InOut;
	static public float CARD_HEIGHT = 3.5f;
	static public float CARD_WIDTH = 2f;

	public CBState state = CBState.drawpile;

	//Fields to store info the card will use to move and rotate
	public List<Vector3> bezierPts;
	public List <Quaternion> bezierRots;
	public float timeStart, timeDuration;

	public GameObject reportFinishTo = null;

	public void MoveTo(Vector3 ePos, Quaternion eRot){
		bezierPts = new List<Vector3> ();
		bezierPts.Add (transform.localPosition);
		bezierPts.Add (ePos);
		bezierRots = new List<Quaternion> ();
		bezierRots.Add (transform.rotation);
		bezierRots.Add (eRot);

		if (timeStart == 0) {
			timeStart = Time.time;
		}
		timeDuration = MOVE_DURATION;

		state = CBState.to;
	}
	public void MoveTo(Vector3 ePos){
		MoveTo (ePos, Quaternion.identity);
	}
		
	void Update () {
		switch (state) {
		// All the to___ states are ones where the card is interpolating case CBState.toHand:
		case CBState.toTarget:
		case CBState.to:
			// Get u from the current time and duration
			// u ranges from 0 to 1 (usually)
			float u = (Time.time - timeStart)/timeDuration;

			// Use Easing class from Utils to curve the u value 
			float uC = Easing.Ease (u, MOVE_EASING);

			if (u<0) { // If u<0, then we shouldn't move yet. 
				// Stay at the initial position 
				transform.localPosition = bezierPts[0];
				transform.rotation = bezierRots[0];
				return;
			} else if (u>=1) { // If u>=1, we're finished moving
				uC = 1; // Set uC=1 so we don't overshoot
				// Move from the to___ state to the following state
				if (state == CBState.toHand) state = CBState.hand;
				if (state == CBState.toTarget) state = CBState.toTarget;
				if (state == CBState.to) state = CBState.idle;
				// Move to the final position
				transform.localPosition = bezierPts[bezierPts.Count-1];
				transform.rotation = bezierRots[bezierPts.Count-1];
				// Reset timeStart to 0 so it gets overwritten next time 
				timeStart = 0;

				if (reportFinishTo != null) { //If there's a callback GameObject 
					// ... then use SendMessage to call the CBCallback method 
					// with this as the parameter. 
					reportFinishTo.SendMessage("CBCallback", this);
					// After calling SendMessage(), reportFinishTo must be set 
					// to null so that it the card doesn't continue to report
					// to the same GameObject every subsequent time it moves.
					reportFinishTo = null;
				} else { // If there is nothing to callback
					// Do nothing 
				}
				} else { // 0<=u<1, which means that this is interpolating now 
				// Use Bezier curve to move this to the right point
				Vector3 pos = Utils.Bezier(uC, bezierPts);
				transform.localPosition = pos;
				Quaternion rotQ = Utils.Bezier(uC, bezierRots);
				transform.rotation = rotQ;
			
			}
			break;
		}
	}
}
