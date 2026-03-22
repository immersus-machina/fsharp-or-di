namespace ErrorMessages

type SensorId = SensorId of int
type TemperatureReading = { Temperature: float }
type PressureReading = { Pressure: float }
type HumidityReading = { Humidity: float }
type WindReading = { WindSpeed: float }
type CombinedReading = { Temperature: float; Pressure: float }

type RouteAResult = { ViaA: float }
type RouteBResult = { ViaB: float }
type FinalResult = { Value: float }

type StepOneResult = { StepOne: float }
