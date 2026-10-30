namespace RhythmGame.Core

open System

type Note = {
    TargetTime : double
    IsHit : bool
    IsMissed : bool
}

type GameState = {
    Score : int
    Combo : int
    Notes : Note list
}

module RhythmEngine =
    let [<Literal>] HitWindow = 0.15 // +/- 150ms

    // Initializes a starting game snapshot configuration
    let createInitialState (times: double list) : GameState =
        { Score = 0; Combo = 0; Notes = times |> List.map (fun t -> { TargetTime = t; IsHit = false; IsMissed = false }) }

    // 1. Tick function: Sweeps past notes to flag missed inputs automatically
    let updateTimeTick (elapsed: double) (state: GameState) : GameState =
        let processNote n =
            if not n.IsHit && not n.IsMissed && elapsed > n.TargetTime + HitWindow then
                { n with IsMissed = true }
            else n
        
        let updatedNotes = state.Notes |> List.map processNote
        // If any note transitioned to 'Missed' on this tick frame, break combo streak
        let brokenCombo = updatedNotes |> List.exists (fun n -> n.IsMissed && not (state.Notes |> List.find (fun old -> old.TargetTime = n.TargetTime)).IsMissed)
        
        { state with 
            Notes = updatedNotes
            Combo = if brokenCombo then 0 else state.Combo }

    // 2. Action function: Evaluates an explicit tap input action event
    let registerPlayerTap (elapsed: double) (state: GameState) : GameState =
        let getProximity n = abs (n.TargetTime - elapsed)
        
        let validTarget = 
            state.Notes 
            |> List.filter (fun n -> not n.IsHit && not n.IsMissed && getProximity n <= HitWindow)
            |> List.sortBy getProximity
            |> List.tryHead

        match validTarget with
        | Some note ->
            let updatedNotes = state.Notes |> List.map (fun n -> if n.TargetTime = note.TargetTime then { n with IsHit = true } else n)
            let newCombo = state.Combo + 1
            { Score = state.Score + (100 * newCombo); Combo = newCombo; Notes = updatedNotes }
        | None -> 
            { state with Combo = 0 } // Penalty for firing inputs out of phase
