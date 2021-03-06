﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This enum contains the different phases of a game turn 
public enum TurnPhase {
	idle,
	pre,
	waiting, 
	post, 
	gameOver
}

public class Bartok : MonoBehaviour {
	static public Bartok S;
	// This field is static to enforce that there is only 1 current player 
	static public Player CURRENT_PLAYER;

	public TextAsset deckXML;
	public TextAsset layoutXML;
	public Vector3 layoutCenter = Vector3.zero;


	// The number of degrees to fan each card in a hand 
	public float handFanDegrees = 10f;
	public int numStartingCards=7;
	public float drawTimeStagger=0.1f;
	public bool ________________;

	public Deck deck;
	public List<CardBartok> drawPile;
	public List<CardBartok> discardPile;

	public BartokLayout layout;
	public Transform layoutAnchor;

	public List<Player> players;
	public CardBartok targetCard;

	public TurnPhase phase = TurnPhase.idle;
	public GameObject turnLight;

	void Awake(){
		S = this;

		//Find the TurnLight by name
		turnLight = GameObject.Find("TurnLight");
	}

	void Start(){
		deck = GetComponent<Deck> ();
		deck.InitDeck (deckXML.text);
		Deck.Shuffle (ref deck.cards);


		layout = GetComponent<BartokLayout>(); // Get the Layout 
		layout.ReadLayout(layoutXML.text); // Pass LayoutXML to it

		drawPile = UpgradeCardsList( deck.cards );
		LayoutGame ();
	}

	// UpgradeCardsList casts the Cards in lCD to be CardBartoks 
	// Of course, they were all along, but this lets Unity know it 
	List<CardBartok> UpgradeCardsList(List<Card> lCD) {
		List<CardBartok> lCB = new List<CardBartok>();
		foreach( Card tCD in lCD ) {
			lCB.Add ( tCD as CardBartok );
		}
		return( lCB );}

	// Position all the cards in the drawPile properly 
	public void ArrangeDrawPile() {
		CardBartok tCB;

	for (int i=0; i<drawPile.Count; i++) {
			tCB = drawPile[i];
			tCB.transform.parent = layoutAnchor;
			tCB.transform.localPosition = layout.drawPile.pos;
			// Rotation should start at 0
			tCB.faceUp = false;
			tCB.SetSortingLayerName(layout.drawPile.layerName);
			tCB.SetSortOrder(-i*4); // Order them front-to-back 
			tCB.state = CBState.drawpile;
		}
	}

	void LayoutGame(){
		//Create an empty GameObject to serve as an anchor for the tableau
		if (layoutAnchor == null) {
			GameObject tGO = new GameObject ("_LayoutAnchor");
			//^Create an empty GameObject named _LayoutAnchor in the Hierarchy
			layoutAnchor = tGO.transform;
			layoutAnchor.transform.position = layoutCenter;
		}

		//Position the drawPile cards
		ArrangeDrawPile ();

		//Set up the players
		Player pl;
		players = new List<Player> ();
		foreach (SlotDef tSD in layout.slotDefs) {
			pl = new Player ();
			pl.handSlotDef = tSD;
			players.Add (pl);
			pl.playerNum = players.Count;
		}
		players[0].type = PlayerType.human;

		CardBartok tCB;
		// Deal 7 cards to each player
		for (int i=0; i<numStartingCards; i++) {
			for (int j=0; j<4; j++) { // There are always 4 players
				tCB = Draw (); // Draw a card
				// Stagger the draw time a bit. Remember order of operations. 
				tCB.timeStart = Time.time + drawTimeStagger * ( i*4 + j );
				// ^ By setting the timeStart before calling AddCard, we
				// override the automatic setting of timeStart in
				// CardBartok.MoveTo().

				// Add the card to the player's hand. The modulus (%4) 
				// results in a number from 0 to 3
				players[ (j+1)%4 ].AddCard(tCB);
			}
		}
		

		// Call Bartok.DrawFirstTarget() when the hand cards have been drawn.
		Invoke("DrawFirstTarget", drawTimeStagger * (numStartingCards*4+4) );

	}

	public void DrawFirstTarget() {
		// Flip up the first target card from the drawPile 
		CardBartok tCB = MoveToTarget( Draw () );
		// Set the CardBartok to call CBCallback on this Bartok when it is done 
		tCB.reportFinishTo = this.gameObject;
	}

	// This callback is used by the last card to be dealt at the beginning 
	// It is only used once per game.
	public void CBCallback(CardBartok cb) {
		// You sometimes want to have reporting of method calls like this 
		Utils.tr(Utils.RoundToPlaces(Time.time),"Bartok.CBCallback()",cb.name);

		StartGame(); // Start the Game
	}
		public void StartGame() {
			// Pick the player to the left of the human to go first. // (players[0] is the human)
			PassTurn(1);
		}

	// This makes a new card the target
	public CardBartok MoveToTarget(CardBartok tCB) {
		tCB.timeStart = 0; 
		tCB.MoveTo(layout.discardPile.pos+Vector3.back); 
		tCB.state = CBState.toTarget;
		tCB.faceUp = true;
		tCB.SetSortingLayerName("10");//layout.target.layerName); 
		tCB.eventualSortLayer = layout.target.layerName;
		if (targetCard != null) {
			MoveToDiscard(targetCard);
		}
		targetCard = tCB;

		return(tCB);
	}

	public CardBartok MoveToDiscard(CardBartok tCB) {
		tCB.state = CBState.discard;
		discardPile.Add ( tCB );
		tCB.SetSortingLayerName(layout.discardPile.layerName);
		tCB.SetSortOrder( discardPile.Count*4 );
		tCB.transform.localPosition = layout.discardPile.pos + Vector3.back/2;

		return(tCB);
	}

	//The draw function will pull a single card from the drawPile and return it
	public CardBartok Draw(){
		CardBartok cd = drawPile [0];
		drawPile.RemoveAt (0);
		return(cd);
	}

	//This update method is used to test adding cards to players' hands
	void Update(){
		if (Input.GetKeyDown (KeyCode.Alpha1)) {
			players [0].AddCard (Draw ());
		}
		if (Input.GetKeyDown (KeyCode.Alpha2)) {
			players [1].AddCard (Draw ()); 
		}
		if (Input.GetKeyDown (KeyCode.Alpha3)) {
			players [2].AddCard (Draw ());
		}
		if (Input.GetKeyDown (KeyCode.Alpha4)) {
			players [3].AddCard (Draw ());
		}
	}
}
