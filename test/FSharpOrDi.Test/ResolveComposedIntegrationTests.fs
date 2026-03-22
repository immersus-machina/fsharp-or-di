module FSharpOrDi.Test.ResolveComposedIntegrationTests

open Xunit
open FSharpOrDi.FunctionRegistry

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

[<Fact>]
let ``resolveComposed chains two single-arg functions`` () =
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

    let registry = empty |> register filter |> register normalize

    // Act
    let pipeline: RawSignal -> NormalizedSignal = resolveComposed registry

    // Assert
    let result = pipeline { Reading = Volts 50.0; Timestamp = 1 }
    Assert.Equal(0.45, result.NormalizedValue)
    Assert.True(result.WithinRange)

[<Fact>]
let ``resolveComposed does not report ambiguity for associative composition`` () =
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

    let registry = empty |> register stepOne |> register stepTwo |> register stepThree

    // Act
    let pipeline: RawSignal -> SignalCategory = resolveComposed registry

    // Assert
    let result = pipeline { Reading = Volts 80.0; Timestamp = 1 }
    Assert.Equal(Strong, result)

[<Fact>]
let ``resolveComposed chains three single-arg functions`` () =
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

    let registry = empty |> register filter |> register normalize |> register classify

    // Act
    let pipeline: RawSignal -> ClassifiedSignal = resolveComposed registry

    // Assert
    let result = pipeline { Reading = Volts 80.0; Timestamp = 1 }
    Assert.Equal(Strong, result.Category)

[<Fact>]
let ``resolveComposed chains to satisfy a dependency of a multi-arg function`` () =
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

    let registry = empty |> register filter |> register normalize |> register assessRisk

    // Act
    let resolved: RawSignal -> RiskAssessment = resolveComposed registry

    // Assert
    Assert.Equal(Low, (resolved { Reading = Volts 50.0; Timestamp = 1 }).Level)
    Assert.Equal(High, (resolved { Reading = Volts 80.0; Timestamp = 1 }).Level)

[<Fact>]
let ``resolveComposed uses partially applied multi-arg function as a chain link`` () =
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

    let registry =
        empty
        |> register filter
        |> register calibratedNormalize
        |> register getCalibration

    // Act
    let pipeline: RawSignal -> NormalizedSignal = resolveComposed registry

    // Assert
    let result = pipeline { Reading = Volts 50.0; Timestamp = 1 }
    Assert.Equal(0.5, result.NormalizedValue)

[<Fact>]
let ``resolveComposed does not report ambiguity when partial application and composition produce same result from same building blocks`` () =
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

    let registry =
        empty
        |> register readTemperature
        |> register readAndCalibrate
        |> register classifyFromReading

    // Act
    let resolved: int -> ClassifiedSignal = resolveComposed registry

    // Assert
    let result = resolved 5
    Assert.Equal(Strong, result.Category)

[<Fact>]
let ``resolveComposed resolves when curried function output feeds into higher-order function`` () =
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

    let registry =
        empty
        |> register produceFunction
        |> register consumeFunction

    // Act
    let resolved: RawSignal -> ClassifiedSignal = resolveComposed registry

    // Assert
    let result = resolved { Reading = Volts 80.0; Timestamp = 1 }
    Assert.Equal(Strong, result.Category)

[<Fact>]
let ``resolveComposed fails when neither partial application nor chaining can produce the signature`` () =
    // Arrange
    let filter: RawSignal -> FilteredSignal =
        fun raw ->
            let (Volts v) = raw.Reading

            {
                CleanedReading = Volts(v * 0.9)
                NoiseRemoved = true
            }

    let registry = empty |> register filter

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolveComposed<RawSignal -> NormalizedSignal> registry |> ignore)

    // Assert
    Assert.Contains("Cannot resolve", ex.Message)

[<Fact>]
let ``resolveComposed detects conflict when direct registration and chain both produce same signature`` () =
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

    let registry =
        empty |> register viaChainStep1 |> register viaChainStep2 |> register direct

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolveComposed<RawSignal -> NormalizedSignal> registry |> ignore)

    // Assert
    Assert.Contains("Ambiguous", ex.Message)

[<Fact>]
let ``resolveComposed fails on ambiguous chain with two paths to same output`` () =
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

    let registry =
        empty
        |> register pathA
        |> register pathB
        |> register finishA
        |> register finishB

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolveComposed<RawSignal -> FinalOutput> registry |> ignore)

    // Assert
    Assert.Contains("Ambiguous", ex.Message)

[<Fact>]
let ``resolveComposed fails when partial-application and chain both produce the same dependency`` () =
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

    let registry =
        empty
        |> register filterStep
        |> register normalizeStep
        |> register calibratedNormalize
        |> register getCalibration
        |> register classify

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolveComposed<RawSignal -> ClassifiedSignal> registry |> ignore)

    // Assert
    Assert.Contains("Ambiguous", ex.Message)

[<Fact>]
let ``resolveComposed fails when chained dependency creates ambiguity for a consumer`` () =
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

    let registry =
        empty |> register direct |> register filterStep |> register finishFromFiltered

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolveComposed<RawSignal -> FinalOutput> registry |> ignore)

    // Assert
    Assert.Contains("Ambiguous", ex.Message)

[<Fact>]
let ``resolveComposed detects cycle when graph contains loop even though direct result exists`` () =
    // Arrange
    // filterWithCalibration(calibrate) produces RawSignal -> FilteredSignal
    // decode >> reconstruct produces FilteredSignal -> RawSignal
    // Together they form a cycle: RawSignal -> FilteredSignal -> RawSignal

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

    let registry =
        empty
        |> register filterWithCalibration
        |> register calibrate
        |> register decode
        |> register reconstruct

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolveComposed<RawSignal -> FilteredSignal> registry |> ignore)

    // Assert
    Assert.Contains("Cycle detected", ex.Message)

[<Fact>]
let ``resolveComposed detects cycle when chaining feeds back into partial application`` () =
    // Arrange
    // Same cycle as above (RawSignal -> FilteredSignal -> RawSignal)
    // but requesting RawSignal -> SignalReport which has a valid direct path
    // The cycle in the graph still makes the configuration invalid

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

    let registry =
        empty
        |> register filterWithCalibration
        |> register calibrate
        |> register decode
        |> register reconstruct
        |> register report

    // Act
    let ex =
        Assert.Throws<exn>(fun () -> resolveComposed<RawSignal -> SignalReport> registry |> ignore)

    // Assert
    Assert.Contains("Cycle detected", ex.Message)
