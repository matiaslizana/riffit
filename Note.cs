/*
*   Project:    Riff It!
*   Class:      Note.cs
*   Brief:      A note entity, has type and duration
*   Author:     Matias Lizana García
*/

using UnityEngine;

public class Note {

    public enum NOTE_TYPE {TYPE_REST, TYPE_NOTE, TYPE_GROUP};
    public NOTE_TYPE type;
    public int num;             //0-> 32, 1-> 16, 2-> 8, 3-> 4
    public int duration;        //1-> 32, 2-> 16, 4-> 8, 8-> 4
	private int originalDuration;
    public int dots;
	public bool accent;
    public bool onBeat;
	public bool firstOnGroup;
    public bool lastOnBeat;
	public bool middleBeat;
	public bool lastBeforeMiddle;

	public Note(NOTE_TYPE type, int num, bool accent = false, bool onBeat = false, bool firstOnGroup = false, bool lastOnBeat = false, bool middleBeat = false, bool lastBeforeMiddle = false)
    {
        this.type = type;
        this.num = num;
		this.accent = accent;
        this.onBeat = onBeat;
		this.firstOnGroup = firstOnGroup;
        this.lastOnBeat = lastOnBeat;
	    this.middleBeat = middleBeat;
	    this.lastBeforeMiddle = lastBeforeMiddle;
        dots = 0;
        duration = (int)Mathf.Pow(2, num);
        originalDuration = duration;
    }

    public static int calculateDuration(int num)
    {
        return (int)Mathf.Pow(2, num);
    }

    public void addDot()
    {
        dots++;
        duration = duration + originalDuration / (2*dots);
    }

    public int checkDotsDuration(int calculateDots)
    {
	    int dotsDuration = 0;
	    for (int i = 0; i < calculateDots; i++)
		    dotsDuration+= originalDuration / (2*(i+1));
	    return dotsDuration;
    }
    
    public NOTE_TYPE getType()
    {
        return type;
    }

    public int getDuration()
    {
        return duration;
	}

    public int getDots()
    {
        return dots;
    }

    public int getNum()
    {
        return num;
    }

    public bool isOnBeat()
    {
        return onBeat;
    }
		
	public bool isFirstOnGroup()
	{
		return firstOnGroup;
	}

	public bool isLastOnBeat()
	{
		return lastOnBeat;
	}

	public bool isMiddleBeat()
	{
		return middleBeat;
	}

	public bool isLastBeforeMiddle()
	{
		return lastBeforeMiddle;
	}

	public bool hasAccent()
	{
		return accent;
	}	

	public bool isNote()
	{
		return getType ().Equals (NOTE_TYPE.TYPE_NOTE);
	}

	public bool isRest()
	{
		return getType ().Equals (NOTE_TYPE.TYPE_REST);
	}

	public bool isGroup()
	{
		return getType ().Equals (NOTE_TYPE.TYPE_GROUP);
	}

}
