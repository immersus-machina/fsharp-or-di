module KeplerStation.EnergyGrid

module PrimaryReactor =
    let output (event: DiagnosticEvent) : PrimaryReactorResult =
        { Output = Megawatts (event.SignalStrength * 10.0)
          CoreStability = if event.AnomalyCode < 5 then Stable else Unstable }

module AuxiliaryReactor =
    let output (event: DiagnosticEvent) : AuxiliaryReactorResult =
        { Output = Megawatts (event.SignalStrength * 3.0)
          FuelRemaining = Percentage (75.0 - float event.AnomalyCode * 2.0) }

module SolarCollector =
    let harvest (event: DiagnosticEvent) : SolarCollectorResult =
        { Harvested = Megawatts (event.SignalStrength * 0.5)
          PanelEfficiency = Percentage 82.0 }

module PowerBus =
    let distribute
        (primary: DiagnosticEvent -> PrimaryReactorResult)
        (auxiliary: DiagnosticEvent -> AuxiliaryReactorResult)
        (solar: DiagnosticEvent -> SolarCollectorResult)
        (event: DiagnosticEvent)
        : PowerBusResult =
        let p = primary event
        let a = auxiliary event
        let s = solar event
        let (Megawatts pmw) = p.Output
        let (Megawatts amw) = a.Output
        let (Megawatts smw) = s.Harvested

        { TotalPower = Megawatts (pmw + amw + smw)
          SourceCount = 3 }

module LoadBalancer =
    let balance
        (bus: DiagnosticEvent -> PowerBusResult)
        (event: DiagnosticEvent)
        : LoadBalancerResult =
        let b = bus event
        let (Megawatts total) = b.TotalPower

        { DistributedPower = Megawatts (total * 0.92)
          Load = Percentage (min 100.0 (total / 1200.0 * 100.0)) }

module EnergyAllocation =
    let allocate
        (balancer: DiagnosticEvent -> LoadBalancerResult)
        (event: DiagnosticEvent)
        : EnergyAllocationResult =
        let lb = balancer event
        let (Percentage load) = lb.Load

        { AvailablePower = lb.DistributedPower
          GridStability = if load < 85.0 then Stable else Unstable }

module QuantumEnergyExtractor =
    let extract
        (powerBus: DiagnosticEvent -> PowerBusResult)
        (event: DiagnosticEvent)
        : RawQuantumEnergy =
        let bus = powerBus event
        let (Megawatts totalPower) = bus.TotalPower
        { Flux = QuantumFlux (totalPower * 0.001)
          SourcePower = bus.TotalPower }
