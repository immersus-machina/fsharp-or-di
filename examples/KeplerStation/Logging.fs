module KeplerStation.Logging

module EventRecorder =
    let record (event: DiagnosticEvent) : EventRecorderResult =
        { EntriesLogged = 1
          StorageRemaining = Percentage 98.5 }

module TransmissionBuffer =
    let buffer (event: DiagnosticEvent) : TransmissionBufferResult =
        { QueueDepth = event.AnomalyCode
          BandwidthUsed = Percentage (event.SignalStrength * 0.1) }

module StationLog =
    let log
        (recorder: DiagnosticEvent -> EventRecorderResult)
        (transmission: DiagnosticEvent -> TransmissionBufferResult)
        (pulsedEnergy: DiagnosticEvent -> PulsedEnergy)
        (event: DiagnosticEvent)
        : StationLogResult =
        let r = recorder event
        let t = transmission event
        let p = pulsedEnergy event
        let (Percentage bandwidth) = p.DataCapacity

        { Recorded = r.EntriesLogged > 0
          Transmitted = t.QueueDepth < 100 && bandwidth > 50.0 }
