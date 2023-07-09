/*
*   Project:    Riff It!
*   Class:      GroupNote.cs
*   Brief:      Keeps all the notes from a group
*   Author:     Matias Lizana García
*/

using System.Collections.Generic;
using UnityEngine;

public class GroupNote : Note {

    public List<Note> notes;
	public RiffConfig rc;
	public bool allowAccent, allowRest;
    public int index;

	public GroupNote(RiffConfig rc, int index, int num, bool onBeat = false, bool firstOnGroup = false, bool lastOnBeat = false, bool generate = true) : base(NOTE_TYPE.TYPE_GROUP, num, false, onBeat, firstOnGroup, lastOnBeat) {
		this.rc = rc;
        this.notes = new List<Note>();
		this.index = index;
		this.allowRest = rc.rest;
		this.allowAccent = rc.accent;
		if (generate) {
			if (rc.combiGroups)
				createGroup ();
			else
				createEntireGroup ();
		}
    }
		
	//All the combinations inside the group also
	private void createGroup() {
		bool firstOnGroup = true;
		int noteNum = 0, noteDuration = 0;
		int groupDuration = Note.calculateDuration(num-1)*index;	//An imaginary duration inside the group, based on base note and index
		int maxDuration = groupDuration;
		Note note;
			//When combigroups, 4ths must have 8ths, and 8ths must have 16ths

		while (groupDuration > 0) {
			bool restP, accentP, num1P, num2P, num3P;
			do {
				restP = (UnityEngine.Random.value > 0.5) && onBeat;
				accentP = (UnityEngine.Random.value > 0.5);
				num1P = (UnityEngine.Random.value > 0.5);
				num2P = (UnityEngine.Random.value > 0.5);
				num3P = (UnityEngine.Random.value > 0.5);
				//From semiquavers to quarter notes
				if(num==3)	//Quarters only allow quarters and quavers (to create semiquavers, must be a minor group)
					noteNum = (rc.num2&&num2P)?2:((rc.num3&&num3P)?3:3);
				else if(num==2) //Quavers allow also semiquavers inside
					noteNum = ((rc.num1&&num1P)?1:((rc.num2&&num2P)?2:((rc.num3&&num3P)?3:3)));
				noteDuration = Note.calculateDuration(noteNum);
			} while (noteDuration > groupDuration);

			//Only note values of the group - 1 (quavers -> semiquavers), no dots allowed
			if (restP && maxDuration == groupDuration)	//Only rests at the begining of the group
				note = new Note (Note.NOTE_TYPE.TYPE_REST, noteNum, false, false, false);
			else {
				//Note with accent
				note = new Note (Note.NOTE_TYPE.TYPE_NOTE, noteNum, (allowAccent && accentP), onBeat, firstOnGroup);
				firstOnGroup = false;
			}
			
			groupDuration -= note.getDuration();	
			notes.Add(note);
		}
	}

	//A basic group with all the notes, no combinations inside
    private void createEntireGroup() {
		bool firstOnGroup = true;
		Note note;
		for (int i = 0; i < index; i++) {
			bool accentP = (UnityEngine.Random.value > 0.5);
			//Only note values of the group - 1 (quavers -> semiquavers)
			note = new Note (Note.NOTE_TYPE.TYPE_NOTE, num-1, (allowAccent && accentP), onBeat, firstOnGroup);
			firstOnGroup = false;
			notes.Add(note);
		}
    }

	public Note getNote(int i) {
		return notes[i];
	}

	public void addNote(Note n) {
		notes.Add (n);
	}

	public bool containsRestOrQuaver() {
		bool contains = false;
		foreach (Note n in notes) {
			if (n.isRest() || n.getNum () == 3)
				contains = true;
		}
		return contains;
	}

    public string getString() {
		string s = "";
		if (onBeat) s+= "|";
		s = "g" + num + "("+index+"){";
        foreach (Note n in notes)
        {
            switch (n.getType())
            {
				case Note.NOTE_TYPE.TYPE_NOTE:
					s += (n.hasAccent()?"a":"n");
					break;
                case Note.NOTE_TYPE.TYPE_REST:
                    s += "r";
                    break;
                case Note.NOTE_TYPE.TYPE_GROUP:
                    s += ((GroupNote)n).getString();
                    break;
            }
            s += n.getNum();
            for (int i = 0; i < n.getDots(); i++)
                s += ".";
        }
		s += "}";
        return s;
    }

}
