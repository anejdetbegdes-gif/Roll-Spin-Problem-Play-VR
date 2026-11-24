param([string]$OutDir)
Add-Type -AssemblyName System.Speech
New-Item -ItemType Directory -Force $OutDir | Out-Null

$lines = [ordered]@{
  'what_is' = 'What is'; 'plus' = 'plus'; 'minus' = 'minus'; 'equals' = 'equals'; 'is' = 'Is';
  'odd_or_even' = 'odd, or even?'; 'which_number_is_even' = 'Which number is even?'; 'which_number_is_odd' = 'Which number is odd?';
  'is_even' = 'is even.'; 'is_odd' = 'is odd.';
  'roll_the_die' = 'Your turn! Grab the die with your left hand, then let go.';
  'extra_roll' = 'You won an extra roll! Roll the die again.';
  'spin_the_wheel' = 'Spin the wheel! Hold the handle with your left hand, and press the trigger.';
  'robo_idea' = 'Robo has an idea! Touch Robo with your left hand.';
  'robo_check' = 'Can you help Robo check it? Press correct, or incorrect.';
  'correct' = 'Correct! Great job!'; 'not_quite' = 'Not quite.';
  'great_checking' = 'Great checking!'; 'lets_check' = "Let's check it together.";
  'mini_challenge' = 'Mini challenge! Answer three quick questions.';
  'bonus_star' = 'Lucky spin! You get a free star!'; 'you_did_it' = 'You did it! Great job!'
}
$numbers = 'zero','one','two','three','four','five','six','seven','eight','nine','ten','eleven','twelve','thirteen','fourteen','fifteen','sixteen','seventeen','eighteen','nineteen','twenty'
for ($i = 0; $i -le 20; $i++) { $lines["n$i"] = $numbers[$i] }

function Trim-Wav([string]$path) {
  $b = [IO.File]::ReadAllBytes($path)
  # Find the "data" chunk (SAPI headers are not always 44 bytes).
  $p = 12; $dataStart = -1; $dataLen = 0
  while ($p -lt $b.Length - 8) {
    $id = [Text.Encoding]::ASCII.GetString($b, $p, 4); $len = [BitConverter]::ToInt32($b, $p + 4)
    if ($id -eq 'data') { $dataStart = $p + 8; $dataLen = $len; break }
    $p += 8 + $len
  }
  if ($dataStart -lt 0) { return }
  $count = [int]($dataLen / 2); $first = 0; $last = $count - 1; $th = 400
  while ($first -lt $count -and [Math]::Abs([BitConverter]::ToInt16($b, $dataStart + 2 * $first)) -lt $th) { $first++ }
  while ($last -gt $first -and [Math]::Abs([BitConverter]::ToInt16($b, $dataStart + 2 * $last)) -lt $th) { $last-- }
  $pad = 880   # 40 ms at 22050 Hz
  $first = [Math]::Max(0, $first - $pad); $last = [Math]::Min($count - 1, $last + $pad)
  $n = ($last - $first + 1) * 2
  $ms = New-Object IO.MemoryStream; $w = New-Object IO.BinaryWriter($ms)
  $w.Write([Text.Encoding]::ASCII.GetBytes('RIFF')); $w.Write([int](36 + $n)); $w.Write([Text.Encoding]::ASCII.GetBytes('WAVEfmt '))
  $w.Write([int]16); $w.Write([int16]1); $w.Write([int16]1); $w.Write([int]22050); $w.Write([int]44100); $w.Write([int16]2); $w.Write([int16]16)
  $w.Write([Text.Encoding]::ASCII.GetBytes('data')); $w.Write([int]$n); $w.Write($b, $dataStart + 2 * $first, $n); $w.Flush()
  [IO.File]::WriteAllBytes($path, $ms.ToArray())
}

$s = New-Object System.Speech.Synthesis.SpeechSynthesizer
$s.SelectVoice('Microsoft Zira Desktop')
$s.Rate = -1
$fmt = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(22050, [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, [System.Speech.AudioFormat.AudioChannel]::Mono)
foreach ($k in $lines.Keys) {
  $file = Join-Path $OutDir "$k.wav"
  $s.SetOutputToWaveFile($file, $fmt); $s.Speak($lines[$k]); $s.SetOutputToNull()
  Trim-Wav $file
}
$s.Dispose()
"Generated: " + (Get-ChildItem $OutDir -Filter *.wav).Count
