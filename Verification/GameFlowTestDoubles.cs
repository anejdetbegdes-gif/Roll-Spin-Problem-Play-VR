// Test doubles with the same public API as the real Unity components, so the REAL GameManager can be run headless.
using System; using System.Collections; using UnityEngine;
namespace RSPP {
  public enum Sfx { DiceLand, WheelTick, WheelStop, Correct, Incorrect, Star }
  public static class Haptics { public static void PulseLeft(float a,float d){} public static void PulseRight(float a,float d){} public static void PulseBoth(float a,float d){} }
  public class AudioManager { public void Play(Sfx s, AudioSource src=null, float v=1f){} }
  public class PlayAreaFitter { public void FitToPlayer(){} }
  public class BoardPath { public int LastIndex=>25; public bool IsStarSpace(int i){ foreach(int s in GameConfig.StarSpaces) if(s==i) return true; return false; } }
  public class PlayerToken { public int Shown; public void PlaceOn(int i){Shown=i;} public IEnumerator MoveAlong(int a,int b){ Shown=b; yield break; } }
  public class Dice { public event Action RollStarted; public event Action<int> Rolled; public bool RollEnabled{get;set;} public bool Highlighted; public void SetHighlighted(bool b){Highlighted=b;} public void ResetToHome(){}
    public bool SimRoll(int n){ if(!RollEnabled) return false; RollStarted?.Invoke(); Rolled?.Invoke(n); return true; } }
  public class SpinWheel { public event Action<WheelSegment> Stopped; public bool IsSpinning{get;private set;} public void Spin(){IsSpinning=true;} public void ResetWheel(){IsSpinning=false;}
    public void SimStop(WheelSegment s){ IsSpinning=false; Stopped?.Invoke(s);} }
  public class WheelHandle { public event Action Pulled; public bool Highlighted; public void SetHighlighted(bool b){Highlighted=b;} public void Pull(){Pulled?.Invoke();} }
  public class QuestionPanel { public event Action<int> AnswerSelected; public event Action NextPressed; public int Stars, Level=1; public string Mode="", Title="", Body="", ButtonLabel; public MathQuestion Q; public bool Buttons;
    public void SetStars(int s){Stars=s;} public void SetLevel(int l){Level=l;}
    public void ShowMessage(string t,string m,string b){Mode="message";Title=t;Body=m;ButtonLabel=b;Q=null;Buttons=false;}
    public void ShowQuestion(string t,MathQuestion q,bool show,string hint){Mode="question";Title=t;Q=q;Buttons=show;ButtonLabel=null;Body=hint;}
    public void ShowFeedback(bool pos,string text,int sel,int cor,string b){Mode="feedback";Body=text;ButtonLabel=b;}
    public void Answer(int i){AnswerSelected?.Invoke(i);} public void Next(){NextPressed?.Invoke();} }
  public class AIHelperRobot { public event Action<bool> Judged; public string Text; public bool Asking; public void ShowMessage(string m){Text=m;Asking=false;} public void ShowSuggestion(string m){Text=m;Asking=true;} public void Judge(bool b){Judged?.Invoke(b);} }
}
