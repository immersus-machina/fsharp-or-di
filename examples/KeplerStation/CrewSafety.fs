module KeplerStation.CrewSafety

module IntercomRelay =
    let broadcast (event: DiagnosticEvent) : IntercomRelayResult =
        { ChannelsOpen = max 1 (8 - event.AnomalyCode)
          SignalClarity = Percentage (95.0 - float event.AnomalyCode * 5.0) }

module EvacuationRouter =
    let route (event: DiagnosticEvent) : EvacuationRouterResult =
        { RoutesAvailable = max 1 (6 - event.AnomalyCode / 2)
          EstimatedClearTime = Seconds (120.0 + float event.AnomalyCode * 30.0) }

module CrewAlert =
    let alert
        (intercom: DiagnosticEvent -> IntercomRelayResult)
        (evacuation: DiagnosticEvent -> EvacuationRouterResult)
        (lifeSupport: DiagnosticEvent -> LifeSupportControllerResult)
        (event: DiagnosticEvent)
        : CrewAlertResult =
        let i = intercom event
        let e = evacuation event
        let ls = lifeSupport event

        { AlertsSent = i.ChannelsOpen + ls.CriticalAlerts
          EvacReady = e.RoutesAvailable >= 2 && ls.SystemsNominal }
