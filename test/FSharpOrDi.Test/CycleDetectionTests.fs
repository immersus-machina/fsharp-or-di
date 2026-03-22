module FSharpOrDi.Test.CycleDetectionTests

open Xunit
open FSharpOrDi.TypeSignature
open FSharpOrDi.ResolutionGraph
open FSharpOrDi.CycleDetection

let rec private mockFormatSignature (signature: TypeSignature) =
    match signature with
    | ValueType systemType -> systemType.Name
    | FunctionType(input, output) -> sprintf "(%s -> %s)" (mockFormatSignature input) (mockFormatSignature output)

let private makeNode (signature: TypeSignature) (origin: NodeOrigin) : Node =
    {
        Signature = signature
        Implementation = obj ()
        Origin = origin
    }

let private makeStage (nodes: Node list) : Stage =
    {
        Nodes = nodes |> List.map (fun node -> node.Signature, node) |> Map.ofList
    }

[<Fact>]
let ``detectCycles returns Ok for empty stage`` () =
    // Arrange
    let stage = makeStage []

    // Act
    let result = detectCycles mockFormatSignature stage

    // Assert
    Assert.Equal(Ok(), result)

[<Fact>]
let ``detectCycles returns Ok for linear chain without cycles`` () =
    // Arrange
    let intToStringSignature = FunctionType(ValueType typeof<int>, ValueType typeof<string>)
    let stringToFloatSignature = FunctionType(ValueType typeof<string>, ValueType typeof<float>)
    let intToString = makeNode intToStringSignature (Registered intToStringSignature)
    let stringToFloat = makeNode stringToFloatSignature (Registered stringToFloatSignature)
    let stage = makeStage [ intToString; stringToFloat ]

    // Act
    let result = detectCycles mockFormatSignature stage

    // Assert
    Assert.Equal(Ok(), result)

[<Fact>]
let ``detectCycles returns Error when functions form a type cycle`` () =
    // Arrange
    let intToStringSignature = FunctionType(ValueType typeof<int>, ValueType typeof<string>)
    let stringToIntSignature = FunctionType(ValueType typeof<string>, ValueType typeof<int>)
    let intToString = makeNode intToStringSignature (Registered intToStringSignature)
    let stringToInt = makeNode stringToIntSignature (Registered stringToIntSignature)
    let stage = makeStage [ intToString; stringToInt ]

    // Act
    let result = detectCycles mockFormatSignature stage

    // Assert
    match result with
    | Error message -> Assert.Contains("Cycle detected", message)
    | Ok() -> Assert.Fail("Expected Error but got Ok")

[<Fact>]
let ``detectCycles returns Ok for three-node linear chain`` () =
    // Arrange
    let intToStringSignature = FunctionType(ValueType typeof<int>, ValueType typeof<string>)
    let stringToFloatSignature = FunctionType(ValueType typeof<string>, ValueType typeof<float>)
    let floatToBoolSignature = FunctionType(ValueType typeof<float>, ValueType typeof<bool>)
    let intToString = makeNode intToStringSignature (Registered intToStringSignature)
    let stringToFloat = makeNode stringToFloatSignature (Registered stringToFloatSignature)
    let floatToBool = makeNode floatToBoolSignature (Registered floatToBoolSignature)
    let stage = makeStage [ intToString; stringToFloat; floatToBool ]

    // Act
    let result = detectCycles mockFormatSignature stage

    // Assert
    Assert.Equal(Ok(), result)

[<Fact>]
let ``detectCycles returns Error for three-node cycle`` () =
    // Arrange
    let intToStringSignature = FunctionType(ValueType typeof<int>, ValueType typeof<string>)
    let stringToFloatSignature = FunctionType(ValueType typeof<string>, ValueType typeof<float>)
    let floatToIntSignature = FunctionType(ValueType typeof<float>, ValueType typeof<int>)
    let intToString = makeNode intToStringSignature (Registered intToStringSignature)
    let stringToFloat = makeNode stringToFloatSignature (Registered stringToFloatSignature)
    let floatToInt = makeNode floatToIntSignature (Registered floatToIntSignature)
    let stage = makeStage [ intToString; stringToFloat; floatToInt ]

    // Act
    let result = detectCycles mockFormatSignature stage

    // Assert
    match result with
    | Error message -> Assert.Contains("Cycle detected", message)
    | Ok() -> Assert.Fail("Expected Error but got Ok")

[<Fact>]
let ``detectCycles returns Ok for chain with higher order function that does not cycle`` () =
    // Arrange
    let intToStringSignature = FunctionType(ValueType typeof<int>, ValueType typeof<string>)
    let higherOrderSignature = FunctionType(intToStringSignature, ValueType typeof<float>)
    let floatToBoolSignature = FunctionType(ValueType typeof<float>, ValueType typeof<bool>)
    let boolToIntSignature = FunctionType(ValueType typeof<bool>, ValueType typeof<int>)
    let higherOrder = makeNode higherOrderSignature (Registered higherOrderSignature)
    let floatToBool = makeNode floatToBoolSignature (Registered floatToBoolSignature)
    let boolToInt = makeNode boolToIntSignature (Registered boolToIntSignature)
    let stage = makeStage [ higherOrder; floatToBool; boolToInt ]

    // Act
    let result = detectCycles mockFormatSignature stage

    // Assert
    Assert.Equal(Ok(), result)

[<Fact>]
let ``detectCycles returns Error for chain with higher order function that cycles`` () =
    // Arrange
    let intToStringSignature = FunctionType(ValueType typeof<int>, ValueType typeof<string>)
    let higherOrderSignature = FunctionType(intToStringSignature, ValueType typeof<float>)
    let floatToBoolSignature = FunctionType(ValueType typeof<float>, ValueType typeof<bool>)
    let boolToIntToStringSignature = FunctionType(ValueType typeof<bool>, intToStringSignature)
    let higherOrder = makeNode higherOrderSignature (Registered higherOrderSignature)
    let floatToBool = makeNode floatToBoolSignature (Registered floatToBoolSignature)
    let boolToIntToString = makeNode boolToIntToStringSignature (Registered boolToIntToStringSignature)
    let stage = makeStage [ higherOrder; floatToBool; boolToIntToString ]

    // Act
    let result = detectCycles mockFormatSignature stage

    // Assert
    match result with
    | Error message -> Assert.Contains("Cycle detected", message)
    | Ok() -> Assert.Fail("Expected Error but got Ok")

[<Fact>]
let ``detectCycles does not confuse higher order input with similar simple type signatures`` () =
    // Arrange
    let boolToIntSignature = FunctionType(ValueType typeof<bool>, ValueType typeof<int>)
    let intToStringSignature = FunctionType(ValueType typeof<int>, ValueType typeof<string>)
    let higherOrderSignature = FunctionType(boolToIntSignature, ValueType typeof<string>)
    let boolToInt = makeNode boolToIntSignature (Registered boolToIntSignature)
    let intToString = makeNode intToStringSignature (Registered intToStringSignature)
    let higherOrder = makeNode higherOrderSignature (Registered higherOrderSignature)
    let stage = makeStage [ boolToInt; intToString; higherOrder ]

    // Act
    let result = detectCycles mockFormatSignature stage

    // Assert
    Assert.Equal(Ok(), result)
