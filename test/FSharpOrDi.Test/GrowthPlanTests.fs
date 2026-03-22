module FSharpOrDi.Test.GrowthPlanTests

open Xunit
open FSharpOrDi.TypeSignature
open FSharpOrDi.ResolutionGraph
open FSharpOrDi.GrowthPlan

let private intSignature = ValueType typeof<int>
let private stringSignature = ValueType typeof<string>
let private floatSignature = ValueType typeof<float>
let private boolSignature = ValueType typeof<bool>

let private intToStringSignature = FunctionType(intSignature, stringSignature)
let private stringToFloatSignature = FunctionType(stringSignature, floatSignature)
let private intToFloatSignature = FunctionType(intSignature, floatSignature)
let private floatToBoolSignature = FunctionType(floatSignature, boolSignature)

let private makeNode signature implementation origin =
    { Signature = signature; Implementation = implementation; Origin = origin }

let private makeStage nodes =
    { Nodes = nodes |> List.map (fun node -> node.Signature, node) |> Map.ofList }

[<Fact>]
let ``partialApplicationGrowth produces growth plan that produces new node when input dependency is satisfied`` () =
    // Arrange
    let functionNode = makeNode intToStringSignature (box "intToStringFunc") (Registered intToStringSignature)
    let argumentNode = makeNode intSignature (box 42) (Registered intSignature)
    let stage = makeStage [ functionNode; argumentNode ]
    let applyFunction = fun funcImpl argImpl -> box $"applied({funcImpl},{argImpl})"

    // Act
    let growthPlan = partialApplicationGrowth applyFunction

    // Assert
    let result = growthPlan { NewNodes = [ functionNode ]; CurrentStage = stage }
    Assert.Equal(1, result.Length)
    let newNode = result.[0]
    Assert.Equal(stringSignature, newNode.Signature)
    Assert.Equal(box "applied(intToStringFunc,42)", newNode.Implementation)
    Assert.Equal(DerivedByPartialApplication(Registered intToStringSignature, Registered intSignature), newNode.Origin)

[<Fact>]
let ``partialApplicationGrowth produces growth plan that uses registered value as argument for partial application`` () =
    // Arrange
    let functionNode = makeNode intToStringSignature (box "intToStringFunc") (Registered intToStringSignature)
    let valueNode = makeNode intSignature (box 42) (Registered intSignature)
    let stage = makeStage [ functionNode; valueNode ]
    let applyFunction = fun funcImpl argImpl -> box $"applied({funcImpl},{argImpl})"

    // Act
    let growthPlan = partialApplicationGrowth applyFunction

    // Assert
    let result = growthPlan { NewNodes = [ valueNode ]; CurrentStage = stage }
    Assert.Equal(1, result.Length)
    Assert.Equal(stringSignature, result.[0].Signature)
    Assert.Equal(box "applied(intToStringFunc,42)", result.[0].Implementation)

[<Fact>]
let ``partialApplicationGrowth produces growth plan that produces nothing when no nodes are registered`` () =
    // Arrange
    let stage = makeStage []
    let applyFunction = fun _ _ -> failwith "should not be called"

    // Act
    let growthPlan = partialApplicationGrowth applyFunction

    // Assert
    let result = growthPlan { NewNodes = []; CurrentStage = stage }
    Assert.Empty result

[<Fact>]
let ``partialApplicationGrowth produces growth plan that calls applyFunction with correct implementations`` () =
    // Arrange
    let mutable capturedFuncImpl = None
    let mutable capturedArgImpl = None
    let functionNode = makeNode intToStringSignature (box "theFunction") (Registered intToStringSignature)
    let argumentNode = makeNode intSignature (box 99) (Registered intSignature)
    let stage = makeStage [ functionNode; argumentNode ]

    let applyFunction =
        fun funcImpl argImpl ->
            capturedFuncImpl <- Some funcImpl
            capturedArgImpl <- Some argImpl
            box "result"

    // Act
    let growthPlan = partialApplicationGrowth applyFunction

    // Assert
    growthPlan { NewNodes = [ functionNode; argumentNode ]; CurrentStage = stage } |> ignore
    Assert.Equal(Some (box "theFunction"), capturedFuncImpl)
    Assert.Equal(Some (box 99), capturedArgImpl)

[<Fact>]
let ``partialApplicationGrowth produces growth plan that returns empty when input dependency is not in stage`` () =
    // Arrange
    let functionNode = makeNode intToStringSignature (box "intToStringFunc") (Registered intToStringSignature)
    let wrongArgumentNode = makeNode floatSignature (box 3.14) (Registered floatSignature)
    let stage = makeStage [ functionNode; wrongArgumentNode ]
    let applyFunction = fun _ _ -> failwith "should not be called"

    // Act
    let growthPlan = partialApplicationGrowth applyFunction

    // Assert
    let result = growthPlan { NewNodes = [ functionNode; wrongArgumentNode ]; CurrentStage = stage }
    Assert.Empty result

