module FSharpOrDi.Test.BuildComposedAndResolveIntegrationTests

open Xunit
open FSharpOrDi.FunctionRegistry
open FSharpOrDi.FunctionGraph

type Volts = Volts of float
type RawSignal = { Reading: Volts; Timestamp: int }

type FilteredSignal =
    {
        CleanedReading: Volts
        NoiseRemoved: bool
    }

type NormalizedSignal =
    {
        NormalizedValue: float
        WithinRange: bool
    }

type SignalCategory =
    | Weak
    | Normal
    | Strong

type ClassifiedSignal =
    {
        Category: SignalCategory
        Confidence: float
    }

type RiskLevel =
    | Low
    | High

type RiskAssessment =
    {
        Level: RiskLevel
        Description: string
    }

type Celsius = Celsius of float

type TemperatureReading =
    {
        Temperature: Celsius
        SensorId: string
    }

type CalibrationFactor = { Factor: float; Offset: float }

type PathAIntermediate = { ViaA: float }
type PathBIntermediate = { ViaB: float }
type FinalOutput = { Value: float }

type DecodedSignal = { DecodedValue: float }
type SignalReport = { Summary: string }

// ── Build and resolve succeeds ──────────────────────────────────────

[<Fact>]
let ``two single-arg functions chain automatically`` () =
    // Arrange
    let filter: RawSignal -> FilteredSignal =
        fun raw ->
            let (Volts v) = raw.Reading

            {
                CleanedReading = Volts(v * 0.9)
                NoiseRemoved = true
            }

    let normalize: FilteredSignal -> NormalizedSignal =
        fun filtered ->
            let (Volts v) = filtered.CleanedReading

            {
                NormalizedValue = v / 100.0
                WithinRange = v < 100.0
            }

    let graph = register filter >> register normalize |> buildComposed

    // Act
    let pipeline: RawSignal -> NormalizedSignal = resolve graph

    // Assert
    let result = pipeline { Reading = Volts 50.0; Timestamp = 1 }
    Assert.Equal(0.45, result.NormalizedValue)
    Assert.True(result.WithinRange)

[<Fact>]
let ``three single-arg functions chain automatically`` () =
    // Arrange
    let filter: RawSignal -> FilteredSignal =
        fun raw ->
            let (Volts v) = raw.Reading

            {
                CleanedReading = Volts(v * 0.9)
                NoiseRemoved = true
            }

    let normalize: FilteredSignal -> NormalizedSignal =
        fun filtered ->
            let (Volts v) = filtered.CleanedReading

            {
                NormalizedValue = v / 100.0
                WithinRange = v < 100.0
            }

    let classify: NormalizedSignal -> ClassifiedSignal =
        fun normalized ->
            let category =
                if normalized.NormalizedValue > 0.7 then Strong
                elif normalized.NormalizedValue > 0.3 then Normal
                else Weak

            {
                Category = category
                Confidence = 0.95
            }

    let graph = register filter >> register normalize >> register classify |> buildComposed

    // Act
    let pipeline: RawSignal -> ClassifiedSignal = resolve graph

    // Assert
    let result = pipeline { Reading = Volts 80.0; Timestamp = 1 }
    Assert.Equal(Strong, result.Category)

[<Fact>]
let ``associative composition does not report ambiguity`` () =
    // Arrange
    let stepOne: RawSignal -> FilteredSignal =
        fun raw ->
            let (Volts v) = raw.Reading

            {
                CleanedReading = Volts(v * 0.9)
                NoiseRemoved = true
            }

    let stepTwo: FilteredSignal -> NormalizedSignal =
        fun filtered ->
            let (Volts v) = filtered.CleanedReading

            {
                NormalizedValue = v / 100.0
                WithinRange = v < 100.0
            }

    let stepThree: NormalizedSignal -> SignalCategory =
        fun normalized -> if normalized.NormalizedValue > 0.5 then Strong else Weak

    let graph = register stepOne >> register stepTwo >> register stepThree |> buildComposed

    // Act
    let pipeline: RawSignal -> SignalCategory = resolve graph

    // Assert
    let result = pipeline { Reading = Volts 80.0; Timestamp = 1 }
    Assert.Equal(Strong, result)

