module FSharpOrDi.Test.ResolveIntegrationTests

open Xunit
open FSharpOrDi.FunctionRegistry

type Celsius = Celsius of float
type Pascal = Pascal of float
type RelativeHumidity = RelativeHumidity of float

type TemperatureReading =
    {
        Temperature: Celsius
        SensorId: string
    }

type PressureReading = { Pressure: Pascal; SensorId: string }

type HumidityReading =
    {
        Humidity: RelativeHumidity
        SensorId: string
    }

type CombinedSensorReading =
    {
        Temperature: Celsius
        Pressure: Pascal
    }

type RiskLevel =
    | Low
    | Medium
    | High

type RiskAssessment =
    {
        Level: RiskLevel
        Description: string
    }

type AlertPriority =
    | Routine
    | Urgent
    | Critical

type AlertDecision =
    {
        Priority: AlertPriority
        Message: string
    }

type SensorInput =
    {
        TemperatureC: float
        PressureHPa: float
        HumidityPct: float
    }

type RouteAIntermediate = { ViaA: float }
type RouteBIntermediate = { ViaB: float }
type FinalResult = { Value: float }

[<Fact>]
let ``resolve returns directly registered function`` () =
    // Arrange
    let readTemperature: int -> TemperatureReading =
        fun sensorId ->
            {
                Temperature = Celsius(float sensorId)
                SensorId = string sensorId
            }

    let registry = empty |> register readTemperature

    // Act
    let resolved: int -> TemperatureReading = resolve registry

    // Assert
    let result = resolved 25
    Assert.Equal(Celsius 25.0, result.Temperature)

[<Fact>]
let ``resolve returns directly registered value`` () =
    // Arrange
    let registry =
        empty
        |> register
            {
                Temperature = Celsius 42.0
                SensorId = "fixed"
            }

    // Act
    let resolved: TemperatureReading = resolve registry

    // Assert
    Assert.Equal(Celsius 42.0, resolved.Temperature)

[<Fact>]
let ``register fails on duplicate signature`` () =
    // Arrange
    let sensor1: int -> TemperatureReading =
        fun id ->
            {
                Temperature = Celsius 20.0
                SensorId = string id
            }

    let sensor2: int -> TemperatureReading =
        fun id ->
            {
                Temperature = Celsius 25.0
                SensorId = string id
            }

    let registry = empty |> register sensor1

    // Act
    let ex = Assert.Throws<exn>(fun () -> registry |> register sensor2 |> ignore)

    // Assert
    Assert.Contains("Already registered", ex.Message)

[<Fact>]
let ``register fails when lambda and named function share the same signature`` () =
    // Arrange
    let asLambda: int -> TemperatureReading =
        fun id ->
            {
                Temperature = Celsius(float id)
                SensorId = string id
            }

    let asNamedFunction (id: int) : TemperatureReading =
        {
            Temperature = Celsius(float id * 2.0)
            SensorId = string id
        }

    let registry = empty |> register asLambda

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> registry |> register asNamedFunction |> ignore)

    // Assert
    Assert.Contains("Already registered", ex.Message)

[<Fact>]
let ``register allows different signatures that share a parameter type`` () =
    // Arrange
    let readTemperature: int -> TemperatureReading =
        fun id ->
            {
                Temperature = Celsius(float id)
                SensorId = string id
            }

    let readPressure: int -> PressureReading =
        fun id ->
            {
                Pressure = Pascal(float id * 100.0)
                SensorId = string id
            }

    // Act
    let registry = empty |> register readTemperature |> register readPressure

    // Assert
    let temp: int -> TemperatureReading = resolve registry
    let press: int -> PressureReading = resolve registry
    Assert.Equal(Celsius 25.0, (temp 25).Temperature)
    Assert.Equal(Pascal 2500.0, (press 25).Pressure)

[<Fact>]
let ``resolve wires single dependency by signature`` () =
    // Arrange
    let readTemperature: int -> TemperatureReading =
        fun id ->
            {
                Temperature = Celsius(float id)
                SensorId = string id
            }

    let assessRisk: (int -> TemperatureReading) -> int -> RiskAssessment =
        fun readTemp id ->
            let reading = readTemp id
            let (Celsius c) = reading.Temperature

            {
                Level = if c > 50.0 then High else Low
                Description = sprintf "Sensor %s: %.1f°C" reading.SensorId c
            }

    let registry = empty |> register readTemperature |> register assessRisk

    // Act
    let resolved: int -> RiskAssessment = resolve registry

    // Assert
    Assert.Equal(Low, (resolved 25).Level)
    Assert.Equal(High, (resolved 100).Level)

