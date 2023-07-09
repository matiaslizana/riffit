/*
*   Project:    Riff It!
*   Class:      GameController.cs
*   Brief:      Controls everything that happens on the application
*   Author:     Matias Lizana García
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
class RiffItConfig
{
    public bool firstTime = true;
}

public class GameController : MonoBehaviour
{
    #region Parameters

    private float startDraw; //where to start to draw on the canvas "X"
    private float drawY; //a line reference for "Y" coordinate
    private float drawSpace; //space between each 32th notes (maximum resolution)
    private RiffConfig rc; //activates noteNums, rests, dots, accents, and groups
    private string riffPath; //Path to save the riffs
    private Transform canvasTransform;
    private Transform notesTransform;
    private RiffItConfig config;

    [Header("Rythm Selection")] public GameObject key8;
    public GameObject key16;
    public GameObject key32;
    public GameObject keyG3;
    public GameObject keyG5;
    public GameObject keyG7;
    public GameObject keyCombi;
    public GameObject keyRest;
    public GameObject keyAccent;
    public GameObject keyDot;
    public GameObject dice;
    public GameObject rythmSelector;

    private Riff currentRiff;
    private Dictionary<string,GameObject> currentRiffObjects;
    private Dictionary<string,Image> currentRiffImages;

    [Header("Player")] public bool playerActive; //toggle between active/inactive
    public GameObject playText, stopText, playButton, stopButton; //text toggle and play/stop button
    public Animation stopButtonAnim;
    public GameObject player;
    public GameObject headphones;
    public Toggle toggleButton;
    public AudioClip beatAudio, measureAudio; //audio to play metronome
    public AudioClip[] noteAudio, noteAccentAudio; //audios to play the note
    public AudioSource clickAudioSource; //audiosource for playing click
    public AudioSource noteAudioSource; //audiosource for playing notes
    private Coroutine playerCoroutine;

    [Header("Metronome")] public GameObject metronome; //metronome game object
    public GameObject metronomeInput; //metronome input
    public GameObject oStep; //step for measure
    public GameObject oBase; //base for measure
    public GameObject textMStep;
    public GameObject textMBase;
    public float BPM; //beats per minute
    public int mStep; //measure step
    public int mBase; //measure base
    public int mCurrentStep; //current beat
    public int mCurrentMeasure; //current measure
    private float mInterval; //the beat time
    private float minInterval; //minimum note (32th) time
    private Image lastImage;
    private float currentTime;

    [Header("Menu")] public GameObject menu;
    public GameObject menuButton;
    public GameObject background;
    public GameObject drawArea;
    private GameObject rythmHelper;
    private GameObject diceHelper;
    private GameObject measureHelper;
    private GameObject playerHelper;
    private GameObject metronomeHelper;
    private GameObject menuHelper;

    [Header("Materials")] public Material noteHeadMat;
    public Material defaultMat;
    public Material lineMat;

    #endregion

    #region Initialization

    /*
    *   Game Controller Initialization
    */
    void Start()
    {
        config = new RiffItConfig();
        loadConfig();
        if (config.firstTime)
            enableHelp();

        rc = new RiffConfig();
        startDraw = -240f;
        drawY = -20f;
        drawSpace = 18f;
        drawBeat();
        clickAudioSource = GetComponent<AudioSource>();
        riffPath = Application.persistentDataPath + "/riffsave.txt";
        canvasTransform = GameObject.Find("Canvas").transform;
        notesTransform = GameObject.Find("Notes").transform;
        BPM = int.Parse(metronomeInput.GetComponent<InputField>().text);
        currentRiff = new Riff(rc, "|n3|n3|n3|n3");
        drawRiff(currentRiff);
    }

    #endregion

    #region Riff Functions

    /*
    *   Complete action to create the riff notes and draw to screen
    */
    public void createRiff()
    {
        if (playerActive)
            toggleButton.isOn = false;
        eraseRiff();
        currentRiff = calculateRiff();
        //Debug.Log ("Current Riff: " + currentRiff.getString ());
        drawRiff(currentRiff);
    }

    /*
    *   Calculates the notes of the riff
    */
    public Riff calculateRiff()
    {
        int n = mStep;
        Riff r = new Riff();
        int beat = 32 / mBase; //2 => 16, 4 => 8, 8 => 4
        int totalBeat = beat * mStep;

        while (n-- > 0)
        {
            //Grouping beats
            if (totalBeat - 8 >= 0)
                beat = 8;
            else if (totalBeat - 4 >= 0)
                beat = 4;
            else if (totalBeat - 2 >= 0)
                beat = 2;
            totalBeat -= beat;

            bool onBeat = true; //First note is on the beat
            bool auxRest = rc.rest; //To avoid consecutive rests
            bool firstOnGroup = true; //First note on a beat that is not a rest

            while (beat > 0)
            {
                int num, //Note selected randomly
                    duration; //Duration of the note
                bool restP, dotP, accentP, group3P, group5P, group7P, num0P, num1P, num2P, num3P;
                do
                {
                    restP = UnityEngine.Random.value > 0.5 && onBeat;
                    dotP = UnityEngine.Random.value > 0.5;
                    accentP = UnityEngine.Random.value > 0.5;
                    group3P = UnityEngine.Random.value > 0.5;
                    group5P = UnityEngine.Random.value > 0.5;
                    group7P = UnityEngine.Random.value > 0.5;
                    num0P = UnityEngine.Random.value > 0.5;
                    num1P = UnityEngine.Random.value > 0.5;
                    num2P = UnityEngine.Random.value > 0.5;
                    num3P = UnityEngine.Random.value > 0.5;
                    //(0: 32th, 1: 16th, 2: 8th, 3: 4th)
                    num = ((num0P && rc.num0)
                        ? 0
                        : ((num1P && rc.num1) ? 1 : ((num2P && rc.num2) ? 2 : ((num3P && rc.num3) ? 3 : 3))));
                    duration = Note.calculateDuration(num);
                } while (duration > beat);

                //Fill with the current note (Group, Rest, Note)
                Note note;
                if (num > 1 && (rc.group3 && group3P || rc.group5 && group5P || rc.group7 && group7P) &&
                    (num == 3 && rc.num2 || num == 2 && rc.num1))
                {
                    //Groups only on quavers and quarter notes
                    note = new GroupNote(rc,
                        (rc.group3 && group3P) ? 3 : ((rc.group5 && group5P) ? 5 : ((rc.group7 && group7P) ? 7 : 3)),
                        num, onBeat, firstOnGroup);
                    firstOnGroup = false;
                }
                else
                {
                    if (auxRest && restP)
                        note = new Note(Note.NOTE_TYPE.TYPE_REST, num, false, onBeat);
                    else
                    {
                        //Note and accent
                        note = new Note(Note.NOTE_TYPE.TYPE_NOTE, num, (rc.accent && accentP), onBeat, firstOnGroup);
                        firstOnGroup = false;
                    }

                    //Avoid two consecutive silences on the same beat (if option rest is active)
                    if (rc.rest)
                        auxRest = !note.getType().Equals(Note.NOTE_TYPE.TYPE_REST);

                    //Check for the dot, if we can fill the beat and with it's minor value
                    if (rc.dot)
                    {
                        int dotNum = num;
                        while (dotP && dotNum > 0 && dotAllowed(dotNum) && duration > 1 && duration + note.checkDotsDuration(note.getDots() + 1) <= beat)
                        {
                            note.addDot();
                            dotNum--;
                            dotP = UnityEngine.Random.value > 0.5;
                        }
                    }
                }

                onBeat = false; //Not on the beat until next step

                //For quavers, separate in two groups
                if (beat > 4 && beat - note.getDuration() <= 4)
                    note.lastBeforeMiddle = true;
                if (beat == 4)
                    note.middleBeat = true;

                //Substract duration to the beat
                beat -= note.getDuration();

                //Save if it's the last one of the beat (to draw)
                if (beat <= 0)
                    note.lastOnBeat = true;

                //Add the note to the riff
                r.Add(note);
            }
        }

        //r.Optimize();
        return r;
    }

    //Calculates if we can draw a dot, we supose num is allowed because note it's created
    public bool dotAllowed(int num)
    {
        if (num == 1)
            return rc.num0;
        if (num == 2)
            return rc.num1;
        if (num == 3)
            return rc.num2;
        return false;
    }

    #endregion

    #region Draw Functions

    /*
    *   Draws the beat to the screen
    */
    public void drawBeat()
    {
        int beat = 32 / mBase;
        int duration = beat * mStep;
        GameObject bdraw;
        Transform ruleTransform = GameObject.Find("BeatRule").transform;

        for (int i = 0; i < duration; i += 2)
        {
            bdraw = Instantiate(Resources.Load("Drawing/Beat16"), ruleTransform) as GameObject;
            bdraw.transform.localPosition = new Vector3(startDraw + drawSpace * i, drawY - 5f);
            bdraw.transform.localScale = new Vector3(0.2f, 0.2f, 1);
            
            if (i % 4 == 0)
            {
                bdraw = Instantiate(Resources.Load("Drawing/Beat8"), ruleTransform) as GameObject;
                bdraw.transform.localPosition = new Vector3(startDraw + drawSpace * i, drawY - 10f);
                bdraw.transform.localScale = new Vector3(0.2f, 0.2f, 1);
            }
            
            if (i % 8 == 0)
            {
                bdraw = Instantiate(Resources.Load("Drawing/Beat4"), ruleTransform) as GameObject;
                bdraw.transform.localPosition = new Vector3(startDraw + drawSpace * i, drawY - 15f);
                bdraw.transform.localScale = new Vector3(0.2f, 0.2f, 1);
            }
        }
    }

    /*
    *   Draws the riff to the screen
    */
    public void drawRiff(Riff riff)
    {
        float x = startDraw;
        int n = 0;

        while (n < riff.Length())
        {
            Note note = riff.getNote(n);
            string noteNumber = n.ToString();
            //Checks the type
            if (note.isRest())
                drawRest(note, x, drawY, noteNumber);
            else if (note.isGroup())
                drawGroup((GroupNote) note, x, drawY, noteNumber);
            else
            {
                drawNote(note, x, drawY, noteNumber);
                drawStem(note, x, drawY, noteNumber);
            }

            float xNoteDist = drawSpace * note.getDuration();

            if (!note.isLastOnBeat())
            {
                //If is not the last note on the Beat (we have not draw it yet)
                Note noteNext = riff.getNote(n + 1);

                //FLAGS
                if (note.isRest() && noteNext.lastOnBeat && !noteNext.isGroup())
                {
                    //Is a rest and the next is last on beat, we have to draw a flag
                    drawFlag(noteNext, x + xNoteDist, drawY, noteNumber);
                }

                int num = 1;

                //HSTEM on a beat
                if (note.isNote() && note.getNum() < 3)
                {
                    //If next note is a group, check that it doesn't have a quaver at the two first notes
                    if (!noteNext.isGroup() || (((GroupNote) noteNext).getNote(0).getNum() != 3 &&
                                                ((GroupNote) noteNext).getNote(1).getNum() != 3))
                    {
                        drawHStem(note, x, drawY, noteNumber, 0);
                    }
                    else
                    {
                        if (note.isOnBeat())
                            drawFlag(note, x, drawY, noteNumber);
                    }

                    //It's a note and is at less than a quaver
                    while (num >= note.getNum())
                    {
                        if (num >= noteNext.getNum())
                        {
                            if (!noteNext.isMiddleBeat())
                            {
                                drawHStem(note, x, drawY, noteNumber, 2 - num);
                                if (n + 2 < riff.Length() && riff.getNote(n + 2).getNum() > noteNext.getNum() && noteNext.getNum()!=1)
                                {
                                    float minusDist = (drawSpace / (2 - noteNext.getNum())) +
                                                      (noteNext.getDots() * (float) 0.5 * noteNext.getNum() *
                                                       drawSpace);
                                    drawHStem(noteNext, x + xNoteDist - minusDist, drawY, noteNumber, 2 - noteNext.getNum(),
                                        true);
                                }
                            }
                            else
                            {
                                if (Math.Abs(note.getNum() - noteNext.getNum()) == 1 ||
                                    (note.getNum() == 1 && noteNext.getNum() == 1))
                                    drawHStem(note, x, drawY, noteNumber, 2 - num);
                                else
                                {
                                    if (note.isFirstOnGroup())
                                        drawHStem(note, x, drawY, noteNumber, 2 - num, true);
                                }
                            }
                        }
                        else
                        {
                            if (note.isFirstOnGroup() || note.isMiddleBeat())
                                if (!noteNext.isGroup() ||
                                    (((GroupNote) noteNext).getNote(0).getNum() != 3 &&
                                     ((GroupNote) noteNext).getNote(1).getNum() != 3))
                                {
                                    drawHStem(note, x, drawY, noteNumber, 2 - num, true);
                                }
                        }

                        num--;
                    }

                    if (noteNext.isLastOnBeat() || noteNext.isLastBeforeMiddle())
                    {
                        num = 1;
                        while (num >= noteNext.getNum())
                        {
                            if (num < note.getNum())
                            {
                                float minusDist = (drawSpace / (2 - noteNext.getNum())) +
                                                  (noteNext.getDots() * (float) 0.5 * noteNext.getNum() * drawSpace);
                                drawHStem(noteNext, x + xNoteDist - minusDist, drawY, noteNumber, 2 - num, true);
                            }

                            num--;
                        }
                    }
                }

                if (note.isGroup())
                {
                    GroupNote gn = (GroupNote) note;
                    if (!gn.containsRestOrQuaver())
                    {
                        drawHStem(note, x, drawY, noteNumber, 0);
                        if (noteNext.isLastOnBeat())
                        {
                            num = 1;
                            while (num >= noteNext.getNum())
                            {
                                if (num < note.getNum())
                                    drawHStem(noteNext, x + xNoteDist - drawSpace / (2 - noteNext.getNum()), drawY, noteNumber,
                                        2 - num, true);
                                num--;
                            }
                        }
                    }
                    else
                    {
                        if (noteNext.isLastOnBeat() && !noteNext.isGroup())
                        {
                            drawFlag(noteNext, x + xNoteDist, drawY, noteNumber);
                        }
                    }
                }
            }

            //Last note we draw a flag for the irregular measures
            if (n == riff.Length() - 1 && note.isNote() && (mBase == 8 || mBase == 16) && mStep%2!=0 && note.isOnBeat())
            {
                drawFlag(note, x, drawY, noteNumber);
            }
            
            x += xNoteDist;
            n++;
        }

    }

    /*
    * 	Draw objects
    */
    public void drawNote(Note n, float x, float y, string num)
    {
        GameObject noteDraw = Instantiate(Resources.Load("Drawing/NoteHead"), notesTransform) as GameObject;
        noteDraw.transform.localPosition = new Vector3(x, y + 18f, 0);
        noteDraw.transform.localScale = new Vector3(0.2f, 0.2f, 1);
        noteDraw.name = "Note" + num;

#if RIFF_DEBUG
            if (n.isFirstOnGroup())
                drawDebugHStem(x, y + 10, num, new Color(0, 0, 1));
            if (n.isLastOnBeat())
                drawDebugHStem(x, y + 10, num, new Color(1, 0, 0));
            if (n.isMiddleBeat())
                drawDebugHStem(x, y + 10, num, new Color(0, 1, 0));
            if (n.isLastBeforeMiddle())
                drawDebugHStem(x, y + 10, num, new Color(1, 1, 0));
#endif

        for (var nd = 0; nd < n.getDots(); nd++)
        {
            GameObject dotDraw = Instantiate(Resources.Load("Drawing/Dot"), notesTransform) as GameObject;
            dotDraw.transform.localPosition = new Vector3(x + 15f + 8f * nd, y + 18f, 0);
            dotDraw.transform.localScale = new Vector3(0.2f, 0.2f, 1);
            dotDraw.name = "Dot" + num;
        }

        if (n.hasAccent())
        {
            GameObject accentDraw = Instantiate(Resources.Load("Drawing/Accent"), notesTransform) as GameObject;
            accentDraw.transform.localPosition = new Vector3(x + 1f, y + 5f, 0);
            accentDraw.transform.localScale = new Vector3(0.1f, 0.1f, 1);
            accentDraw.name = "Accent" + num;
        }
    }

    public void drawRest(Note n, float x, float y, string num)
    {
        GameObject restDraw = Instantiate(Resources.Load("Drawing/Rest" + n.getNum()), notesTransform) as GameObject;
        restDraw.transform.localPosition = new Vector3(x, y + 30f, 0);
        restDraw.transform.localScale = new Vector3(0.3f, 0.3f, 1);
        restDraw.name = "Rest" + num;

        for (var nd = 0; nd < n.getDots(); nd++)
        {
            GameObject dotDraw = Instantiate(Resources.Load("Drawing/Dot"), notesTransform) as GameObject;
            dotDraw.transform.localPosition = new Vector3(x + 10f + 8f * nd, y + 20f, 0);
            dotDraw.transform.localScale = new Vector3(0.2f, 0.2f, 1);
            dotDraw.name = "Dot" + num;
        }
    }

    public void drawStem(Note n, float x, float y, string num)
    {
        GameObject stemDraw = Instantiate(Resources.Load("Drawing/Stem"), notesTransform) as GameObject;
        stemDraw.transform.localPosition = new Vector3(x + 5f, y + 40f, 0);
        stemDraw.transform.localScale = new Vector3(0.35f, 0.35f, 1);
        stemDraw.name = "Stem" + num;
    }

    public void drawFlag(Note n, float x, float y, string num)
    {
        GameObject flagDraw = Instantiate(Resources.Load("Drawing/Flag" + n.getNum()), notesTransform) as GameObject;
        flagDraw.transform.localPosition = new Vector3(x + 10f, y + 50f, 0);
        flagDraw.transform.localScale = new Vector3(0.3f, 0.3f, 1);
        flagDraw.name = "Flag" + num;
    }

    public void drawText(int index, float x, float y, string num)
    {
        GameObject tG = Instantiate(
            (index == 3
                ? Resources.Load("Drawing/Group3Text")
                : (index == 5
                    ? Resources.Load("Drawing/Group5Text")
                    : (index == 7 ? Resources.Load("Drawing/Group7Text") : Resources.Load("Drawing/Group3Text")))),
            canvasTransform) as GameObject;
        tG.transform.localPosition = new Vector3(x, y + 80f, 0);
        tG.transform.localScale = new Vector3(0.25f, 0.25f, 1);
        tG.name = "GroupText" + num;
    }

    public void drawGroupDecoLeft(GroupNote note, float xO, float yO, string num)
    {
        GameObject gD = Instantiate(Resources.Load("Drawing/GroupDecoLeft"), notesTransform) as GameObject;
        float itemDist = drawSpace * note.getDuration() / note.index;
        float width = itemDist * (note.index - 1) / 2 - 10f;
        gD.transform.localPosition = new Vector3(xO + width / 2, yO + 75f, 0);
        gD.transform.localScale = new Vector3(1f, 0.15f, 1);
        gD.name = "GroupDecoL" + num;
        RectTransform rt = gD.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 100);
    }

    public void drawGroupDecoRight(GroupNote note, float xO, float yO, string num)
    {
        GameObject gD = Instantiate(Resources.Load("Drawing/GroupDecoRight"), notesTransform) as GameObject;
        float itemDist = drawSpace * note.getDuration() / note.index;
        float width = itemDist * (note.index - 1) / 2 - 5f;
        gD.transform.localPosition = new Vector3(xO - width / 2 - itemDist + 5f, yO + 75f, 0);
        gD.transform.localScale = new Vector3(1f, 0.15f, 1);
        gD.name = "GroupDecoR" + num;
        RectTransform rt = gD.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 100);
    }

    public void drawDebugHStem(float x, float y, string num, Color c)
    {
        GameObject hS = Instantiate(Resources.Load("Drawing/HStem"), notesTransform) as GameObject;
        hS.transform.localScale = new Vector3(1f, 0.03f, 1);
        hS.name = "Debug-Stem" + num;
        hS.GetComponent<Image>().color = c;
        RectTransform rt = hS.GetComponent<RectTransform>();
        hS.transform.localPosition = new Vector3(x, y, 0);
        rt.sizeDelta = new Vector2(20, 100);
    }

    public void drawHStem(Note note, float xO, float yO, string num, int level, bool half = false)
    {
        GameObject hS = Instantiate(Resources.Load("Drawing/HStem"), notesTransform) as GameObject;
        float newX = xO + 2f + drawSpace * note.getDuration() / 2;
        float newY = yO + 59f - level * 8f;
        hS.transform.localScale = new Vector3(1f, 0.03f, 1);
        hS.name = "H-Stem" + num;
        RectTransform rt = hS.GetComponent<RectTransform>();
        float width = drawSpace * note.getDuration();
        if (half)
        {
            width /= 2;
            newX -= width / 2;
        }

        hS.transform.localPosition = new Vector3(newX, newY, 0);
        rt.sizeDelta = new Vector2(width, 100);
    }

    public void drawGroupHStem(GroupNote note, float xO, float yO, float xNoteDist, string num,
        int level, bool half = false)
    {
        GameObject hS = Instantiate(Resources.Load("Drawing/HStem"), notesTransform) as GameObject;
        float newX = xO + 2f + xNoteDist / 2;
        float newY = yO + 59f - level * 8f;
        hS.transform.localScale = new Vector3(1f, 0.03f, 1);
        hS.name = "H-Stem" + num;
        RectTransform rt = hS.GetComponent<RectTransform>();
        float width = xNoteDist;
        if (half)
        {
            width /= note.index;
            newX -= width;
        }

        hS.transform.localPosition = new Vector3(newX, newY, 0);
        rt.sizeDelta = new Vector2(width, 100);
    }

    public void drawGroupLastHStem(GroupNote note, float xO, float yO, float xNoteDist, string num,
        int level, bool half = false)
    {
        GameObject hS = Instantiate(Resources.Load("Drawing/HStem"), notesTransform) as GameObject;
        float newX = xO + 2f + xNoteDist - xNoteDist / note.index;
        if (note.index == 3)
            newX += 10f;
        float newY = yO + 59f - level * 8f;
        hS.transform.localScale = new Vector3(1f, 0.03f, 1);
        hS.name = "H-Stem" + num;
        RectTransform rt = hS.GetComponent<RectTransform>();
        float width = xNoteDist / 3;
        hS.transform.localPosition = new Vector3(newX, newY, 0);
        rt.sizeDelta = new Vector2(width, 100);
    }

    public void drawGroup(GroupNote note, float x, float y, string num)
    {
        //Calculates space between notes on this group
        float xItemGroupDist = drawSpace * note.getDuration() / note.index;
        int i = 0;
        drawText(note.index, x + (xItemGroupDist * (note.index - 1)) / 2, drawY + 0.35f, num);
        drawGroupDecoLeft(note, x, y, num);
        drawGroupDecoRight(note, x + drawSpace * note.getDuration(), y, num);

        while (i < note.notes.Count)
        {
            string itemNum = num + "_" + i;
            Note n = note.getNote(i);
            float xNoteDist = xItemGroupDist * n.getDuration() / (note.getDuration() / 2);
            if (n.isRest())
                drawRest(n, x, drawY, itemNum);
            else
            {
                drawNote(n, x, drawY, itemNum);
                drawStem(n, x, drawY, itemNum);

                //Draw HStem for groups only if is not the last and...
                if (i < note.notes.Count - 1)
                {
                    Note noteNext = note.getNote(i + 1);
                    if (n.getNum() < 3)
                    {
                        //No HStem if it's a quarter note
                        if (noteNext.getNum() == 3)
                        {
                            //Next is a quarter, put the flag
                            if (i > 0)
                            {
                                Note notePrev = note.getNote(i - 1);
                                if (notePrev.getNum() == 3 || notePrev.isRest())
                                {
                                    drawFlag(n, x, drawY, itemNum);
                                }
                            }
                            else
                            {
                                drawFlag(n, x, drawY, itemNum);
                            }
                        }
                        else
                        {
                            drawGroupHStem(note, x, drawY, xNoteDist, itemNum, 0);
                            //Draw stems for semiquavers
                            if (n.getNum() == 1)
                            {
                                if (n.getNum() == noteNext.getNum())
                                {
                                    drawGroupHStem(note, x, drawY, xNoteDist, itemNum, 1);
                                }
                                else
                                {
                                    if (n.isFirstOnGroup())
                                        drawGroupHStem(note, x, drawY, xNoteDist, itemNum, 1, true);
                                    if (i > 0)
                                    {
                                        Note notePrev = note.getNote(i - 1);
                                        if (notePrev.getNum() > 1)
                                            drawGroupHStem(note, x, drawY, xNoteDist, itemNum, 1, true);
                                    }
                                }
                            }
                            else
                            {
                                //Its a quaver
                                if (i == note.notes.Count - 2)
                                {
                                    //Last note on group
                                    if (noteNext.getNum() == 1)
                                        drawGroupLastHStem(note, x, drawY, xNoteDist, itemNum, 1, true);
                                }
                            }
                        }
                    }
                    else if (n.getNum() == 3 && i == note.notes.Count - 2 && noteNext.getNum() != 3)
                    {
                        //Prev-last note is a quarter, next must have flag if is not a quarter too
                        drawFlag(noteNext, x + xNoteDist, drawY, itemNum);
                    }
                }
                else if (i == note.notes.Count - 1)
                {
                    Note notePrev = note.getNote(i - 1);
                    if (notePrev.isRest() && n.getNum() != 3)
                        drawFlag(n, x, drawY, itemNum);
                }
            }

            x += xNoteDist;
            i++;
        }
    }

    /*
    *   Erases riff game objects
    */
    public void eraseRiff()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("RiffItem"))
            Destroy(go);
    }

    /*
    *   Clean painted riff game objects
    */
    public void cleanRiff()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("RiffItem"))
            go.GetComponent<Image>().material = defaultMat;
    }

    /*
    *   Erases beat game objects
    */
    public void eraseBeat()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("BeatItem"))
            Destroy(go);
    }

    #endregion

    #region Player

    public void togglePlayer()
    {
        playerActive = !playerActive;
        if (playerActive)
        {
            mCurrentStep = 1;
            mCurrentMeasure = 0;
            playText.SetActive(false);
            stopText.SetActive(true);
            StartPlayer();
        }
        else
        {
            playText.SetActive(true);
            stopText.SetActive(false);
        }
    }

    public void StartPlayer()
    {
        if (playerCoroutine != null)
        {
            StopCoroutine(playerCoroutine);
            playerCoroutine = null;
        }

        mCurrentStep = 1;
        var multiplier = mBase / 4f;
        var tmpInterval = 60f / BPM;
        mInterval = tmpInterval / multiplier;
        minInterval = mInterval / (8 / multiplier);

        if (playerCoroutine == null)
            playerCoroutine = StartCoroutine(Play());
    }

    private void GetRiffObjects()
    {
        currentRiffObjects = new Dictionary<string,GameObject>();
        currentRiffImages = new Dictionary<string, Image>();
        int n = 0;
        foreach (Note note in currentRiff.getNotes())
        {
            if (note.isGroup())
            {
                int index = 0;
                foreach (Note gnote in ((GroupNote) note).notes)
                {
                    string search = (gnote.isNote() ? "Note" : "Rest") + n + "_" + index;
                    GameObject o = GameObject.Find(search);
                    currentRiffObjects.Add(search, o);
                    currentRiffImages.Add(search, o.GetComponent<Image>());
                    index++;
                }
            }
            else
            {
                string search = (note.isNote() ? "Note" : "Rest") + n;
                GameObject o = GameObject.Find(search);
                currentRiffObjects.Add(search, o);
                currentRiffImages.Add(search, o.GetComponent<Image>());
            }
            n++;
        }
    }

    IEnumerator Play()
    {
        currentTime = 0;
        lastImage = null;
        GetRiffObjects();

        while (playerActive)
        {
            int n = 0;
            foreach (Note note in currentRiff.getNotes())
            {
                if (!playerActive) break; //Deactivates player

                if (!note.isGroup())
                    yield return PlayNote(note, n, (note.isNote()?"Note":"Rest") + n, minInterval * note.getDuration());
                else
                {
                    int index = 0;
                    foreach (Note gnote in ((GroupNote) note).notes)
                    {
                        yield return PlayNote(gnote, n, (gnote.isNote()?"Note":"Rest") + n + "_" + index, minInterval * gnote.getDuration() / ((GroupNote) note).index * 2);
                        index++;
                    }
                }

                n++;
            }
        }

        cleanRiff();
    }

    private IEnumerator PlayNote(Note note, int n, string index, float noteTime)
    {
        //Cleans the last item painted
        if (lastImage)
            lastImage.material = defaultMat;

        if (currentRiffObjects[index])
        {
            lastImage = currentRiffImages[index];
            lastImage.material = noteHeadMat;
        }

        //Plays note
        if (note.isNote())
            noteAudioSource.PlayOneShot(
                (AudioClip) (note.hasAccent() ? noteAccentAudio : noteAudio).GetValue(
                    Mathf.CeilToInt(UnityEngine.Random.Range(0, noteAccentAudio.Length))));

        if (Math.Abs(currentTime % mInterval) < 0.01f)
        {
            PlayTick(n == 0);
            currentTime = 0;
        }

        if (currentTime + noteTime > mInterval)
        {
            //Check previous wait to beat
            if (currentTime > 0)
            {
                yield return new WaitForSeconds(mInterval - currentTime);
                PlayTick();
                noteTime -= mInterval - currentTime;
            }

            //Beat inside a note
            while (noteTime >= mInterval)
            {
                yield return new WaitForSeconds(mInterval);
                PlayTick();
                noteTime -= mInterval;
            }

            //Rest of the note (less than a beat)
            currentTime = 0;
            if (noteTime > 0.01f)
            {
                yield return new WaitForSeconds(noteTime);
                currentTime += noteTime;
            }
        }
        else
        {
            yield return new WaitForSeconds(noteTime);
            currentTime += noteTime;
        }
    }

    private void PlayTick(bool first = false)
    {
        clickAudioSource.PlayOneShot(first ? measureAudio : beatAudio);
        stopButtonAnim.Play();
        mCurrentStep++;
        if (mCurrentStep > mStep)
        {
            mCurrentStep = 1;
            mCurrentMeasure++;
        }
    }

    #endregion

    #region Metronome

    public void changeTempo()
    {
        BPM = int.Parse(metronomeInput.GetComponent<InputField>().text);
        cleanRiff();
        StartPlayer();
    }

    public void changeStep(int newStep = 0)
    {
        if (newStep != 0)
            mStep = newStep;
        else
            mStep++;

        if (mStep <= mBase)
            textMStep.GetComponent<Text>().text = mStep.ToString();
        else
        {
            textMStep.GetComponent<Text>().text = "1";
            mStep = 1;
        }

        eraseBeat();
        drawBeat();
        createRiff();
    }

    public void changeBase(int newBase = 0)
    {
        //When loading from data
        if (newBase != 0)
        {
            mBase = newBase;
            if (mBase == 4)
                mBase = 16;
            else if (mBase == 8)
                mBase = 4;
            else if (mBase == 16)
                mBase = 8;
        }

        if (mBase == 4)
        {
            mBase = mStep = 8;
            if (!key8.GetComponent<Toggle>().isOn)
            {
                key8.GetComponent<Toggle>().isOn = true;
                toggleKey8();
            }
        }
        else if (mBase == 8)
        {
            mBase = mStep = 16;
            if (!key16.GetComponent<Toggle>().isOn)
            {
                key16.GetComponent<Toggle>().isOn = true;
                toggleKey16();
            }
        }
        else if (mBase == 16)
            mBase = mStep = 4;

        textMStep.GetComponent<Text>().text = mStep.ToString();
        textMBase.GetComponent<Text>().text = mBase.ToString();
        eraseBeat();
        drawBeat();
        createRiff();
    }

    #endregion

    #region Menu

    public void toggleMenu()
    {
        menu.SetActive(!menu.activeInHierarchy);
    }

    public void saveRiff()
    {
        //Debug.Log ("Saving file...");
        FileStream file = new FileStream(riffPath, FileMode.Create, FileAccess.Write);
        StreamWriter sw = new StreamWriter(file);
        sw.WriteLine("R0:" + mStep + "_" + mBase + ":" + currentRiff.getString());
        sw.Close();
        file.Close();
        menu.SetActive(false);
        StartCoroutine(enableMenuTooltip("SaveHelper", "Riff Saved!"));
    }

    public void loadRiff()
    {
        FileStream file = new FileStream(riffPath, FileMode.Open, FileAccess.Read);
        StreamReader sr = new StreamReader(file);
        string s = sr.ReadLine();
        string[] r = s.Split((':'));

        //Loading measure
        string measure = r[1];
        string[] m = measure.Split('_');
        changeBase(Int32.Parse(m[1]));
        changeStep(Int32.Parse(m[0]));

        //Loading riff
        string riff = r[2];
        eraseRiff();
        currentRiff = new Riff(rc, riff);
        drawRiff(currentRiff);
        sr.Close();
        file.Close();
        menu.SetActive(false);
        StartCoroutine(enableMenuTooltip("LoadHelper", "Riff Loaded!"));
    }

    IEnumerator testLoadSave()
    {
        int i = 0;
        while (i < 100)
        {
            currentRiff = calculateRiff();
            string old = currentRiff.getString();
            saveRiff();
            loadRiff();
            string newRiff = currentRiff.getString();
            if (newRiff.Equals(old))
                Debug.Log("-> Test " + i + " OK : (" + old + " --- " + newRiff + ")");
            else
                Debug.LogError("-> Test " + i + " ERROR : (" + old + " --- " + newRiff + ")");
            yield return new WaitForSeconds(3f);
            i++;
        }
    }

    public void exportRiff()
    {
        menu.SetActive(false);
        StartCoroutine(screenshot());
        StartCoroutine(enableMenuTooltip("ExportHelper", "Riff Exported!"));
    }

    IEnumerator screenshot()
    {
        yield return new WaitForEndOfFrame();
        string filename = "Riff" + DateTime.UtcNow.ToString("yy-MM-dd-hhmmss") + ".png";
        Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
        NativeGallery.SaveImageToGallery(texture, Application.productName, filename);
    }

    #endregion

    #region Rythm Config

    public void toggleKey8()
    {
        //Avoids having an eight measure without eights
        if (mBase == 8 && !key8.GetComponent<Toggle>().isOn)
        {
            key8.GetComponent<Toggle>().isOn = true;
            return;
        }

        rc.num2 = key8.GetComponent<Toggle>().isOn;
        if (!rc.num2)
        {
            if (rc.group3) rc.group3 = keyG3.GetComponent<Toggle>().isOn = false;
            if (rc.group5) rc.group5 = keyG5.GetComponent<Toggle>().isOn = false;
            if (rc.group7) rc.group7 = keyG7.GetComponent<Toggle>().isOn = false;
        }
    }

    public void toggleKey16()
    {
        //Avoids having a sixteen measure without sixteenths
        if (mBase == 16 && !key16.GetComponent<Toggle>().isOn)
        {
            key16.GetComponent<Toggle>().isOn = true;
            return;
        }

        rc.num1 = key16.GetComponent<Toggle>().isOn;
    }

    public void toggleKey32()
    {
        rc.num0 = key32.GetComponent<Toggle>().isOn;
    }

    public void toggleKeyG3()
    {
        rc.group3 = keyG3.GetComponent<Toggle>().isOn;
        if (rc.group3)
            rc.num2 = key8.GetComponent<Toggle>().isOn = true;
    }

    public void toggleKeyG5()
    {
        rc.group5 = keyG5.GetComponent<Toggle>().isOn;
        if (rc.group5)
            rc.num2 = key8.GetComponent<Toggle>().isOn = true;
    }

    public void toggleKeyG7()
    {
        rc.group7 = keyG7.GetComponent<Toggle>().isOn;
        if (rc.group7)
            rc.num2 = key8.GetComponent<Toggle>().isOn = true;
    }

    public void toggleKeyCombi()
    {
        rc.combiGroups = keyCombi.GetComponent<Toggle>().isOn;
    }

    public void toggleKeyRest()
    {
        rc.rest = keyRest.GetComponent<Toggle>().isOn;
    }

    public void toggleKeyDot()
    {
        rc.dot = keyDot.GetComponent<Toggle>().isOn;
    }

    public void toggleKeyAccent()
    {
        rc.accent = keyAccent.GetComponent<Toggle>().isOn;
    }

    #endregion

    #region Helper

    public IEnumerator enableMenuTooltip(string helperName, string helperText)
    {
        menuButton.GetComponentInChildren<Button>().interactable = false;
        GameObject rh =
            Instantiate(Resources.Load("UI/Helpers/BaseHelper"), menuButton.transform) as GameObject;
        rh.name = helperName;
        rh.GetComponentInChildren<Text>().text = helperText;
        yield return new WaitForSeconds(3);
        menuButton.GetComponentInChildren<Button>().interactable = true;
        Destroy(rh);
    }

    public void enableHelp()
    {
        StartCoroutine(enableMenuTooltip("HelpHelper", "Help"));

        rythmHelper = Instantiate(Resources.Load("UI/Helpers/RythmHelper"), GameObject.Find("RythmSelector").transform) as GameObject;
        rythmHelper.name = "RythmHelper";
        diceHelper = Instantiate(Resources.Load("UI/Helpers/DiceHelper"), GameObject.Find("Dice").transform) as GameObject;
        diceHelper.name = "DiceHelper";
        diceHelper.transform.SetSiblingIndex(0);
        measureHelper = Instantiate(Resources.Load("UI/Helpers/MeasureHelper"), GameObject.Find("MeasureStep").transform) as GameObject;
        measureHelper.name = "MeasureHelper";
        playerHelper = Instantiate(Resources.Load("UI/Helpers/PlayerHelper"), GameObject.Find("Player").transform) as GameObject;
        playerHelper.name = "PlayerHelper";
        playerHelper.transform.SetSiblingIndex(0);
        metronomeHelper = Instantiate(Resources.Load("UI/Helpers/MetronomeHelper"), GameObject.Find("Player").transform) as GameObject;
        metronomeHelper.name = "MetronomeHelper";
        menuHelper = Instantiate(Resources.Load("UI/Helpers/MenuHelper"), GameObject.Find("MenuButton").transform) as GameObject;
        menuHelper.name = "MenuHelper";

        background.GetComponent<Image>().color = new Color(0.5f, 1, 1);

        if (playerActive)
            toggleButton.isOn = false;

        oStep.SetActive(true);
        oBase.SetActive(true);
        rythmSelector.SetActive(false);
        dice.SetActive(false);
        player.SetActive(false);
        metronome.SetActive(false);
        menuButton.SetActive(false);
        menu.SetActive(false);

        measureHelper.SetActive(true);
        rythmHelper.SetActive(false);
        diceHelper.SetActive(false);
        playerHelper.SetActive(false);
        metronomeHelper.SetActive(false);
        menuHelper.SetActive(false);
    }

    public void enableRythmHelper()
    {
        if (!rythmSelector.activeInHierarchy)
        {
            rythmSelector.SetActive(true);
            measureHelper.SetActive(false);
            rythmHelper.SetActive(true);
        }
    }

    public void enableDiceHelper()
    {
        if (!dice.activeInHierarchy)
        {
            dice.SetActive(true);
            dice.transform.GetChild(2).GetComponent<Animation>().Play();
            rythmHelper.SetActive(false);
            diceHelper.SetActive(true);
        }
    }

    public void enablePlayerHelper()
    {
        if (!player.activeInHierarchy)
        {
            dice.transform.GetChild(2).GetComponent<Animation>().Stop();
            dice.transform.GetChild(2).GetComponent<Image>().color = Color.white;
            player.SetActive(true);
            diceHelper.SetActive(false);
            playerHelper.SetActive(true);
            oStep.GetComponentInChildren<Button>().interactable = false;
            oBase.GetComponentInChildren<Button>().interactable = false;
        }
    }

    public void enableMetronomeHelper()
    {
        if (!metronome.activeInHierarchy)
        {
            metronome.SetActive(true);
            playerHelper.SetActive(false);
            metronomeHelper.SetActive(true);
        }
    }

    public void enableMenuHelper()
    {
        if (!menuButton.activeInHierarchy)
        {
            menuButton.SetActive(true);
            metronomeHelper.SetActive(false);
            menuHelper.SetActive(true);
        }
    }

    public void disableHelp()
    {
        if (background.activeInHierarchy)
        {
            oStep.GetComponentInChildren<Button>().interactable = true;
            oBase.GetComponentInChildren<Button>().interactable = true;
            if (config.firstTime)
            {
                config.firstTime = false;
                saveConfig();
            }

            Destroy(rythmHelper);
            Destroy(diceHelper);
            Destroy(measureHelper);
            Destroy(playerHelper);
            Destroy(metronomeHelper);
            Destroy(menuHelper);
            background.GetComponent<Image>().color = new Color(1, 1, 1);
        }
    }

    #endregion

    #region Config

    public void saveConfig()
    {
        string configPath = Application.persistentDataPath + "/config.txt";
        Debug.Log("Saving config variables...");
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(configPath, FileMode.Open);
        bf.Serialize(file, config);
        file.Close();
    }

    public void loadConfig()
    {
        string configPath = Application.persistentDataPath + "/config.txt";
        if (File.Exists(configPath))
        {
            Debug.Log("Loading config variables...");
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(configPath, FileMode.Open);
            RiffItConfig c = (RiffItConfig) bf.Deserialize(file);
            config.firstTime = c.firstTime;
            file.Close();
            Debug.Log("Config loaded!");
        }
        else
        {
            Debug.Log("Creating first time config...");
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(configPath);
            bf.Serialize(file, config);
            file.Close();
        }
    }

    #endregion
}