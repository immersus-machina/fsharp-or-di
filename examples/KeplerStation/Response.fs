module KeplerStation.Response

module ResponseCoordinator =
    let coordinate
        (shields: DiagnosticEvent -> ShieldControlResult)
        (propulsion: DiagnosticEvent -> PropulsionControlResult)
        (crew: DiagnosticEvent -> CrewAlertResult)
        (containment: DiagnosticEvent -> ContainmentStatusResult)
        (event: DiagnosticEvent)
        : ResponseCoordinatorResult =
        let s = shields event
        let p = propulsion event
        let c = crew event
        let ct = containment event

        { DefensesActive = s.ShieldsUp
          CourseAdjusted = p.CourseSet
          CrewNotified = c.AlertsSent > 0
          Contained = ct.IsSecure }
