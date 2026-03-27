module FSharpOrDi.Test.BuildAndResolveIntegrationTests

open Xunit
open FSharpOrDi.FunctionRegistry
open FSharpOrDi.FunctionGraph

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

// ── Build and resolve succeeds ──────────────────────────────────────

[<Fact>]
let ``single registration resolves directly`` () =
    // Arrange
    let readTemperature: int -> TemperatureReading =
        fun sensorId ->
            {
                Temperature = Celsius(float sensorId)
                SensorId = string sensorId
            }

    let graph = register readTemperature |> build

    // Act
    let resolved: int -> TemperatureReading = resolve graph

    // Assert
    let result = resolved 25
    Assert.Equal(Celsius 25.0, result.Temperature)

[<Fact>]
let ``single value registration resolves directly`` () =
    // Arrange
    let graph =
        register
            {
                Temperature = Celsius 42.0
                SensorId = "fixed"
            }
        |> build

    // Act
    let resolved: TemperatureReading = resolve graph

    // Assert
    Assert.Equal(Celsius 42.0, resolved.Temperature)

[<Fact>]
let ``different signatures sharing a parameter type resolve independently`` () =
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
    let graph = register readTemperature >> register readPressure |> build

    // Assert
    let temp: int -> TemperatureReading = resolve graph
    let press: int -> PressureReading = resolve graph
    Assert.Equal(Celsius 25.0, (temp 25).Temperature)
    Assert.Equal(Pascal 2500.0, (press 25).Pressure)

[<Fact>]
let ``single dependency wires by signature`` () =
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

    let graph = register readTemperature >> register assessRisk |> build

    // Act
    let resolved: int -> RiskAssessment = resolve graph

    // Assert
    Assert.Equal(Low, (resolved 25).Level)
    Assert.Equal(High, (resolved 100).Level)

[<Fact>]
let ``multiple dependencies wire by signature`` () =
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

    let graph =
        register readTemperature
        >> register readPressure
        >> register combineSensors
        |> build

    // Act
    let resolved: SensorInput -> CombinedSensorReading = resolve graph

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
let ``nested dependencies wire recursively`` () =
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

    let graph =
        register readTemperature
        >> register readPressure
        >> register combineSensors
        >> register assessRisk
        |> build

    // Act
    let resolved: SensorInput -> RiskAssessment = resolve graph

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
let ``three layers deep with branching wires correctly``
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

    let graph =
        register readTemperature
        >> register readPressure
        >> register readHumidity
        >> register combineSensors
        >> register makeDecision
        |> build

    // Act
    let resolved: SensorInput -> AlertDecision = resolve graph

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
let ``registration order does not affect resolution`` () =
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

    let graph = register assessRisk >> register readTemperature |> build

    // Act
    let resolved: int -> RiskAssessment = resolve graph

    // Assert
    Assert.Equal(Low, (resolved 25).Level)

// ── Build fails ─────────────────────────────────────────────────────

[<Fact>]
let ``duplicate signatures fail at build time`` () =
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

    // Act
    let ex = Assert.Throws<exn>(fun () -> register sensor1 >> register sensor2 |> build |> ignore)

    // Assert
    Assert.Contains("Already registered", ex.Message)

[<Fact>]
let ``lambda and named function with same signature fail at build time`` () =
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

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> register asLambda >> register asNamedFunction |> build |> ignore)

    // Assert
    Assert.Contains("Already registered", ex.Message)

[<Fact>]
let ``multiple partial-application routes to same signature fail at build time`` () =
    // Arrange
    let viaRouteA: (int -> RouteAIntermediate) -> int -> FinalResult =
        fun getA x -> { Value = (getA x).ViaA }

    let viaRouteB: (int -> RouteBIntermediate) -> int -> FinalResult =
        fun getB x -> { Value = (getB x).ViaB }

    let makeA: int -> RouteAIntermediate = fun x -> { ViaA = float x }
    let makeB: int -> RouteBIntermediate = fun x -> { ViaB = float x * 10.0 }

    // Act
    let ex =
        Assert.Throws<exn>(fun () ->
            register makeA >> register makeB >> register viaRouteA >> register viaRouteB
            |> build
            |> ignore)

    // Assert
    Assert.Contains("Ambiguous", ex.Message)

[<Fact>]
let ``direct registration and partial-application route to same signature fail at build time`` () =
    // Arrange
    let direct: int -> FinalResult = fun x -> { Value = float x }

    let viaRoute: (int -> RouteAIntermediate) -> int -> FinalResult =
        fun getA x -> { Value = (getA x).ViaA }

    let makeA: int -> RouteAIntermediate = fun x -> { ViaA = float x * 2.0 }

    // Act
    let ex =
        Assert.Throws<exn>(fun () ->
            register direct >> register makeA >> register viaRoute
            |> build
            |> ignore)

    // Assert
    Assert.Contains("Ambiguous", ex.Message)

[<Fact>]
let ``cycle in dependency graph fails at build time`` () =
    // Arrange
    let produceFinal: (int -> RouteAIntermediate) -> int -> FinalResult =
        fun _getA x -> { Value = float x }

    let produceA: (int -> RouteBIntermediate) -> int -> RouteAIntermediate =
        fun _getB x -> { ViaA = float x }

    let produceB: (int -> RouteAIntermediate) -> int -> RouteBIntermediate =
        fun _getA x -> { ViaB = float x }

    // Act
    let ex =
        Assert.Throws<exn>(fun () ->
            register produceFinal >> register produceA >> register produceB
            |> build
            |> ignore)

    // Assert
    Assert.Contains("Cycle detected", ex.Message)

// ── Resolve fails ───────────────────────────────────────────────────

[<Fact>]
let ``single-arg functions do not chain without buildComposed`` () =
    // Arrange
    let step1: int -> RouteAIntermediate = fun x -> { ViaA = float x }
    let step2: RouteAIntermediate -> FinalResult = fun a -> { Value = a.ViaA * 2.0 }

    let graph = register step1 >> register step2 |> build

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolve<int -> FinalResult> graph |> ignore)

    // Assert
    Assert.Contains("Cannot resolve", ex.Message)

[<Fact>]
let ``unregistered type produces readable error`` () =
    // Arrange
    let graph = build id

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolve<int -> TemperatureReading> graph |> ignore)

    // Assert
    Assert.Contains("Cannot resolve", ex.Message)
    Assert.Contains("TemperatureReading", ex.Message)

[<Fact>]
let ``missing dependency produces readable diagnostic`` () =
    // Arrange
    let assessRisk: (int -> TemperatureReading) -> int -> RiskAssessment =
        fun _readTemp _id -> { Level = Low; Description = "" }

    let graph = register assessRisk |> build

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolve<int -> RiskAssessment> graph |> ignore)

    // Assert
    Assert.Contains("Cannot resolve", ex.Message)
    Assert.Contains("TemperatureReading", ex.Message)

[<Fact>]
let ``empty graph produces readable error`` () =
    // Arrange
    let graph = build id

    // Act
    let ex = Assert.Throws<exn>(fun () -> resolve<TemperatureReading> graph |> ignore)

    // Assert
    Assert.Contains("Cannot resolve", ex.Message)
