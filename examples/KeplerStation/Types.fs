namespace KeplerStation

open System

// --- Wrapper types for cross-cutting measurements ---

type Megawatts = Megawatts of float
type Percentage = Percentage of float
type Kelvin = Kelvin of float
type Pascals = Pascals of float
type Hertz = Hertz of float
type Newtons = Newtons of float
type Bearing = Bearing of float
type Seconds = Seconds of float
type LitersPerMinute = LitersPerMinute of float

// --- Discriminated unions for classification/status ---

type Stability = Stable | Unstable
type NodeStatus = Online | Degraded | Offline
type HarmonicsStatus = Calibrated | Uncalibrated
type ThreatCategory = Asteroid | Storm | IonideStorm | Unknown
type DangerLevel = Low | Moderate | Critical
type StormSeverity = Mild | Severe
type RiskCategory = Green | Yellow | Red
type AlertLevel = Nominal | Elevated | Maximum

// --- Input ---

type DiagnosticEvent =
    { Timestamp: DateTime
      SectorId: string
      SignalStrength: float
      AnomalyCode: int }

// --- Energy grid ---

type PrimaryReactorResult = { Output: Megawatts; CoreStability: Stability }
type AuxiliaryReactorResult = { Output: Megawatts; FuelRemaining: Percentage }
type SolarCollectorResult = { Harvested: Megawatts; PanelEfficiency: Percentage }
type PowerBusResult = { TotalPower: Megawatts; SourceCount: int }
type LoadBalancerResult = { DistributedPower: Megawatts; Load: Percentage }
type EnergyAllocationResult = { AvailablePower: Megawatts; GridStability: Stability }

// --- Quantum energy pipeline ---

type QuantumFlux = QuantumFlux of float
type RawQuantumEnergy = { Flux: QuantumFlux; SourcePower: Megawatts }

type CoherentEnergy = { CoherenceLevel: Percentage; Frequency: Hertz }
type ChaoticEnergy = { EntropyFactor: float; RawPower: Megawatts }
type SplitQuantumEnergy = { Coherent: CoherentEnergy; Chaotic: ChaoticEnergy }

type HarmonizedEnergy = { HarmonicFrequency: Hertz; ShieldCompatibility: Percentage }
type CalibratedShieldEnergy = { CalibratedFrequency: Hertz; Stability: Percentage }
type FocusedEnergy = { FocusIntensity: Percentage; SignalBoost: float }
type ContainmentEnergy = { SealPower: Megawatts; FieldStrength: Percentage }
type AmplifiedEnergy = { ThrustOutput: Megawatts; AmplificationFactor: float }
type StabilizedThrustEnergy = { StableThrust: Megawatts; Efficiency: Percentage }
type ThermalEnergy = { HeatOutput: Kelvin; Regulation: Percentage }
type PulsedEnergy = { PulseFrequency: Hertz; DataCapacity: Percentage }

// --- Sensor leaves ---

type RadarPulseResult = { EchoDistance: float; EchoCount: int }
type GravityWaveResult = { WaveAmplitude: float; SourceBearing: Bearing }
type PressureReaderResult = { Reading: Pascals; Stability: Stability }
type TemperatureReaderResult = { Reading: Kelvin; DeltaRate: float }

// --- Sensor composites ---

type ProximitySensorResult = { CombinedDistance: float; ThreatNearby: bool }
type AtmosphericSensorResult = { PressureOk: bool; TemperatureOk: bool }
type SensorArrayResult = { ProximityAlert: bool; AtmosphereAlert: bool }

// --- Threat leaves ---

type TrajectoryPlotterResult = { ImpactProbability: Percentage; EstimatedEta: Seconds }
type MassEstimatorResult = { KilogramsEstimate: float; Confidence: Percentage }
type IonFluxReaderResult = { FluxDensity: float; IsElevated: bool }
type MagneticFieldReaderResult = { TeslaReading: float; Polarity: int }

// --- Threat composites ---

type AsteroidAnalyzerResult = { Danger: DangerLevel; ImpactWindow: Seconds }
type StormAnalyzerResult = { Severity: StormSeverity; ShieldRecommendation: AlertLevel }
type ThreatClassifierResult = { PrimaryThreat: ThreatCategory; SecondaryThreat: ThreatCategory }
type RiskScorerResult = { OverallRisk: float; Category: RiskCategory }
type ThreatAssessmentResult = { Level: AlertLevel; Summary: string }

// --- Emergency threat/trajectory ---

type BlindTrajectoryResult = { AssumedImpactProbability: Percentage; WorstCaseEta: Seconds }
type EmergencyThreatResult = { AssumedThreat: ThreatCategory; AssumedSeverity: AlertLevel }

// --- Shield leaves ---

type GridNodeResult = { Status: NodeStatus; LoadFactor: float }
type HarmonicCalibratorResult = { Frequency: Hertz; Harmonics: HarmonicsStatus }
type ManualDeflectorResult = { FixedCoverage: Percentage }

// --- Shield composites ---

type PowerRegulatorResult = { AllocatedPower: Megawatts; Stability: Stability }
type DeflectorGridResult = { Coverage: Percentage; Harmonics: HarmonicsStatus }
type ShieldControlResult = { ShieldsUp: bool; PowerDraw: Megawatts }

// --- Propulsion leaves ---

type FuelInjectorResult = { FlowRate: LitersPerMinute; Purity: Percentage }
type NozzleControllerResult = { ApertureAngle: float; ThrustVector: float }
type StarChartResult = { NearestStarDistance: float; SafeCorridors: int }
type GyroscopeArrayResult = { RollRate: float; PitchRate: float; YawRate: float }

// --- Propulsion composites ---

type ThrusterArrayResult = { Thrust: Newtons; Efficiency: Percentage }
type NavigationComputerResult = { Heading: Bearing; SpeedRecommendation: float }
type PropulsionControlResult = { EnginesReady: bool; CourseSet: bool }

// --- Life support leaves ---

type OxygenRecyclerResult = { FlowRate: LitersPerMinute; Purity: Percentage }
type CO2ScrubberResult = { PartsPerMillion: float; FilterHealth: Percentage }
type WaterReclaimerResult = { Reclaimed: LitersPerMinute; ContaminantLevel: float }

// --- Life support composites ---

type AtmosphereProcessorResult = { AirQualityIndex: float; PowerDraw: Megawatts }
type LifeSupportControllerResult = { SystemsNominal: bool; CriticalAlerts: int }

// --- Containment leaves ---

type HullIntegrityResult = { Integrity: Percentage; BreachCount: int }
type BlastDoorResult = { DoorsSealed: int; DoorsTotal: int }

// --- Containment composites ---

type SealManagerResult = { ContainmentLevel: Percentage; PowerDraw: Megawatts }
type ContainmentStatusResult = { IsSecure: bool; CompromisedSectors: int }

// --- Crew safety ---

type IntercomRelayResult = { ChannelsOpen: int; SignalClarity: Percentage }
type EvacuationRouterResult = { RoutesAvailable: int; EstimatedClearTime: Seconds }
type CrewAlertResult = { AlertsSent: int; EvacReady: bool }

// --- Logging ---

type EventRecorderResult = { EntriesLogged: int; StorageRemaining: Percentage }
type TransmissionBufferResult = { QueueDepth: int; BandwidthUsed: Percentage }
type StationLogResult = { Recorded: bool; Transmitted: bool }

// --- Top level ---

type ResponseCoordinatorResult = { DefensesActive: bool; CourseAdjusted: bool; CrewNotified: bool; Contained: bool }
type StationCommandResult = { Decision: string; ThreatLevel: AlertLevel; ActionsExecuted: int }
