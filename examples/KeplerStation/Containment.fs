module KeplerStation.Containment

module HullIntegrityMonitor =
    let scan (event: DiagnosticEvent) : HullIntegrityResult =
        { Integrity = Percentage (100.0 - float event.AnomalyCode * 3.0)
          BreachCount = event.AnomalyCode / 3 }

module BlastDoorController =
    let status (event: DiagnosticEvent) : BlastDoorResult =
        { DoorsSealed = min 24 (24 - event.AnomalyCode)
          DoorsTotal = 24 }

module SealManager =
    let manage
        (hull: DiagnosticEvent -> HullIntegrityResult)
        (doors: DiagnosticEvent -> BlastDoorResult)
        (energy: DiagnosticEvent -> ContainmentEnergy)
        (event: DiagnosticEvent)
        : SealManagerResult =
        let h = hull event
        let d = doors event
        let e = energy event
        let (Percentage integrity) = h.Integrity
        let (Percentage fieldStrength) = e.FieldStrength
        let sealLevel = (integrity / 100.0) * (float d.DoorsSealed / float d.DoorsTotal)
        let level = sealLevel * fieldStrength

        { ContainmentLevel = Percentage level
          PowerDraw = e.SealPower }

module ContainmentStatus =
    let assess
        (seals: DiagnosticEvent -> SealManagerResult)
        (sensors: DiagnosticEvent -> SensorArrayResult)
        (event: DiagnosticEvent)
        : ContainmentStatusResult =
        let s = seals event
        let sa = sensors event
        let (Percentage level) = s.ContainmentLevel
        let compromised = (if sa.ProximityAlert then 1 else 0) + (if level < 80.0 then 1 else 0)

        { IsSecure = level >= 80.0 && not sa.ProximityAlert
          CompromisedSectors = compromised }