[<Fact>]
let ``chained result satisfies a dependency of a multi-arg function`` () =
    // Arrange
    let filter: RawSignal -> FilteredSignal =
        fun raw ->
            let (Volts v) = raw.Reading

            {
                CleanedReading = Volts(v * 0.9)
                NoiseRemoved = true
            }

    let normalize: FilteredSignal -> NormalizedSignal =
        fun filtered ->
            let (Volts v) = filtered.CleanedReading

            {
                NormalizedValue = v / 100.0
                WithinRange = v < 100.0
            }

    let assessRisk: (RawSignal -> NormalizedSignal) -> RawSignal -> RiskAssessment =
        fun getNormalized raw ->
            let n = getNormalized raw

            {
                Level = if n.NormalizedValue > 0.5 then High else Low
                Description = sprintf "Value=%.2f, in range=%b" n.NormalizedValue n.WithinRange
            }

    let graph = register filter >> register normalize >> register assessRisk |> buildComposed

    // Act
    let resolved: RawSignal -> RiskAssessment = resolve graph

    // Assert
    Assert.Equal(Low, (resolved { Reading = Volts 50.0; Timestamp = 1 }).Level)
    Assert.Equal(High, (resolved { Reading = Volts 80.0; Timestamp = 1 }).Level)

[<Fact>]
let ``partially applied multi-arg function becomes a chain link`` () =
    // Arrange
    let filter: RawSignal -> FilteredSignal =
        fun raw ->
            let (Volts v) = raw.Reading

            {
                CleanedReading = Volts(v * 0.9)
                NoiseRemoved = true
            }

    let calibratedNormalize: (int -> CalibrationFactor) -> FilteredSignal -> NormalizedSignal =
        fun getCalibration filtered ->
            let cal = getCalibration 0
            let (Volts v) = filtered.CleanedReading

            {
                NormalizedValue = (v * cal.Factor) + cal.Offset
                WithinRange = v < 100.0
            }

    let getCalibration: int -> CalibrationFactor =
        fun _ -> { Factor = 0.01; Offset = 0.05 }

    let graph =
        register filter
        >> register calibratedNormalize
        >> register getCalibration
        |> buildComposed

    // Act
    let pipeline: RawSignal -> NormalizedSignal = resolve graph

    // Assert
    let result = pipeline { Reading = Volts 50.0; Timestamp = 1 }
    Assert.Equal(0.5, result.NormalizedValue)

[<Fact>]
let ``partial application and composition from same building blocks do not conflict`` () =
    // Arrange
    let readTemperature: int -> Celsius = fun id -> Celsius(float id * 10.0)

    let readAndCalibrate: (int -> Celsius) -> int -> TemperatureReading =
        fun getTemp id ->
            let (Celsius c) = getTemp id
            { Temperature = Celsius(c + 0.5); SensorId = string id }

    let classifyFromReading: (int -> TemperatureReading) -> int -> ClassifiedSignal =
        fun getReading id ->
            let reading = getReading id
            let (Celsius c) = reading.Temperature
            { Category = (if c > 50.0 then Strong else Weak); Confidence = 0.9 }

    let graph =
        register readTemperature
        >> register readAndCalibrate
        >> register classifyFromReading
        |> buildComposed

    // Act
    let resolved: int -> ClassifiedSignal = resolve graph

    // Assert
    let result = resolved 5
    Assert.Equal(Strong, result.Category)

[<Fact>]
let ``curried function output feeds into higher-order function`` () =
    // Arrange
    let produceFunction: RawSignal -> FilteredSignal -> NormalizedSignal =
        fun raw ->
            let (Volts v) = raw.Reading
            fun filtered ->
                let (Volts fv) = filtered.CleanedReading
                { NormalizedValue = (v + fv) / 100.0; WithinRange = true }

    let consumeFunction: (FilteredSignal -> NormalizedSignal) -> ClassifiedSignal =
        fun normalize ->
            let n = normalize { CleanedReading = Volts 50.0; NoiseRemoved = true }
            { Category = (if n.NormalizedValue > 0.5 then Strong else Weak); Confidence = 0.95 }

    let graph = register produceFunction >> register consumeFunction |> buildComposed

    // Act
    let resolved: RawSignal -> ClassifiedSignal = resolve graph

    // Assert
    let result = resolved { Reading = Volts 80.0; Timestamp = 1 }
    Assert.Equal(Strong, result.Category)

