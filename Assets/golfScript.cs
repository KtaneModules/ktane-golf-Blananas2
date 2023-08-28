using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class golfScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable SubmitButton;
    public KMSelectable[] Arrowbuttons;
    public KMSelectable[] KeypadButtons;
    public GameObject Arrow;
    public GameObject GolfBall;
    public TextMesh Hole;
    public TextMesh Stroke;
    public TextMesh InputText;

    int[] HoleStrokes = new int[9];
    int[] ParStrokes = new int[9];
    int Handicap = 0;
    int WindDirection = 0;
    int CurrentHole = 1;
    int Total = 0;
    int Input = 0;
    int Thing = 0;
    float GolfBallDistance = 0.0767f;
    private List<int> HandicapNums = new List<int> {3, -2, 8, -7, -6, 2, 4, 5, 6, 0, -3, -5, 7, -1, -4, 1};
    private List<float> ArrowRotation = new List<float> {0f, -22.5f, -45f, -67.5f, -90f, -112.5f, -135f, -157.5f, -180f, -202.5f, -225f, -247.5f, -270f, -292.5f, -315f, -337.5f};//For some reason it needed to start at 0f, it actually doesn't make sense
    private List<string> Terms = new List<string> {"Hole in One", "Eagle", "Birdie", "Par", "Bogey", "Double Bogey", "Triple Bogey"};
    private List<string> DirectionNames = new List<string> {"N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"};
    private List<int> TermNums = new List<int> {0, -2, -1, 0, 1, 2, 3};

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;

        foreach (KMSelectable Arrow in Arrowbuttons)
        {
            Arrow.OnInteract += delegate () { ArrowPress(Arrow); return false; };
        }
        foreach (KMSelectable NumberButton in KeypadButtons)
        {
            NumberButton.OnInteract += delegate () { NumberPress(NumberButton); return false; };
        }
        SubmitButton.OnInteract += delegate () { PressSubmit(); return false; };
    }


    void Start()
    {
      for(int i = 0; i < 9; i++)
      {
        if (UnityEngine.Random.Range(0, 25100) != 0) { //odds of getting a hole in one are 1/25100
            HoleStrokes[i] = UnityEngine.Random.Range(1, 7);
        } else {
            HoleStrokes[i] = 0;
        }
      }
      WindDirection = UnityEngine.Random.Range(0,16);
      Handicap = HandicapNums[WindDirection];
      Debug.LogFormat("[Golf #{0}] The wind direction is {1} which is a handicap of {2}.", moduleId, DirectionNames[WindDirection], Handicap);
      Arrow.transform.localEulerAngles = new Vector3(90f, 0f, ArrowRotation[WindDirection]);
      if (Bomb.GetSerialNumberNumbers().Last() % 2 == 0) {
          ParStrokes[0] = ((Bomb.GetOnIndicators().Count() + Bomb.GetOffIndicators().Count()) % 4) + 4;
          ParStrokes[1] = ((Bomb.GetSerialNumberLetters().Count()) % 4) + 4;
          ParStrokes[2] = ((Bomb.GetBatteryCount()) % 4) + 4;
          ParStrokes[3] = ((Bomb.GetSerialNumberNumbers().Count()) % 4) + 4;
          ParStrokes[4] = ((Bomb.GetPortCount()) % 4) + 4;
          ParStrokes[5] = ((Bomb.GetModuleNames().Count()) % 4) + 4;
          ParStrokes[6] = ((Bomb.GetBatteryHolderCount()) % 4) + 4;
          ParStrokes[7] = ((Bomb.GetPortPlates().Count()) % 4) + 4;
          ParStrokes[8] = 7;
      } else {
          ParStrokes[0] = ((Bomb.GetBatteryCount(Battery.D)) % 4) + 4;
          ParStrokes[1] = (((Bomb.GetBatteryCount(Battery.AA) + Bomb.GetBatteryCount(Battery.AAx3) + Bomb.GetBatteryCount(Battery.AAx4))) % 4) + 4; //yeah apparently that shit exists
          ParStrokes[2] = ((Bomb.GetSerialNumberNumbers().Count()) % 4) + 4;
          ParStrokes[3] = ((Bomb.GetPortPlates().Count()) % 4) + 4;
          ParStrokes[4] = ((Bomb.GetModuleNames().Count()) % 4) + 4;
          ParStrokes[5] = ((Bomb.GetSerialNumberLetters().Count()) % 4) + 4;
          ParStrokes[6] = ((Bomb.GetIndicators().Count()) % 4) + 4;
          ParStrokes[7] = ((Bomb.GetPortCount()) % 4) + 4;
          ParStrokes[8] = 7;
      }
      for(int i = 0; i < 9; i++)
      {
          if (HoleStrokes[i] != 0) {
              Thing = ParStrokes[i] + TermNums[HoleStrokes[i]];
          } else {
              Thing = 1;
          }
          Debug.LogFormat("[Golf #{0}] Hole {1}: Par was {2}, you got a(n) {3}. Score is {4}.", moduleId, i+1, ParStrokes[i], Terms[HoleStrokes[i]], Thing);
          Total += Thing;
      }
      Debug.LogFormat("[Golf #{0}] Sum of scores for all holes is {1}.", moduleId, Total);
      Total += Handicap;
      Debug.LogFormat("[Golf #{0}] Adding handicap results in {1}.", moduleId, Total);
      Stroke.text = Terms[HoleStrokes[0]];
    }

    void PressSubmit()
    {
      SubmitButton.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress,SubmitButton.transform);
      if (moduleSolved)
      {
        return;
      }
      if (Input == Total)
      {
        Debug.LogFormat("[Golf #{0}] You entered {1}, module solved!", moduleId, Total);
        GetComponent<KMBombModule>().HandlePass();
        moduleSolved = true;
        StartCoroutine(ping());
      }
      else
      {
        Debug.LogFormat("[Golf #{0}] You entered {1}, that is incorrect.", moduleId, Input);
        GetComponent<KMBombModule>().HandleStrike();
        Input &= 0;
        InputText.text = null;
      }
    }

    void ArrowPress(KMSelectable Arrow)
    {
      Arrow.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress,Arrow.transform);
          if (Arrow == Arrowbuttons[1])
            {
              if (CurrentHole != 1)
              {
                CurrentHole--;
                Stroke.text = Terms[HoleStrokes[CurrentHole - 1]];
                Hole.text = "Hole "+ CurrentHole;
              }
            }
            else
            {
              if (CurrentHole != 9)
              {
                CurrentHole++;
                Stroke.text = Terms[HoleStrokes[CurrentHole - 1]];
                Hole.text = "Hole "+ CurrentHole;
              }
            }
    }

    void NumberPress(KMSelectable NumberButton)
    {
      NumberButton.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress,NumberButton.transform);
        for(int i = 0; i < 10; i++)
        {
            if (NumberButton == KeypadButtons[i] && Input.ToString().Length < 7)
            {
             Input = Input * 10 + i;
             InputText.text = Input.ToString();
            }
        }
    }

    IEnumerator ping()
    {
      Audio.PlaySoundAtTransform("ping_soundeffect", transform);
        for(int i = 0; i < 200; i++){
        yield return new WaitForSeconds(0.02f);
        GolfBallDistance += 0.05f;
        GolfBall.transform.localPosition = new Vector3(0.0189f, -0.01462f, GolfBallDistance);
      }
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use '!{0} cycle' to cycle between all of the holes. Use '!{0} submit ##' to submit a number.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
      Match mt;

      Command = Command.Trim().ToLowerInvariant();
      if (Regex.IsMatch(Command, @"^cycle ?(?:holes?)?$"))
      {
        yield return null;

        // In case for whatever reason it's not at hole 1 already, put back to 1.
        while (CurrentHole != 1)
          yield return new KMSelectable[] { Arrowbuttons[1] };

        while (CurrentHole != 9)
        {
          yield return "trywaitcancel 1.5 the cycle was aborted due to a request to cancel.";
          yield return new KMSelectable[] { Arrowbuttons[0] };
        }
        yield return "trywaitcancel 2.5 the cycle was aborted due to a request to cancel.";
        while (CurrentHole != 1)
          yield return new KMSelectable[] { Arrowbuttons[1] };
      }
      else if ((mt = Regex.Match(Command, @"^submit ([0-9]+)$")).Success)
      {
        string answer = mt.Groups[1].ToString();
        if (answer.Length >= 8)
          yield return "sendtochaterror!h That number is far, far too big!";

        yield return null;
        yield return answer.Select(x => KeypadButtons[x - '0']).ToArray();
        yield return new KMSelectable[] { SubmitButton };
      }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
      if (moduleSolved)
        yield break;

      foreach (char c in Total.ToString())
      {
        KeypadButtons[c - '0'].OnInteract();
        yield return new WaitForSeconds(0.1f);
      }
      SubmitButton.OnInteract();
    }
 }
