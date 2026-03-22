module KeplerStation.Threats

module TrajectoryPlotter =
    let plot
        (sensors: DiagnosticEvent -> SensorArrayResult)
        (event: DiagnosticEvent)
        : TrajectoryPlotterResult =
        let s = sensors event
        let boost = if s.ProximityAlert then 2.0 else 1.0

        { ImpactProbability = Percentage (event.SignalStrength / 100.0 * boost)
          EstimatedEta = Seconds (3600.0 / (float event.AnomalyCode + 1.0)) }

module MassEstimator =
    let estimate (event: DiagnosticEvent) : MassEstimatorResult =
        { KilogramsEstimate = event.SignalStrength * 1e6
          Confidence = Percentage 85.0 }

module IonFluxReader =
    let read (event: DiagnosticEvent) : IonFluxReaderResult =
        { FluxDensity = event.SignalStrength * 2.2
          IsElevated = event.SignalStrength > 50.0 }

module MagneticFieldReader =
    let read (event: DiagnosticEvent) : MagneticFieldReaderResult =
        { TeslaReading = event.SignalStrength * 0.001
          Polarity = if event.AnomalyCode % 2 = 0 then 1 else -1 }

module AsteroidAnalyzer =
    let analyze
        (trajectory: DiagnosticEvent -> TrajectoryPlotterResult)
        (blindTrajectory: DiagnosticEvent -> BlindTrajectoryResult)
        (mass: DiagnosticEvent -> MassEstimatorResult)
        (event: DiagnosticEvent)
        : AsteroidAnalyzerResult =
        let t = trajectory event
        let bt = blindTrajectory event
        let m = mass event
        let (Percentage normalProb) = t.ImpactProbability
        let (Percentage blindProb) = bt.AssumedImpactProbability
        let impactProb = if normalProb < 0.3 then blindProb else normalProb
        let eta = if normalProb < 0.3 then bt.WorstCaseEta else t.EstimatedEta
        let danger =
            if impactProb > 0.5 && m.KilogramsEstimate > 1e8 then Critical
            elif impactProb > 0.3 then Moderate
            else Low

        { Danger = danger
          ImpactWindow = eta }

module StormAnalyzer =
    let analyze
        (ionFlux: DiagnosticEvent -> IonFluxReaderResult)
        (magnetic: DiagnosticEvent -> MagneticFieldReaderResult)
        (event: DiagnosticEvent)
        : StormAnalyzerResult =
        let i = ionFlux event
        let m = magnetic event
        let severity = if i.IsElevated && m.TeslaReading > 0.05 then Severe else Mild

        { Severity = severity
          ShieldRecommendation = match severity with Severe -> Maximum | Mild -> Nominal }

module ThreatClassifier =
    let classify
        (asteroid: DiagnosticEvent -> AsteroidAnalyzerResult)
        (storm: DiagnosticEvent -> StormAnalyzerResult)
        (event: DiagnosticEvent)
        : ThreatClassifierResult =
        let a = asteroid event
        let s = storm event

        { PrimaryThreat = if a.Danger = Critical then Asteroid else Storm
          SecondaryThreat = if a.Danger = Critical then Storm else Asteroid }

module RiskScorer =
    let score
        (sensors: DiagnosticEvent -> SensorArrayResult)
        (classifier: DiagnosticEvent -> ThreatClassifierResult)
        (containment: DiagnosticEvent -> ContainmentStatusResult)
        (event: DiagnosticEvent)
        : RiskScorerResult =
        let s = sensors event
        let c = classifier event
        let ct = containment event
        let alerts = (if s.ProximityAlert then 0.3 else 0.0) + (if s.AtmosphereAlert then 0.2 else 0.0)
        let threat = if c.PrimaryThreat = Asteroid then 0.5 else 0.2
        let containmentPenalty = if ct.IsSecure then 0.0 else 0.2
        let total = alerts + threat + containmentPenalty

        { OverallRisk = total
          Category = if total > 0.7 then Red elif total > 0.4 then Yellow else Green }

module ThreatAssessment =
    let assess
        (classifier: DiagnosticEvent -> ThreatClassifierResult)
        (emergencyThreat: DiagnosticEvent -> EmergencyThreatResult)
        (riskScorer: DiagnosticEvent -> RiskScorerResult)
        (event: DiagnosticEvent)
        : ThreatAssessmentResult =
        let et = emergencyThreat event
        let c = classifier event
        let r = riskScorer event

        match et.AssumedThreat with
        | IonideStorm ->
            { Level = et.AssumedSeverity
              Summary = sprintf "IONIDE STORM — emergency assessment active, risk %.1f" r.OverallRisk }
        | _ ->
            let level = if r.OverallRisk > 0.7 then Maximum elif r.OverallRisk > 0.4 then Elevated else Nominal

            { Level = level
              Summary = sprintf "%A threat (%A), risk %.1f" c.PrimaryThreat r.Category r.OverallRisk }
