These files are NOT part of the Unity project (Unity ignores folders outside Assets).
They are the checks that were run with the .NET 8 SDK (Roslyn compiler), without Unity:

  LogicTests.cs             runs the real Assets/Scripts/Logic/*.cs classes
  GameFlowSimulation.cs     runs the real Assets/Scripts/Core/GameManager.cs through 4,000 simulated games
  GameFlowTestDoubles.cs    simple stand-ins for the Unity components used by the simulation
  UnityApiStubs.cs          signature-only stand-ins for the Unity / XR Interaction Toolkit APIs,
                            used to compile-check all project scripts

They prove the game rules and the turn flow. They do NOT prove that the project compiles in Unity
or works on a headset.
