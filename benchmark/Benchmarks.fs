open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
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

let registrations () =
    register PrimaryReactor.output
    >> register AuxiliaryReactor.output
    >> register SolarCollector.harvest
    >> register PowerBus.distribute
    >> register LoadBalancer.balance
    >> register EnergyAllocation.allocate
    >> register QuantumEnergyExtractor.extract
    >> register QuantumSplitter.split
    >> register CoherentExtractor.extract
    >> register ChaoticExtractor.extract
    >> register HarmonicTuner.tune
    >> register ShieldCalibrator.calibrate
    >> register SensorFocuser.focus
    >> register ContainmentSealer.seal
    >> register ThrustAmplifier.amplify
    >> register ThrustStabilizer.stabilize
    >> register ThermalConverter.convert
    >> register TransmissionPulser.pulse
    >> register RadarPulse.scan
    >> register GravityWave.detect
    >> register ProximitySensor.analyze
    >> register PressureReader.read
    >> register TemperatureReader.read
    >> register AtmosphericSensor.analyze
    >> register SensorArray.sweep
    >> register TrajectoryPlotter.plot
    >> register BlindTrajectoryPlotter.plot
    >> register MassEstimator.estimate
    >> register AsteroidAnalyzer.analyze
    >> register IonFluxReader.read
    >> register MagneticFieldReader.read
    >> register StormAnalyzer.analyze
    >> register ThreatClassifier.classify
    >> register EmergencyThreatClassifier.classify
    >> register HullIntegrityMonitor.scan
    >> register BlastDoorController.status
    >> register SealManager.manage
    >> register ContainmentStatus.assess
    >> register RiskScorer.score
    >> register ThreatAssessment.assess
    >> register PowerRegulator.regulate
    >> register GridNode.report
    >> register HarmonicCalibrator.calibrate
    >> register DeflectorGrid.activate
    >> register ManualDeflectorGrid.activate
    >> register ShieldControl.engage
    >> register FuelInjector.inject
    >> register NozzleController.adjust
    >> register ThrusterArray.fire
    >> register StarChart.query
    >> register GyroscopeArray.read
    >> register NavigationComputer.compute
    >> register PropulsionControl.engage
    >> register OxygenRecycler.recycle
    >> register CO2Scrubber.scrub
    >> register WaterReclaimer.reclaim
    >> register AtmosphereProcessor.regulate
    >> register LifeSupportController.control
    >> register IntercomRelay.broadcast
    >> register EvacuationRouter.route
    >> register CrewAlert.alert
    >> register ResponseCoordinator.coordinate
    >> register EventRecorder.record
    >> register TransmissionBuffer.buffer
    >> register StationLog.log
    >> register StationCommand.execute

[<MemoryDiagnoser>]
type KeplerStationBenchmarks() =

    [<Benchmark(Description = "buildComposed (66 functions)")>]
    member _.BuildComposed() =
        buildComposed (registrations())

BenchmarkRunner.Run<KeplerStationBenchmarks>() |> ignore
