/*
*   Project:    Riff It!
*   Class:      Riff.cs
*   Brief:      Keeps all the notes on a list
*   Author:     Matias Lizana García
*/

using System.Collections.Generic;
using UnityEngine;

public class Riff {

    public List<Note> notes;

    public Riff() {
        notes = new List<Note>();
    }

	public Riff(RiffConfig rc, string s) {
		notes = new List<Note>();
		string[] sR = s.Split (('|'));
		for (int t = 1; t < sR.Length; t++) {
			string beatRiff = sR [t];
			int i = 0;
			bool firstOnGroup = true;
			int beatDuration = 0;
			while (i < beatRiff.Length) {
				char type = beatRiff [i];
				int num = (int) (beatRiff [i+1] - '0');
				bool onBeat = i == 0;
				i += 2;
				Note n;
				if (type == 'g') {
					int index = (int)(beatRiff [i+1] - '0');
					i += 3;
					n = new GroupNote(rc,index,num,onBeat,firstOnGroup,false,false);
					int pFrom = beatRiff.IndexOf ("{",i);
					int pTo = beatRiff.IndexOf("}",i);
					string groupRiff = beatRiff.Substring (pFrom + 1, pTo - pFrom - 1);
					int gi = 0;
					i++;
					while (gi < groupRiff.Length) {
						type = groupRiff [gi];
						num = (int) (groupRiff [gi+1] - '0');
						onBeat = gi == 0;
						gi += 2;
						i += 2;
						Note gn;
						if (type == 'r') {
							gn = new Note (Note.NOTE_TYPE.TYPE_REST, num, false, onBeat);
						} else {
							bool accent = (type == 'a');
							gn = new Note (Note.NOTE_TYPE.TYPE_NOTE, num, accent, onBeat, firstOnGroup);
							firstOnGroup = false;
						}
						if (gi < groupRiff.Length) {
							if (groupRiff [gi] == '.') {
								gn.addDot ();
								gi++;
								i++;
							}
						}
						if (gi == groupRiff.Length)
							gn.lastOnBeat = true;
						((GroupNote)n).addNote (gn);
					}
					i++;
				} else {
					if (type == 'r') {
						n = new Note (Note.NOTE_TYPE.TYPE_REST, num, false, onBeat);
					} else {
						bool accent = (type == 'a');
						n = new Note (Note.NOTE_TYPE.TYPE_NOTE, num, accent, onBeat, firstOnGroup);
						firstOnGroup = false;
					}
					if (i < beatRiff.Length) {
						if (beatRiff [i] == '.') {
							n.addDot ();
							i++;
						}
					}
				}
				if (beatDuration < 4 && beatDuration + n.getDuration() >= 4)
					n.lastBeforeMiddle = true;
				if (beatDuration == 4)
					n.middleBeat = true;
				if (i == beatRiff.Length)
					n.lastOnBeat = true;
				beatDuration+= n.getDuration();
				Add (n);
			}
		}
	}

    public void Add(Note n) {
        notes.Add(n);
    }

    public Note getNote(int i) {
        return notes[i];
    }

    public List<Note> getNotes() {
        return notes;
    }

    public int Length() {
		return notes.Count;
    }

    public string getString() {
        string s = "";
        foreach(Note n in notes)
        {
            if (n.isOnBeat()) s += "|";
            switch (n.getType())
            {
                case Note.NOTE_TYPE.TYPE_NOTE:
					s += (n.hasAccent()?"a":"n") + n.getNum();
                    break;
                case Note.NOTE_TYPE.TYPE_REST:
					s += "r" + n.getNum();
                    break;
                case Note.NOTE_TYPE.TYPE_GROUP:
                    s += ((GroupNote)n).getString();
                    break;
            }
            for (int i = 0; i < n.getDots(); i++)
                s += ".";
        }
        return s;
    }

}