[<Fact>]
let ``compositionGrowth produces growth plan that composes two functions where output of first matches input of second`` () =
    // Arrange
    let firstFunctionNode = makeNode intToStringSignature (box "intToString") (Registered intToStringSignature)
    let secondFunctionNode = makeNode stringToFloatSignature (box "stringToFloat") (Registered stringToFloatSignature)
    let stage = makeStage [ firstFunctionNode; secondFunctionNode ]
    let composeFunctions = fun firstImpl secondImpl _composedType -> box $"composed({firstImpl},{secondImpl})"
    let reconstructType = fun _signature -> typeof<int -> float>

    // Act
    let growthPlan = compositionGrowth composeFunctions reconstructType

    // Assert
    let result = growthPlan { NewNodes = [ firstFunctionNode ]; CurrentStage = stage }
    let composedNodes = result |> List.filter (fun node -> node.Signature = intToFloatSignature)
    Assert.Equal(1, composedNodes.Length)
    let composedNode = composedNodes.[0]
    Assert.Equal(intToFloatSignature, composedNode.Signature)
    Assert.Equal(DerivedByComposition(Registered intToStringSignature, Registered stringToFloatSignature), composedNode.Origin)

[<Fact>]
let ``compositionGrowth produces growth plan that returns empty when no composable pairs exist`` () =
    // Arrange
    let firstFunctionNode = makeNode intToStringSignature (box "intToString") (Registered intToStringSignature)
    let secondFunctionNode = makeNode floatToBoolSignature (box "floatToBool") (Registered floatToBoolSignature)
    let stage = makeStage [ firstFunctionNode; secondFunctionNode ]
    let composeFunctions = fun _ _ _ -> failwith "should not be called"
    let reconstructType = fun _ -> failwith "should not be called"

    // Act
    let growthPlan = compositionGrowth composeFunctions reconstructType

    // Assert
    let result = growthPlan { NewNodes = [ firstFunctionNode; secondFunctionNode ]; CurrentStage = stage }
    Assert.Empty result

[<Fact>]
let ``compositionGrowth produces growth plan that produces nothing when no nodes are registered`` () =
    // Arrange
    let stage = makeStage []
    let composeFunctions = fun _ _ _ -> failwith "should not be called"
    let reconstructType = fun _ -> failwith "should not be called"

    // Act
    let growthPlan = compositionGrowth composeFunctions reconstructType

    // Assert
    let result = growthPlan { NewNodes = []; CurrentStage = stage }
    Assert.Empty result

[<Fact>]
let ``compositionGrowth produces growth plan that passes reconstructed type to composeFunctions`` () =
    // Arrange
    let firstFunctionNode = makeNode intToStringSignature (box "first") (Registered intToStringSignature)
    let secondFunctionNode = makeNode stringToFloatSignature (box "second") (Registered stringToFloatSignature)
    let stage = makeStage [ firstFunctionNode; secondFunctionNode ]
    let mutable capturedType = None

    let composeFunctions =
        fun _ _ composedType ->
            capturedType <- Some composedType
            box "composed"

    let expectedType = typeof<int -> float>
    let reconstructType = fun _signature -> expectedType

    // Act
    let growthPlan = compositionGrowth composeFunctions reconstructType

    // Assert
    growthPlan { NewNodes = [ firstFunctionNode; secondFunctionNode ]; CurrentStage = stage } |> ignore
    let composedEntries =
        capturedType
        |> Option.filter (fun capturedSystemType -> capturedSystemType = expectedType)
    Assert.True(composedEntries.IsSome, "composeFunctions should have been called with the reconstructed type")

[<Fact>]
let ``compositionGrowth produces growth plan that skips ValueType nodes as first node`` () =
    // Arrange
    let valueNode = makeNode intSignature (box 42) (Registered intSignature)
    let functionNode = makeNode stringToFloatSignature (box "stringToFloat") (Registered stringToFloatSignature)
    let stage = makeStage [ valueNode; functionNode ]
    let composeFunctions = fun _ _ _ -> failwith "should not be called"
    let reconstructType = fun _ -> failwith "should not be called"

    // Act
    let growthPlan = compositionGrowth composeFunctions reconstructType

    // Assert
    let result = growthPlan { NewNodes = [ valueNode; functionNode ]; CurrentStage = stage }
    Assert.Empty result
