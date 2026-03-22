module KeplerStation.Propulsion

module FuelInjector =
    let inject (event: DiagnosticEvent) : FuelInjectorResult =
        { FlowRate = LitersPerMinute (event.SignalStrength * 0.8)
          Purity = Percentage (99.2 - float event.AnomalyCode * 0.3) }

module NozzleController =
    let adjust (event: DiagnosticEvent) : NozzleControllerResult =
        { ApertureAngle = 15.0 + float event.AnomalyCode
          ThrustVector = event.SignalStrength * 0.5 }

module StarChart =
    let query (event: DiagnosticEvent) : StarChartResult =
        { NearestStarDistance = 4.2 + event.SignalStrength * 0.01
          SafeCorridors = max 1 (10 - event.AnomalyCode) }

module GyroscopeArray =
    let read (event: DiagnosticEvent) : GyroscopeArrayResult =
        { RollRate = float event.AnomalyCode * 0.1
          PitchRate = event.SignalStrength * 0.02
          YawRate = 0.05 }

module ThrusterArray =
    let fire
        (fuel: DiagnosticEvent -> FuelInjectorResult)
        (nozzle: DiagnosticEvent -> NozzleControllerResult)
        (energy: DiagnosticEvent -> StabilizedThrustEnergy)
        (event: DiagnosticEvent)
        : ThrusterArrayResult =
        let f = fuel event
        let n = nozzle event
        let e = energy event
        let (LitersPerMinute flow) = f.FlowRate
        let (Percentage efficiency) = e.Efficiency
        let powerFactor = efficiency / 100.0
        let (Percentage purity) = f.Purity

        { Thrust = Newtons (flow * n.ThrustVector * 1000.0 * powerFactor)
          Efficiency = Percentage (purity / 100.0 * 100.0) }

module NavigationComputer =
    let compute
        (chart: DiagnosticEvent -> StarChartResult)
        (gyro: DiagnosticEvent -> GyroscopeArrayResult)
        (event: DiagnosticEvent)
        : NavigationComputerResult =
        let c = chart event
        let g = gyro event

        { Heading = Bearing (180.0 + g.YawRate * 10.0)
          SpeedRecommendation = if c.SafeCorridors > 3 then 0.8 else 0.3 }

module PropulsionControl =
    let engage
        (thrusters: DiagnosticEvent -> ThrusterArrayResult)
        (navigation: DiagnosticEvent -> NavigationComputerResult)
        (event: DiagnosticEvent)
        : PropulsionControlResult =
        let t = thrusters event
        let n = navigation event
        let (Percentage eff) = t.Efficiency

        { EnginesReady = eff > 90.0
          CourseSet = n.SpeedRecommendation > 0.5 }
