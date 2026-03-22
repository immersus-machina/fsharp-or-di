module KeplerStation.LifeSupport

module OxygenRecycler =
    let recycle (event: DiagnosticEvent) : OxygenRecyclerResult =
        { FlowRate = LitersPerMinute (320.0 - float event.AnomalyCode * 10.0)
          Purity = Percentage (99.5 - float event.AnomalyCode * 0.2) }

module CO2Scrubber =
    let scrub (event: DiagnosticEvent) : CO2ScrubberResult =
        { PartsPerMillion = 400.0 + float event.AnomalyCode * 50.0
          FilterHealth = Percentage (90.0 - float event.AnomalyCode * 5.0) }

module WaterReclaimer =
    let reclaim (event: DiagnosticEvent) : WaterReclaimerResult =
        { Reclaimed = LitersPerMinute (150.0 - float event.AnomalyCode * 5.0)
          ContaminantLevel = float event.AnomalyCode * 0.01 }

module AtmosphereProcessor =
    let regulate
        (oxygen: DiagnosticEvent -> OxygenRecyclerResult)
        (co2: DiagnosticEvent -> CO2ScrubberResult)
        (energy: DiagnosticEvent -> ThermalEnergy)
        (event: DiagnosticEvent)
        : AtmosphereProcessorResult =
        let o = oxygen event
        let c = co2 event
        let e = energy event
        let (Percentage purity) = o.Purity
        let (Percentage regulation) = e.Regulation
        let qualityFactor = regulation / 100.0
        let quality = purity - (c.PartsPerMillion / 1000.0)

        { AirQualityIndex = quality * qualityFactor
          PowerDraw = Megawatts 45.0 }

module LifeSupportController =
    let control
        (atmosphere: DiagnosticEvent -> AtmosphereProcessorResult)
        (water: DiagnosticEvent -> WaterReclaimerResult)
        (event: DiagnosticEvent)
        : LifeSupportControllerResult =
        let a = atmosphere event
        let w = water event
        let alerts =
            (if a.AirQualityIndex < 90.0 then 1 else 0)
            + (if w.ContaminantLevel > 0.05 then 1 else 0)

        { SystemsNominal = alerts = 0
          CriticalAlerts = alerts }
