namespace RhythmGame.Core

open System

// Distinct scoring accuracy evaluation tiers
type Judgment =
    | Perfect // Within 40ms
    | Great // Within 90ms
    | Good // Within 150ms
    | Miss // Out of bounds or skipped

type Note =
    { TargetTime: double
      IsProcessed: bool }

type GameState =
    { Score: int
      Combo: int
      Multiplier: int
      LastJudgment: Judgment option
      Notes: Note list }

module RhythmEngine =
    // Precision millisecond boundaries converted to seconds
    [<Literal>]
    let PerfectWindow = 0.040

    [<Literal>]
    let GreatWindow = 0.090

    [<Literal>]
    let GoodWindow = 0.150

    let createInitialState (times: double list) : GameState =
        { Score = 0
          Combo = 0
          Multiplier = 1
          LastJudgment = None
          Notes = times |> List.map (fun t -> { TargetTime = t; IsProcessed = false }) }

    // Evaluates what a specific timing differential translates to in judgment tiers
    let evaluateTiming (delta: double) : Judgment =
        if delta <= PerfectWindow then Perfect
        elif delta <= GreatWindow then Great
        elif delta <= GoodWindow then Good
        else Miss

    // Translates judgment tiers into raw baseline scores
    let getBaseScore =
        function
        | Perfect -> 300
        | Great -> 100
        | Good -> 50
        | Miss -> 0

    // Calculates scaling multiplier tier increments based on ongoing streak benchmarks
    let calculateMultiplier (combo: int) : int =
        if combo >= 40 then 4
        elif combo >= 20 then 3
        elif combo >= 10 then 2
        else 1

    // 1. Tick engine update loop sweep
    let updateTimeTick (elapsed: double) (state: GameState) : GameState =
        // Auto-miss notes that pass entirely out of the widest acceptable window (+150ms)
        let notePastWindow n =
            not n.IsProcessed && elapsed > n.TargetTime + GoodWindow

        let missedNotesExist = state.Notes |> List.exists notePastWindow

        if missedNotesExist then
            let updatedNotes =
                state.Notes
                |> List.map (fun n ->
                    if notePastWindow n then
                        { n with IsProcessed = true }
                    else
                        n)

            { state with
                Notes = updatedNotes
                Combo = 0
                Multiplier = 1
                LastJudgment = Some Miss }
        else
            state

    // 2. Action engine player input execution intercept
    let registerPlayerTap (elapsed: double) (state: GameState) : GameState =
        let getProximity n = abs (n.TargetTime - elapsed)

        let targetNote =
            state.Notes
            |> List.filter (fun n -> not n.IsProcessed && getProximity n <= GoodWindow)
            |> List.sortBy getProximity
            |> List.tryHead

        match targetNote with
        | Some note ->
            let judgment = evaluateTiming (getProximity note)

            let updatedNotes =
                state.Notes
                |> List.map (fun n ->
                    if n.TargetTime = note.TargetTime then
                        { n with IsProcessed = true }
                    else
                        n)

            match judgment with
            | Miss -> // Boundary configuration fall-through fail-safes
                { state with
                    Notes = updatedNotes
                    Combo = 0
                    Multiplier = 1
                    LastJudgment = Some Miss }
            | hitJudgment ->
                let nextCombo = state.Combo + 1
                let nextMult = calculateMultiplier nextCombo
                let addedValue = getBaseScore hitJudgment * state.Multiplier

                { Score = state.Score + addedValue
                  Combo = nextCombo
                  Multiplier = nextMult
                  LastJudgment = Some hitJudgment
                  Notes = updatedNotes }
        | None ->
            // Penalty for stray blind taps out of phase completely
            { state with
                Combo = 0
                Multiplier = 1
                LastJudgment = Some Miss }
