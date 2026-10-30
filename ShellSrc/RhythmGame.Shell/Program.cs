using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using RhythmGame.Core; // Brings in our F# types seamlessly

namespace RhythmGame.Shell
{
    public class Program
    {
        private const double TrackLength = 5.0;

        public static void Main()
        {
            Console.Clear();
            Console.CursorVisible = false;
            Console.WriteLine("🎮 INTEROP ENGINE ONLINE! [SPACEBAR] to play.");
            Thread.Sleep(1000);

            // Seed note timings via native F# collection types (FSharpList)
            var noteTimeline = Microsoft.FSharp.Collections.ListModule.OfSeq(
                new double[] { 1.0, 1.8, 2.5, 3.2, 4.0 }
            );

            // Generate initial immutable F# state record structure blueprint
            GameState state = RhythmEngine.createInitialState(noteTimeline);
            
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed.TotalSeconds < TrackLength)
            {
                double elapsed = stopwatch.Elapsed.TotalSeconds;

                // Call F# to evaluate missed targets (returns updated snapshot reference pointer)
                state = RhythmEngine.updateTimeTick(elapsed, state);

                // Poll system input loop framework thread channels
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Spacebar)
                    {
                        // Pass current frame parameters to pure F# evaluator logic
                        state = RhythmEngine.registerPlayerTap(elapsed, state);
                    }
                }

                DrawInterfaceFrame(elapsed, state);
                Thread.Sleep(16); // Sync to continuous ~60Hz logic rate intervals
            }

            Console.Clear();
            Console.WriteLine("🏁 SONG FINISHED!");
            Console.WriteLine($"Final Score: {state.Score} | Final Max Combo Stretch: {state.Combo}");
            Console.CursorVisible = true;
        }

        private static void DrawInterfaceFrame(double elapsed, GameState state)
        {
            Console.SetCursorPosition(0, 2);
            Console.WriteLine($"Timeline: [ {elapsed:F2}s / {TrackLength:F2}s ] | Score: {state.Score} | Combo: {state.Combo}    ");
            Console.WriteLine("==================================================================");
            Console.Write("Track: [ ");

            // Query immutable F# structural note lists inside C# comfortably using LINQ structures
            for (double t = 0; t < TrackLength; t += 0.2)
            {
                var note = state.Notes.FirstOrDefault(n => Math.Abs(n.TargetTime - t) < 0.1);

                if (Math.Abs(t - elapsed) < 0.1) Console.Write("||"); // Strike playhead
                else if (note != null)
                {
                    if (note.IsHit) Console.Write(" ");
                    else if (note.IsMissed) Console.Write("x");
                    else Console.Write("O");
                }
                else Console.Write("-");
            }
            Console.WriteLine(" ]");
        }
    }
}
