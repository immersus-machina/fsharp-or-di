module KeplerStation.Sensors

module RadarPulse =
    let scan
        (energy: DiagnosticEvent -> FocusedEnergy)
        (event: DiagnosticEvent)
        : RadarPulseResult =
        let e = energy event
        let boost = e.SignalBoost

        { EchoDistance = event.SignalStrength * 1.5 * boost
          EchoCount = event.AnomalyCode % 5 + 1 }

module GravityWave =
    let detect
        (energy: DiagnosticEvent -> FocusedEnergy)
        (event: DiagnosticEvent)
        : GravityWaveResult =
        let e = energy event
        let (Percentage intensity) = e.FocusIntensity
        let sensitivity = intensity / 100.0

        { WaveAmplitude = event.SignalStrength * 0.3 * sensitivity
          SourceBearing = Bearing (42.0 + float event.AnomalyCode) }

module PressureReader =
    let read (event: DiagnosticEvent) : PressureReaderResult =
        { Reading = Pascals (101325.0 + event.SignalStrength * 10.0)
          Stability = if event.AnomalyCode < 5 then Stable else Unstable }

module TemperatureReader =
    let read (event: DiagnosticEvent) : TemperatureReaderResult =
        { Reading = Kelvin (293.0 + event.SignalStrength * 0.1)
          DeltaRate = float event.AnomalyCode * 0.5 }

module ProximitySensor =
    let analyze
        (radar: DiagnosticEvent -> RadarPulseResult)
        (gravity: DiagnosticEvent -> GravityWaveResult)
        (event: DiagnosticEvent)
        : ProximitySensorResult =
        let r = radar event
        let g = gravity event

        { CombinedDistance = (r.EchoDistance + g.WaveAmplitude) / 2.0
          ThreatNearby = r.EchoDistance < 100.0 }

module AtmosphericSensor =
    let analyze
        (pressure: DiagnosticEvent -> PressureReaderResult)
        (temperature: DiagnosticEvent -> TemperatureReaderResult)
        (event: DiagnosticEvent)
        : AtmosphericSensorResult =
        let p = pressure event
        let t = temperature event
        let (Kelvin k) = t.Reading

        { PressureOk = p.Stability = Stable
          TemperatureOk = k < 350.0 }

module SensorArray =
    let sweep
        (proximity: DiagnosticEvent -> ProximitySensorResult)
        (atmosphere: DiagnosticEvent -> AtmosphericSensorResult)
        (event: DiagnosticEvent)
        : SensorArrayResult =
        let p = proximity event
        let a = atmosphere event

        { ProximityAlert = p.ThreatNearby
          AtmosphereAlert = not a.PressureOk || not a.TemperatureOk }