[<Fact>]
let ``resolve wires multiple dependencies by signature`` () =
    // Arrange
    let readTemperature: SensorInput -> TemperatureReading =
        fun input ->
            {
                Temperature = Celsius input.TemperatureC
                SensorId = "T1"
            }

    let readPressure: SensorInput -> PressureReading =
        fun input ->
            {
                Pressure = Pascal(input.PressureHPa * 100.0)
                SensorId = "P1"
            }

    let combineSensors
        : (SensorInput -> TemperatureReading)
              -> (SensorInput -> PressureReading)
              -> SensorInput
              -> CombinedSensorReading =
        fun readTemp readPress input ->
            {
                Temperature = (readTemp input).Temperature
                Pressure = (readPress input).Pressure
            }

    let registry =
        empty
        |> register readTemperature
        |> register readPressure
        |> register combineSensors

    // Act
    let resolved: SensorInput -> CombinedSensorReading = resolve registry

    // Assert
    let result =
        resolved
            {
                TemperatureC = 22.5
                PressureHPa = 1013.25
                HumidityPct = 0.0
            }

    Assert.Equal(Celsius 22.5, result.Temperature)
    Assert.Equal(Pascal 101325.0, result.Pressure)

[<Fact>]
let ``resolve recursively wires nested dependencies`` () =
    // Arrange
    let readTemperature: SensorInput -> TemperatureReading =
        fun input ->
            {
                Temperature = Celsius input.TemperatureC
                SensorId = "T1"
            }

    let readPressure: SensorInput -> PressureReading =
        fun input ->
            {
                Pressure = Pascal(input.PressureHPa * 100.0)
                SensorId = "P1"
            }

    let combineSensors
        : (SensorInput -> TemperatureReading)
              -> (SensorInput -> PressureReading)
              -> SensorInput
              -> CombinedSensorReading =
        fun readTemp readPress input ->
            {
                Temperature = (readTemp input).Temperature
                Pressure = (readPress input).Pressure
            }

    let assessRisk: (SensorInput -> CombinedSensorReading) -> SensorInput -> RiskAssessment =
        fun readCombined input ->
            let s = readCombined input
            let (Celsius c) = s.Temperature
            let (Pascal p) = s.Pressure

            {
                Level = if c > 50.0 || p > 5000.0 then High else Low
                Description = sprintf "%.1f°C, %.0f Pa" c p
            }

    let registry =
        empty
        |> register readTemperature
        |> register readPressure
        |> register combineSensors
        |> register assessRisk

    // Act
    let resolved: SensorInput -> RiskAssessment = resolve registry

    // Assert
    Assert.Equal(
        Low,
        (resolved
            {
                TemperatureC = 20.0
                PressureHPa = 10.0
                HumidityPct = 0.0
            })
            .Level
    )

    Assert.Equal(
        High,
        (resolved
            {
                TemperatureC = 55.0
                PressureHPa = 10.0
                HumidityPct = 0.0
            })
            .Level
    )

    Assert.Equal(
        High,
        (resolved
            {
                TemperatureC = 20.0
                PressureHPa = 60.0
                HumidityPct = 0.0
            })
            .Level
    )

[<Theory>]
[<InlineData(20.0, 10.0, 10.0, "Routine")>] // all calm
[<InlineData(55.0, 10.0, 10.0, "Critical")>] // temperature breach
[<InlineData(30.0, 10.0, 55.0, "Urgent")>] // humidity high, temperature safe
[<InlineData(80.0, 50.0, 90.0, "Critical")>] // everything high — temperature dominates
[<InlineData(50.0, 10.0, 40.0, "Routine")>] // exactly at thresholds — not exceeded
let ``resolve wires three layers deep with branching``
    (tempC: float, pressHPa: float, humPct: float, expectedPriority: string)
    =
    // Arrange
    let readTemperature: SensorInput -> TemperatureReading =
        fun input ->
            {
                Temperature = Celsius input.TemperatureC
                SensorId = "T1"
            }

    let readPressure: SensorInput -> PressureReading =
        fun input ->
            {
                Pressure = Pascal(input.PressureHPa * 100.0)
                SensorId = "P1"
            }

    let readHumidity: SensorInput -> HumidityReading =
        fun input ->
            {
                Humidity = RelativeHumidity input.HumidityPct
                SensorId = "H1"
            }

    let combineSensors
        : (SensorInput -> TemperatureReading)
              -> (SensorInput -> PressureReading)
              -> SensorInput
              -> CombinedSensorReading =
        fun readTemp readPress input ->
            {
                Temperature = (readTemp input).Temperature
                Pressure = (readPress input).Pressure
            }

    let makeDecision
        : (SensorInput -> CombinedSensorReading) -> (SensorInput -> HumidityReading) -> SensorInput -> AlertDecision =
        fun readCombined readHumid input ->
            let s = readCombined input
            let h = readHumid input
            let (Celsius c) = s.Temperature
            let (RelativeHumidity rh) = h.Humidity

            {
                Priority =
                    if c > 50.0 then Critical
                    elif rh > 40.0 then Urgent
                    else Routine
                Message = sprintf "Temp=%.0f°C Humidity=%.0f%%" c rh
            }

    let registry =
        empty
        |> register readTemperature
        |> register readPressure
        |> register readHumidity
        |> register combineSensors
        |> register makeDecision

    // Act
    let resolved: SensorInput -> AlertDecision = resolve registry

    // Assert
    let result =
        resolved
            {
                TemperatureC = tempC
                PressureHPa = pressHPa
                HumidityPct = humPct
            }

    let expected =
        match expectedPriority with
        | "Critical" -> Critical
        | "Urgent" -> Urgent
        | _ -> Routine

    Assert.Equal(expected, result.Priority)

