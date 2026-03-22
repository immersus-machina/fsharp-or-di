module KeplerStation.Emergency

module BlindTrajectoryPlotter =
    let plot
        (sensors: DiagnosticEvent -> SensorArrayResult)
        (event: DiagnosticEvent)
        : BlindTrajectoryResult =
        let s = sensors event
        let eta = if s.ProximityAlert then Seconds 300.0 else Seconds 600.0

        { AssumedImpactProbability = Percentage 95.0
          WorstCaseEta = eta }

module EmergencyThreatClassifier =
    let classify
        (ionFlux: DiagnosticEvent -> IonFluxReaderResult)
        (event: DiagnosticEvent)
        : EmergencyThreatResult =
        let i = ionFlux event

        { AssumedThreat = if i.IsElevated then IonideStorm else Unknown
          AssumedSeverity = if i.IsElevated then Maximum else Nominal }

module ManualDeflectorGrid =
    let activate
        (node: DiagnosticEvent -> GridNodeResult)
        (event: DiagnosticEvent)
        : ManualDeflectorResult =
        let n = node event
        let baseCoverage = match n.Status with Online -> 60.0 | Degraded -> 45.0 | Offline -> 30.0

        { FixedCoverage = Percentage baseCoverage }