[<Fact>]
let ``registration order does not affect resolution`` () =
    // Arrange
    let normalize: FilteredSignal -> NormalizedSignal =
        fun filtered ->
            let (Volts v) = filtered.CleanedReading

            {
                NormalizedValue = v / 100.0
                WithinRange = v < 100.0
            }

    let filter: RawSignal -> FilteredSignal =
        fun raw ->
            let (Volts v) = raw.Reading

            {
                CleanedReading = Volts(v * 0.9)
                NoiseRemoved = true
            }

    let graph = register normalize >> register filter |> buildComposed

    // Act
    let pipeline: RawSignal -> NormalizedSignal = resolve graph

    // Assert
    let result = pipeline { Reading = Volts 50.0; Timestamp = 1 }
    Assert.Equal(0.45, result.NormalizedValue)

// ── Build fails ─────────────────────────────────────────────────────

[<Fact>]
let ``direct registration and chain to same signature fail at build time`` () =
    // Arrange
    let viaChainStep1: RawSignal -> FilteredSignal =
        fun raw ->
            let (Volts v) = raw.Reading

            {
                CleanedReading = Volts(v * 0.9)
                NoiseRemoved = true
            }

    let viaChainStep2: FilteredSignal -> NormalizedSignal =
        fun filtered ->
            let (Volts v) = filtered.CleanedReading

            {
                NormalizedValue = v / 100.0
                WithinRange = v < 100.0
            }

    let direct: RawSignal -> NormalizedSignal =
        fun raw ->
            let (Volts v) = raw.Reading

            {
                NormalizedValue = v / 50.0
                WithinRange = true
            }

    // Act
    let ex =
        Assert.Throws<exn>(fun () ->
            register viaChainStep1 >> register viaChainStep2 >> register direct
            |> buildComposed
            |> ignore)

    // Assert
    Assert.Contains("Ambiguous", ex.Message)

[<Fact>]
let ``two composition paths to same output fail at build time`` () =
    // Arrange
    let pathA: RawSignal -> PathAIntermediate =
        fun raw ->
            let (Volts v) = raw.Reading
            { ViaA = v * 2.0 }

    let pathB: RawSignal -> PathBIntermediate =
        fun raw ->
            let (Volts v) = raw.Reading
            { ViaB = v * 3.0 }

    let finishA: PathAIntermediate -> FinalOutput = fun a -> { Value = a.ViaA }

    let finishB: PathBIntermediate -> FinalOutput = fun b -> { Value = b.ViaB }

    // Act
    let ex =
        Assert.Throws<exn>(fun () ->
            register pathA >> register pathB >> register finishA >> register finishB
            |> buildComposed
            |> ignore)

    // Assert
    Assert.Contains("Ambiguous", ex.Message)

[<Fact>]
let ``partial-application and chain to same dependency fail at build time`` () =
    // Arrange
    let filterStep: RawSignal -> FilteredSignal =
        fun raw ->
            let (Volts v) = raw.Reading

            {
                CleanedReading = Volts(v * 0.9)
                NoiseRemoved = true
            }

    let normalizeStep: FilteredSignal -> NormalizedSignal =
        fun filtered ->
            let (Volts v) = filtered.CleanedReading

            {
                NormalizedValue = v / 100.0
                WithinRange = true
            }

    let calibratedNormalize: (int -> CalibrationFactor) -> RawSignal -> NormalizedSignal =
        fun getCal raw ->
            let (Volts v) = raw.Reading
            let cal = getCal 0

            {
                NormalizedValue = v * cal.Factor
                WithinRange = true
            }

    let getCalibration: int -> CalibrationFactor =
        fun _ -> { Factor = 0.01; Offset = 0.0 }

    let classify: (RawSignal -> NormalizedSignal) -> RawSignal -> ClassifiedSignal =
        fun getNorm raw ->
            let n = getNorm raw

            {
                Category = (if n.NormalizedValue > 0.5 then Strong else Weak)
                Confidence = 0.9
            }

    // Act
    let ex =
        Assert.Throws<exn>(fun () ->
            register filterStep
            >> register normalizeStep
            >> register calibratedNormalize
            >> register getCalibration
            >> register classify
            |> buildComposed
            |> ignore)

    // Assert
    Assert.Contains("Ambiguous", ex.Message)

[<Fact>]
let ``chained dependency that creates ambiguity for a consumer fails at build time`` () =
    // Arrange
    let direct: RawSignal -> FinalOutput =
        fun raw ->
            let (Volts v) = raw.Reading
            { Value = v }

    let filterStep: RawSignal -> FilteredSignal =
        fun raw ->
            let (Volts v) = raw.Reading

            {
                CleanedReading = Volts(v * 0.9)
                NoiseRemoved = true
            }

    let finishFromFiltered: FilteredSignal -> FinalOutput =
        fun filtered ->
            let (Volts v) = filtered.CleanedReading
            { Value = v }

    // Act
    let ex =
        Assert.Throws<exn>(fun () ->
            register direct >> register filterStep >> register finishFromFiltered
            |> buildComposed
            |> ignore)

    // Assert
    Assert.Contains("Ambiguous", ex.Message)

