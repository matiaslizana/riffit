/*
*   Project:    Riff It!
*   Class:      RiffConfig.cs
*   Brief:      Configuration class to store info for the riff
*   Author:     Matias Lizana García
*/

using UnityEngine;
using System.Collections;

public class RiffConfig  {

	public bool num0;			//32th (thirty-second)
	public bool num1;			//16th (sixteenth)
	public bool num2;			//8th  (eight)
	public bool num3;			//4th  (quarter)
	public bool rest;			//Rests
	public bool dot;			//Dots
	public bool accent;			//Accents
	public bool group3;			//Groups of 3
	public bool group5;			//Groups of 5
	public bool group7;			//Groups of 7
	public bool combiGroups;	//Allow combinations inside groups

	public RiffConfig() {
		this.num0 = false;
		this.num1 = false;
		this.num2 = false;
		this.num3 = true;
		this.rest = false;
		this.dot = false;	
		this.accent = false;
		this.group3 = false;
		this.group5 = false;
		this.group7 = false;
		this.combiGroups = false;
	}
	
}
