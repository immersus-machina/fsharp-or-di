open KeplerStation
open KeplerStation.EnergyGrid
open KeplerStation.QuantumPipeline
open KeplerStation.Sensors
open KeplerStation.Threats
open KeplerStation.Emergency
open KeplerStation.Shields
open KeplerStation.Propulsion
open KeplerStation.LifeSupport
open KeplerStation.Containment
open KeplerStation.CrewSafety
open KeplerStation.Logging
open KeplerStation.Response
open KeplerStation.Command
open FSharpOrDi.FunctionRegistry

let registry =
    empty
    // Energy grid
    |> register PrimaryReactor.output
    |> register AuxiliaryReactor.output
    |> register SolarCollector.harvest
    |> register PowerBus.distribute
    |> register LoadBalancer.balance
    |> register EnergyAllocation.allocate
    // Quantum energy pipeline
    |> register QuantumEnergyExtractor.extract
    |> register QuantumSplitter.split
    |> register CoherentExtractor.extract
    |> register ChaoticExtractor.extract
    |> register HarmonicTuner.tune
    |> register ShieldCalibrator.calibrate
    |> register SensorFocuser.focus
    |> register ContainmentSealer.seal
    |> register ThrustAmplifier.amplify
    |> register ThrustStabilizer.stabilize
    |> register ThermalConverter.convert
    |> register TransmissionPulser.pulse
    // Sensors
    |> register RadarPulse.scan
    |> register GravityWave.detect
    |> register ProximitySensor.analyze
    |> register PressureReader.read
    |> register TemperatureReader.read
    |> register AtmosphericSensor.analyze
    |> register SensorArray.sweep
    // Threats
    |> register TrajectoryPlotter.plot
    |> register BlindTrajectoryPlotter.plot
    |> register MassEstimator.estimate
    |> register AsteroidAnalyzer.analyze
    |> register IonFluxReader.read
    |> register MagneticFieldReader.read
    |> register StormAnalyzer.analyze
    |> register ThreatClassifier.classify
    |> register EmergencyThreatClassifier.classify
    // Containment
    |> register HullIntegrityMonitor.scan
    |> register BlastDoorController.status
    |> register SealManager.manage
    |> register ContainmentStatus.assess
    // Risk & assessment
    |> register RiskScorer.score
    |> register ThreatAssessment.assess
    // Shields
    |> register PowerRegulator.regulate
    |> register GridNode.report
    |> register HarmonicCalibrator.calibrate
    |> register DeflectorGrid.activate
    |> register ManualDeflectorGrid.activate
    |> register ShieldControl.engage
    // Propulsion
    |> register FuelInjector.inject
    |> register NozzleController.adjust
    |> register ThrusterArray.fire
    |> register StarChart.query
    |> register GyroscopeArray.read
    |> register NavigationComputer.compute
    |> register PropulsionControl.engage
    // Life support
    |> register OxygenRecycler.recycle
    |> register CO2Scrubber.scrub
    |> register WaterReclaimer.reclaim
    |> register AtmosphereProcessor.regulate
    |> register LifeSupportController.control
    // Crew safety
    |> register IntercomRelay.broadcast
    |> register EvacuationRouter.route
    |> register CrewAlert.alert
    // Response
    |> register ResponseCoordinator.coordinate
    // Logging
    |> register EventRecorder.record
    |> register TransmissionBuffer.buffer
    |> register StationLog.log
    // Command
    |> register StationCommand.execute

printfn "Kepler Station — Functional DI Example"
printfn "========================================="
printfn ""
printfn "Registered 66 functions"

// This single line resolves the entire dependency graph:
// - Partial application wires multi-arg functions with their dependencies
// - Composition chains single-arg quantum energy transforms automatically
//   e.g. QuantumEnergyExtractor >> QuantumSplitter.split >> CoherentExtractor.extract
//        >> HarmonicTuner.tune >> ShieldCalibrator.calibrate = DiagnosticEvent -> CalibratedShieldEnergy
let command: DiagnosticEvent -> StationCommandResult = resolveComposed registry

printfn "Resolved: DiagnosticEvent -> StationCommandResult"
printfn ""

let event =
    { Timestamp = System.DateTime.UtcNow
      SectorId = "7G"
      SignalStrength = 85.0
      AnomalyCode = 3 }

let result = command event
printfn "Station command result:"
printfn "  Decision: %s" result.Decision
printfn "  Threat level: %A" result.ThreatLevel
printfn "  Actions executed: %d" result.ActionsExecuted
