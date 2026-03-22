module KeplerStation.Emergency

module BlindTrajectoryPlotter =
    let plot
        (sensors: DiagnosticEvent -> SensorArrayResult)
        (event: DiagnosticEvent)
        : BlindTrajectoryResult =
        let _ = sensors event

        { AssumedImpactProbability = Percentage 95.0
          WorstCaseEta = Seconds 600.0 }

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
        let _ = node event

        { FixedCoverage = Percentage 60.0 }
