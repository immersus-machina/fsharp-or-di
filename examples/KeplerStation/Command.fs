module KeplerStation.Command

module StationCommand =
    let execute
        (threat: DiagnosticEvent -> ThreatAssessmentResult)
        (response: DiagnosticEvent -> ResponseCoordinatorResult)
        (log: DiagnosticEvent -> StationLogResult)
        (event: DiagnosticEvent)
        : StationCommandResult =
        let t = threat event
        let r = response event
        let _ = log event

        let actions =
            (if r.DefensesActive then 1 else 0)
            + (if r.CourseAdjusted then 1 else 0)
            + (if r.CrewNotified then 1 else 0)
            + (if r.Contained then 1 else 0)

        { Decision = sprintf "%A — %s" t.Level t.Summary
          ThreatLevel = t.Level
          ActionsExecuted = actions }
