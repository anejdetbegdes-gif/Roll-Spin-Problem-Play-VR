using System;
using System.Collections.Generic;
using System.Linq;
using RSPP;

static class T {
  static int fails = 0;
  static void Check(bool c, string msg) { if (!c) { fails++; if (fails < 30) Console.WriteLine("FAIL: " + msg); } }

  static void Main() {
    var rng = new Random(12345);
    var gen = new QuestionGenerator(rng);
    int n = 0;
    var seenPrompts = new HashSet<string>();
    foreach (int level in new[]{1,2})
    foreach (QuestionCategory cat in Enum.GetValues(typeof(QuestionCategory)))
    for (int i = 0; i < 50000; i++) {
      var q = gen.Next(cat, level); n++;
      seenPrompts.Add(level + ":" + q.Prompt);
      string id = level + " " + q.Prompt + " [" + string.Join(",", q.Choices) + "] c=" + q.CorrectIndex;
      Check(q.Category == cat && q.Level == level, "cat/level " + id);
      Check(q.Choices.Length >= 2 && q.Choices.Length <= 4, "choice count " + id);
      Check(q.Choices.Distinct().Count() == q.Choices.Length, "duplicate choices " + id);
      Check(q.CorrectIndex >= 0 && q.CorrectIndex < q.Choices.Length, "index " + id);
      Check(!string.IsNullOrEmpty(q.Explanation), "explanation " + id);
      int max = level == 1 ? 10 : 20;
      if (cat != QuestionCategory.OddEven) {
        Check(q.Choices.Length == 4, "4 choices " + id);
        var parts = q.Prompt.Split(' ');
        int a = int.Parse(parts[0]), b = int.Parse(parts[2]);
        int expected = cat == QuestionCategory.Addition ? a + b : a - b;
        Check(parts[1] == (cat == QuestionCategory.Addition ? "+" : "-"), "operator " + id);
        Check(int.Parse(q.CorrectText) == expected, "wrong correct answer " + id);
        Check(a >= 0 && a <= max && b >= 0 && b <= max && expected >= 0 && expected <= max, "operands outside level range " + id);
        if (level == 2) Check(Math.Max(a, expected) > 10, "level 2 not harder than level 1 " + id);
        foreach (var c in q.Choices) { int v = int.Parse(c); Check(v >= 0 && v <= max, "choice out of range " + id); }
        var sorted = q.Choices.Select(int.Parse).ToList();
        Check(sorted.SequenceEqual(sorted.OrderBy(x => x)), "choices not sorted " + id);
        Check(q.Explanation.Contains(expected.ToString()), "explanation lacks answer " + id);
      } else if (q.Choices.Length == 2) {
        int v = int.Parse(q.Prompt.Split(' ')[1]);
        Check(v >= 1 && v <= max, "odd/even range " + id);
        Check(q.CorrectText == (v % 2 == 0 ? "Even" : "Odd"), "odd/even answer " + id);
        if (level == 2) Check(v > 10, "odd/even level 2 range " + id);
      } else {
        bool wantEven = q.Prompt.Contains("even");
        var vals = q.Choices.Select(int.Parse).ToList();
        Check(vals.Count == 4, "4 number choices " + id);
        Check(vals.Count(x => (x % 2 == 0) == wantEven) == 1, "exactly one matching parity " + id);
        Check((vals[q.CorrectIndex] % 2 == 0) == wantEven, "parity correct " + id);
        foreach (var v in vals) Check(v >= 1 && v <= max, "range " + id);
      }
    }
    Console.WriteLine("questions checked: " + n + ", distinct prompts: " + seenPrompts.Count);

    // NextRandom covers all categories
    var cats = new HashSet<QuestionCategory>();
    for (int i = 0; i < 300; i++) cats.Add(gen.NextRandom(1).Category);
    Check(cats.Count == 3, "NextRandom categories");

    // Adaptive difficulty
    var d = new AdaptiveDifficulty();
    Check(d.Level == 1, "start level 1");
    Check(d.RecordAnswer(true) == 0 && d.RecordAnswer(true) == 0 && d.Level == 1, "2 correct stays L1");
    Check(d.RecordAnswer(true) == 1 && d.Level == 2, "3 correct -> L2");
    Check(d.RecordAnswer(true) == 0 && d.RecordAnswer(true) == 0 && d.RecordAnswer(true) == 0 && d.Level == 2, "stays within max");
    Check(d.RecordAnswer(false) == 0 && d.Level == 2, "1 wrong stays L2");
    Check(d.RecordAnswer(true) == 0 && d.RecordAnswer(false) == 0 && d.Level == 2, "wrong, correct, wrong: not in a row");
    Check(d.RecordAnswer(false) == -1 && d.Level == 1, "2 wrong in a row -> L1");
    Check(d.RecordAnswer(false) == 0 && d.RecordAnswer(false) == 0 && d.RecordAnswer(false) == 0 && d.Level == 1, "stays within min");
    Check(d.RecordAnswer(true) == 0 && d.RecordAnswer(true) == 0 && d.RecordAnswer(false) == 0 && d.RecordAnswer(true) == 0 && d.Level == 1, "streak broken");
    Check(d.RecordAnswer(true) == 0 && d.RecordAnswer(true) == 1 && d.Level == 2, "streak rebuilt -> L2");
    d.Reset(); Check(d.Level == 1 && d.CorrectStreak == 0 && d.WrongStreak == 0, "reset");
    var r2 = new Random(7);
    for (int i = 0; i < 100000; i++) { d.RecordAnswer(r2.Next(2) == 0); Check(d.Level >= 1 && d.Level <= 2, "level bounds"); }

    // AI helper
    var h = new AIHelperLogic(new Random(99));
    int appear = 0, total = 200000, gap = 0, maxGap = 0; bool prev = false; int correct = 0, sug = 0, same = 0, maxSame = 0; bool lastC = false; bool first = true;
    for (int i = 0; i < total; i++) {
      bool a = h.ShouldAppear();
      Check(!(a && prev), "helper twice in a row");
      if (a) {
        appear++; gap = 0;
        var q = gen.NextRandom(1 + i % 2);
        var s = h.MakeSuggestion(q); sug++;
        Check(s.SuggestedIndex >= 0 && s.SuggestedIndex < q.Choices.Length, "suggest index");
        Check(s.IsCorrect == (s.SuggestedIndex == q.CorrectIndex), "IsCorrect flag consistent");
        Check(s.Text == "I think the answer is " + q.Choices[s.SuggestedIndex] + "!", "text");
        if (s.IsCorrect) correct++;
        if (!first && s.IsCorrect == lastC) same++; else same = 1;
        first = false; lastC = s.IsCorrect; maxSame = Math.Max(maxSame, same);
      } else { gap++; maxGap = Math.Max(maxGap, gap); }
      prev = a;
    }
    Check(maxGap <= 2, "helper gap too long: " + maxGap);
    Check(maxSame <= 2, "same correctness too many times in a row: " + maxSame);
    Console.WriteLine($"helper appears on {100.0*appear/total:F1}% of questions; suggestions correct {100.0*correct/sug:F1}%; max gap {maxGap}; max same-result streak {maxSame}");
    Console.WriteLine(fails == 0 ? "ALL LOGIC TESTS PASSED" : ("FAILURES: " + fails));
    Environment.Exit(fails == 0 ? 0 : 1);
  }
}