[<Fact>]
let ``cycle in composition graph fails at build time`` () =
    // Arrange
    let filterWithCalibration: (int -> NormalizedSignal) -> RawSignal -> FilteredSignal =
        fun _getNorm raw ->
            let (Volts v) = raw.Reading

            {
                CleanedReading = Volts(v * 0.9)
                NoiseRemoved = true
            }

    let calibrate: int -> NormalizedSignal =
        fun x ->
            {
                NormalizedValue = float x / 100.0
                WithinRange = true
            }

    let decode: FilteredSignal -> DecodedSignal =
        fun filtered ->
            let (Volts v) = filtered.CleanedReading
            { DecodedValue = v * 2.0 }

    let reconstruct: DecodedSignal -> RawSignal =
        fun decoded ->
            {
                Reading = Volts decoded.DecodedValue
                Timestamp = 0
            }

    // Act
    let ex =
        Assert.Throws<exn>(fun () ->
            register filterWithCalibration
            >> register calibrate
            >> register decode
            >> register reconstruct
            |> buildComposed
            |> ignore)

    // Assert
    Assert.Contains("Cycle detected", ex.Message)

[<Fact>]
let ``cycle from chaining back into partial application fails at build time`` () =
    // Arrange
    let filterWithCalibration: (int -> NormalizedSignal) -> RawSignal -> FilteredSignal =
        fun _getNorm raw ->
            let (Volts v) = raw.Reading

            {
                CleanedReading = Volts(v * 0.9)
                NoiseRemoved = true
            }

    let calibrate: int -> NormalizedSignal =
        fun x ->
            {
                NormalizedValue = float x / 100.0
                WithinRange = true
            }

    let decode: FilteredSignal -> DecodedSignal =
        fun filtered ->
            let (Volts v) = filtered.CleanedReading
            { DecodedValue = v * 2.0 }

    let reconstruct: DecodedSignal -> RawSignal =
        fun decoded ->
            {
                Reading = Volts decoded.DecodedValue
                Timestamp = 0
            }

    let report: FilteredSignal -> SignalReport =
        fun filtered ->
            {
                Summary = sprintf "Cleaned: %A" filtered.CleanedReading
            }

    // Act
    let ex =
        Assert.Throws<exn>(fun () ->
            register filterWithCalibration
            >> register calibrate
            >> register decode
            >> register reconstruct
            >> register report
            |> buildComposed
            |> ignore)

    // Assert
    Assert.Contains("Cycle detected", ex.Message)

// ── Resolve fails ───────────────────────────────────────────────────

[<Fact>]
let ``unreachable signature produces readable error`` () =
    // Arrange
    let filter: RawSignal -> FilteredSignal =
        fun raw ->
            let (Volts v) = raw.Reading

            {
                CleanedReading = Volts(v * 0.9)
                NoiseRemoved = true
            }

    let graph = register filter |> buildComposed

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolve<RawSignal -> NormalizedSignal> graph |> ignore)

    // Assert
    Assert.Contains("Cannot resolve", ex.Message)

[<Fact>]
let ``unregistered type produces readable error`` () =
    // Arrange
    let graph = buildComposed id

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolve<RawSignal -> NormalizedSignal> graph |> ignore)

    // Assert
    Assert.Contains("Cannot resolve", ex.Message)
    Assert.Contains("NormalizedSignal", ex.Message)

[<Fact>]
let ``missing dependency produces readable diagnostic`` () =
    // Arrange
    let assessRisk: (RawSignal -> NormalizedSignal) -> RawSignal -> RiskAssessment =
        fun _getNorm _raw -> { Level = Low; Description = "" }

    let graph = register assessRisk |> buildComposed

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolve<RawSignal -> RiskAssessment> graph |> ignore)

    // Assert
    Assert.Contains("Cannot resolve", ex.Message)
    Assert.Contains("NormalizedSignal", ex.Message)

[<Fact>]
let ``empty graph produces readable error`` () =
    // Arrange
    let graph = buildComposed id

    // Act
    let ex = Assert.Throws<exn>(fun () -> resolve<NormalizedSignal> graph |> ignore)

    // Assert
    Assert.Contains("Cannot resolve", ex.Message)
