module KeplerStation.Shields

module GridNode =
    let report (event: DiagnosticEvent) : GridNodeResult =
        { Status = if event.AnomalyCode < 4 then Online elif event.AnomalyCode < 7 then Degraded else Offline
          LoadFactor = event.SignalStrength / 100.0 }

module HarmonicCalibrator =
    let calibrate (event: DiagnosticEvent) : HarmonicCalibratorResult =
        { Frequency = Hertz (440.0 + float event.AnomalyCode * 10.0)
          Harmonics = if event.AnomalyCode < 5 then Calibrated else Uncalibrated }

module PowerRegulator =
    let regulate
        (energy: DiagnosticEvent -> CalibratedShieldEnergy)
        (event: DiagnosticEvent)
        : PowerRegulatorResult =
        let e = energy event
        let (Percentage stability) = e.Stability
        let (Hertz freq) = e.CalibratedFrequency
        let allocatedPower = freq / 1000.0

        { AllocatedPower = Megawatts allocatedPower
          Stability = if stability > 80.0 then Stable else Unstable }

module DeflectorGrid =
    let activate
        (node: DiagnosticEvent -> GridNodeResult)
        (harmonic: DiagnosticEvent -> HarmonicCalibratorResult)
        (event: DiagnosticEvent)
        : DeflectorGridResult =
        let n = node event
        let h = harmonic event
        let coverage = match n.Status with Online -> 95.0 | Degraded -> 75.0 | Offline -> 30.0

        { Coverage = Percentage coverage
          Harmonics = h.Harmonics }

module ShieldControl =
    let engage
        (power: DiagnosticEvent -> PowerRegulatorResult)
        (deflector: DiagnosticEvent -> DeflectorGridResult)
        (manualDeflector: DiagnosticEvent -> ManualDeflectorResult)
        (event: DiagnosticEvent)
        : ShieldControlResult =
        let p = power event
        let d = deflector event
        let md = manualDeflector event
        let (Megawatts allocated) = p.AllocatedPower

        match d.Harmonics with
        | Uncalibrated ->
            let (Percentage fixedCoverage) = md.FixedCoverage
            { ShieldsUp = p.Stability = Stable && fixedCoverage > 50.0
              PowerDraw = Megawatts (allocated * 0.7) }
        | Calibrated ->
            let (Percentage coverage) = d.Coverage
            { ShieldsUp = p.Stability = Stable && coverage > 80.0
              PowerDraw = Megawatts (allocated * 0.7) }