[<Fact>]
let ``resolve does not chain single-arg functions even when a path exists`` () =
    // Arrange
    let step1: int -> RouteAIntermediate = fun x -> { ViaA = float x }
    let step2: RouteAIntermediate -> FinalResult = fun a -> { Value = a.ViaA * 2.0 }

    let registry = empty |> register step1 |> register step2

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolve<int -> FinalResult> registry |> ignore)

    // Assert
    Assert.Contains("Cannot resolve", ex.Message)

[<Fact>]
let ``resolve fails with readable message for unregistered type`` () =
    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolve<int -> TemperatureReading> empty |> ignore)

    // Assert
    Assert.Contains("Cannot resolve", ex.Message)
    Assert.Contains("TemperatureReading", ex.Message)

[<Fact>]
let ``resolve fails with readable message for missing dependency`` () =
    // Arrange
    let assessRisk: (int -> TemperatureReading) -> int -> RiskAssessment =
        fun _readTemp _id -> { Level = Low; Description = "" }

    let registry = empty |> register assessRisk

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolve<int -> RiskAssessment> registry |> ignore)

    // Assert
    Assert.Contains("Cannot resolve", ex.Message)
    Assert.Contains("TemperatureReading", ex.Message)

[<Fact>]
let ``resolve fails when multiple partial-application routes produce the same signature`` () =
    // Arrange
    let viaRouteA: (int -> RouteAIntermediate) -> int -> FinalResult =
        fun getA x -> { Value = (getA x).ViaA }

    let viaRouteB: (int -> RouteBIntermediate) -> int -> FinalResult =
        fun getB x -> { Value = (getB x).ViaB }

    let makeA: int -> RouteAIntermediate = fun x -> { ViaA = float x }
    let makeB: int -> RouteBIntermediate = fun x -> { ViaB = float x * 10.0 }

    let registry =
        empty
        |> register makeA
        |> register makeB
        |> register viaRouteA
        |> register viaRouteB

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolve<int -> FinalResult> registry |> ignore)

    // Assert
    Assert.Contains("Ambiguous", ex.Message)

[<Fact>]
let ``resolve fails when direct registration and partial-application route both produce the same signature`` () =
    // Arrange
    let direct: int -> FinalResult = fun x -> { Value = float x }

    let viaRoute: (int -> RouteAIntermediate) -> int -> FinalResult =
        fun getA x -> { Value = (getA x).ViaA }

    let makeA: int -> RouteAIntermediate = fun x -> { ViaA = float x * 2.0 }

    let registry = empty |> register direct |> register makeA |> register viaRoute

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolve<int -> FinalResult> registry |> ignore)

    // Assert
    Assert.Contains("Ambiguous", ex.Message)

[<Fact>]
let ``resolve detects cycle buried in a deeper dependency graph`` () =
    // Arrange
    let produceFinal: (int -> RouteAIntermediate) -> int -> FinalResult =
        fun _getA x -> { Value = float x }

    let produceA: (int -> RouteBIntermediate) -> int -> RouteAIntermediate =
        fun _getB x -> { ViaA = float x }

    let produceB: (int -> RouteAIntermediate) -> int -> RouteBIntermediate =
        fun _getA x -> { ViaB = float x }

    let registry =
        empty |> register produceFinal |> register produceA |> register produceB

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolve<int -> FinalResult> registry |> ignore)

    // Assert
    Assert.Contains("Cycle detected", ex.Message)

[<Fact>]
let ``resolve fails with readable message for empty registry`` () =
    // Act
    let ex = Assert.Throws<exn>(fun () -> resolve<TemperatureReading> empty |> ignore)

    // Assert
    Assert.Contains("Cannot resolve", ex.Message)

[<Fact>]
let ``resolve works regardless of registration order`` () =
    // Arrange
    let readTemperature: int -> TemperatureReading =
        fun id ->
            {
                Temperature = Celsius(float id)
                SensorId = string id
            }

    let assessRisk: (int -> TemperatureReading) -> int -> RiskAssessment =
        fun readTemp id ->
            let (Celsius c) = (readTemp id).Temperature

            {
                Level = (if c > 50.0 then High else Low)
                Description = ""
            }

    let registry = empty |> register assessRisk |> register readTemperature

    // Act
    let resolved: int -> RiskAssessment = resolve registry

    // Assert
    Assert.Equal(Low, (resolved 25).Level)
