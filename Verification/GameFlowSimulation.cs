using System; using RSPP;
static class Sim {
  static int fails; static void Check(bool c,string m){ if(!c){ fails++; if(fails<25) Console.WriteLine("FAIL: "+m);} }
  static void Main(){
    var rng=new Random(2026);
    long turns=0, helperChecks=0, minis=0, extraRolls=0, bonus=0, questions=0, levelUps=0, levelDowns=0, restarts=0; int games=0, maxSteps=0; long starSum=0;
    for(int g=0;g<4000;g++){
      var gm=new GameManager{ Board=new BoardPath(), Token=new PlayerToken(), Dice=new Dice(), Wheel=new SpinWheel(), Handle=new WheelHandle(), Panel=new QuestionPanel(), Robot=new AIHelperRobot(), Audio=new AudioManager() };
      gm.Initialize();
      Check(gm.State==GameState.Menu && gm.Panel.ButtonLabel==null,"initial state");
      // nothing works before Start
      Check(!gm.Dice.SimRoll(3),"die usable before start"); gm.Handle.Pull(); gm.Panel.Answer(0); gm.Panel.Next(); gm.Robot.Judge(true);
      Check(gm.State==GameState.Menu,"state changed before start");
      gm.StartGame();
      double skill=rng.NextDouble(); int steps=0, lastStars=0, lastLevel=1; bool restartedOnce=false;
      while(gm.State!=GameState.Finished && steps<5000){
        steps++;
        // random out-of-turn inputs must be ignored
        var before=gm.State; int r=rng.Next(12);
        if(r==0 && before!=GameState.WaitingForSpin){ gm.Handle.Pull(); Check(gm.State==before,"handle accepted in "+before); }
        if(r==1 && before!=GameState.Question){ gm.Panel.Answer(rng.Next(4)); Check(gm.State==before,"answer accepted in "+before); }
        if(r==2 && before!=GameState.Feedback){ gm.Panel.Next(); Check(gm.State==before,"next accepted in "+before); }
        if(r==3 && before!=GameState.HelperCheck){ gm.Robot.Judge(true); Check(gm.State==before,"judge accepted in "+before); }
        if(r==4 && before!=GameState.WaitingForRoll){ Check(!gm.Dice.SimRoll(2) && gm.State==before,"roll accepted in "+before); }
        if(r==5){ gm.StartGame(); Check(gm.State==before,"Start Game restarted a running game"); }
        if(!restartedOnce && rng.Next(400)==0){ gm.RestartGame(); restartedOnce=true; restarts++; lastStars=0; lastLevel=1;
          Check(gm.State==GameState.WaitingForRoll && gm.Stars==0 && gm.Position==0 && gm.Panel.Stars==0 && gm.Panel.Level==1 && gm.Token.Shown==0,"restart resets"); }
        switch(gm.State){
          case GameState.WaitingForRoll:{
            turns++; bool extra=gm.Panel.Title=="Extra Roll!"; if(extra) extraRolls++;
            Check(gm.Dice.RollEnabled && gm.Dice.Highlighted && !gm.Handle.Highlighted,"roll phase cues");
            int pos=gm.Position, n=rng.Next(1,7); gm.Dice.SimRoll(n);
            int expect=Math.Min(pos+n,25);
            Check(gm.Position==expect && gm.Token.Shown==expect,"token position after roll");
            Check(!gm.Dice.RollEnabled || gm.State==GameState.WaitingForRoll,"die disabled after roll");
            if(expect==25) Check(gm.State==GameState.Finished,"finish at last space");
            else if(extra) Check(gm.State==GameState.WaitingForRoll && gm.Panel.Title=="Your Turn","after extra roll -> normal roll");
            else if(gm.Board.IsStarSpace(expect)){ Check(gm.State==GameState.Feedback && gm.Panel.Title=="Mini Challenge" && gm.Panel.ButtonLabel=="Go!","star space -> mini intro"); minis++; }
            else Check(gm.State==GameState.WaitingForSpin && gm.Handle.Highlighted,"-> spin phase");
            break; }
          case GameState.WaitingForSpin:{
            gm.Handle.Pull(); Check(gm.State==GameState.Spinning && gm.Wheel.IsSpinning,"spinning");
            gm.Handle.Pull(); Check(gm.State==GameState.Spinning,"double pull ignored");
            var seg=GameConfig.WheelLayout[rng.Next(GameConfig.WheelLayout.Length)]; int s0=gm.Stars; gm.Wheel.SimStop(seg);
            if(seg==WheelSegment.Bonus){ bonus++; Check(gm.Stars==s0+1 && gm.State==GameState.Feedback,"bonus star"); }
            else if(seg==WheelSegment.MiniChallenge){ minis++; Check(gm.State==GameState.Feedback && gm.Panel.ButtonLabel=="Go!","wheel mini intro"); }
            else { Check(gm.State==GameState.Question||gm.State==GameState.HelperCheck,"question after wheel");
                   Check(gm.Panel.Q!=null && gm.Panel.Q.Category.ToString()==seg.ToString(),"category matches wheel");
                   Check(gm.Panel.Q.Level==gm.Panel.Level,"question level matches difficulty"); }
            break; }
          case GameState.Question:{
            questions++; var q=gm.Panel.Q; Check(q!=null && gm.Panel.Buttons && !gm.Robot.Asking,"question view");
            bool mini=gm.Panel.Title.StartsWith("Mini Challenge"); int s0=gm.Stars; bool right=rng.NextDouble()<skill;
            int pick=right?q.CorrectIndex:(q.CorrectIndex+1+rng.Next(q.Choices.Length-1))%q.Choices.Length;
            gm.Panel.Answer(pick);
            Check(gm.State==GameState.Feedback && gm.Panel.Mode=="feedback" && gm.Panel.ButtonLabel=="Next","feedback shown");
            if(right) Check(gm.Stars==s0+(mini?0:1) && gm.Panel.Body.StartsWith("Correct!"),"correct -> star/feedback");
            else Check(gm.Stars==s0 && gm.Panel.Body.Contains(q.Explanation) && gm.Panel.Body.StartsWith("Not quite."),"incorrect -> explanation, no penalty");
            gm.Panel.Answer(q.CorrectIndex); Check(gm.Stars==(right&&!mini?s0+1:s0),"second answer ignored");
            if(gm.Panel.Level>lastLevel){ levelUps++; Check(gm.Panel.Body.Contains("Level up"),"level up message"); }
            if(gm.Panel.Level<lastLevel){ levelDowns++; Check(gm.Panel.Body.Contains("smaller numbers"),"level down message"); }
            lastLevel=gm.Panel.Level; break; }
          case GameState.HelperCheck:{
            helperChecks++; var q=gm.Panel.Q; Check(q!=null && !gm.Panel.Buttons && gm.Robot.Asking && gm.Robot.Text.StartsWith("I think the answer is "),"helper view");
            string said=gm.Robot.Text.Substring("I think the answer is ".Length).TrimEnd('!'); bool helperRight=said==q.CorrectText;
            bool judge=rng.NextDouble()<skill?helperRight:!helperRight; int s0=gm.Stars, lv=gm.Panel.Level; gm.Robot.Judge(judge);
            Check(gm.State==GameState.Feedback && !gm.Robot.Asking,"helper feedback");
            Check(gm.Stars==s0+(judge==helperRight?1:0),"helper star only for good judgment");
            Check(gm.Panel.Body.Contains(q.Explanation),"helper feedback explains");
            Check(gm.Panel.Level==lv,"helper check does not change level");
            break; }
          case GameState.Feedback:{
            int s0=gm.Stars; string title=gm.Panel.Title, body=gm.Panel.Body, mode=gm.Panel.Mode; gm.Panel.Next();
            Check(gm.State!=GameState.Feedback || gm.Panel.Mode!=mode || gm.Panel.Body!=body,"Next made progress");
            if(gm.State==GameState.Feedback && gm.Panel.Mode=="message" && gm.Panel.Body.StartsWith("You got ")){
               int c=int.Parse(gm.Panel.Body.Substring(8,1)); Check(gm.Stars==s0+(c>=2?1:0),"mini reward star"); 
               gm.Panel.Next(); Check(gm.State==GameState.WaitingForRoll && (gm.Panel.Title=="Extra Roll!")==(c==3),"mini extra roll only for 3/3"); }
            break; }
          default: Check(false,"stuck in state "+gm.State); steps=5000; break;
        }
        Check(gm.Stars>=lastStars && gm.Panel.Stars==gm.Stars,"star counter consistent"); lastStars=gm.Stars;
        Check(gm.Panel.Level>=1 && gm.Panel.Level<=2,"level range");
      }
      Check(gm.State==GameState.Finished,"game did not finish"); maxSteps=Math.Max(maxSteps,steps);
      Check(gm.Panel.Title=="FINISH!" && gm.Panel.ButtonLabel=="Play Again" && gm.Panel.Body.Contains(gm.Stars+" star"),"finish screen");
      Check(!gm.Dice.SimRoll(1),"die disabled after finish");
      games++; starSum+=gm.Stars;
      if(g%2==0){ gm.Panel.Next(); } else { gm.StartGame(); }
      Check(gm.State==GameState.WaitingForRoll && gm.Stars==0 && gm.Position==0,"play again / start after finish");
    }
    Console.WriteLine($"games finished: {games}, avg turns/game: {(double)turns/games:F1}, avg stars: {(double)starSum/games:F1}, max steps: {maxSteps}");
    Console.WriteLine($"questions: {questions}, helper checks: {helperChecks}, mini challenges: {minis}, extra rolls: {extraRolls}, bonus: {bonus}, level ups: {levelUps}, level downs: {levelDowns}, mid-game restarts: {restarts}");
    Console.WriteLine(fails==0?"ALL GAME-FLOW SIMULATION CHECKS PASSED":"FAILURES: "+fails); Environment.Exit(fails==0?0:1);
  }
}
