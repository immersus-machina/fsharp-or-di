module FSharpOrDi.Test.AmbiguityDetectionTests

open Xunit
open FSharpOrDi.TypeSignature
open FSharpOrDi.ResolutionGraph
open FSharpOrDi.AmbiguityDetection

let private makeNode (signature: TypeSignature) (origin: NodeOrigin) : Node =
    {
        Signature = signature
        Implementation = obj ()
        Origin = origin
    }

let private mockFormatSignature (_: TypeSignature) : string = "MOCK_SIGNATURE"

let private mockFormatOrigin (_: NodeOrigin) : string = "MOCK_ORIGIN"

[<Fact>]
let ``failIfAlreadyRegistered throws when signature already exists in stage`` () =
    // Arrange
    let signature = ValueType typeof<int>
    let existingNode = makeNode signature (Registered signature)
    let stage = addNode existingNode emptyStage

    // Act
    let action = fun () -> failIfAlreadyRegistered mockFormatSignature signature stage

    // Assert
    let exn = Assert.Throws<exn>(action)
    Assert.Contains("Already registered", exn.Message)
    Assert.Contains("MOCK_SIGNATURE", exn.Message)

[<Fact>]
let ``failIfAlreadyRegistered does nothing when signature does not exist in stage`` () =
    // Arrange
    let signature = ValueType typeof<int>

    // Act
    failIfAlreadyRegistered mockFormatSignature signature emptyStage

    // Assert
    ()

[<Fact>]
let ``filterCandidateAgainstExistingStage returns Some when no existing node with same signature`` () =
    // Arrange
    let candidateNode = makeNode (ValueType typeof<int>) (Registered(ValueType typeof<int>))

    // Act
    let result = filterCandidateAgainstExistingStage mockFormatSignature mockFormatOrigin emptyStage candidateNode

    // Assert
    Assert.Equal(Some candidateNode, result)

[<Fact>]
let ``filterCandidateAgainstExistingStage returns None when existing node has same flattened origin`` () =
    // Arrange
    let signature = ValueType typeof<int>
    let existingNode = makeNode signature (Registered signature)
    let stage = addNode existingNode emptyStage
    let candidateNode = makeNode signature (Registered signature)

    // Act
    let result = filterCandidateAgainstExistingStage mockFormatSignature mockFormatOrigin stage candidateNode

    // Assert
    Assert.Equal(None, result)

[<Fact>]
let ``filterCandidateAgainstExistingStage throws when existing and candidate have different flattened origins`` () =
    // Arrange
    let signature = ValueType typeof<int>
    let existingNode = makeNode signature (Registered signature)
    let stage = addNode existingNode emptyStage
    let candidateOrigin = DerivedByPartialApplication(Registered(ValueType typeof<string>), Registered(ValueType typeof<bool>))
    let candidateNode = makeNode signature candidateOrigin

    // Act
    let action = fun () -> filterCandidateAgainstExistingStage mockFormatSignature mockFormatOrigin stage candidateNode |> ignore

    // Assert
    let exn = Assert.Throws<exn>(action)
    Assert.Contains("Ambiguous", exn.Message)

[<Fact>]
let ``deduplicateBatch returns single node unchanged`` () =
    // Arrange
    let node = makeNode (ValueType typeof<int>) (Registered(ValueType typeof<int>))

    // Act
    let result = deduplicateBatch mockFormatSignature mockFormatOrigin [ node ]

    // Assert
    Assert.Equal<Node list>([ node ], result)

[<Fact>]
let ``deduplicateBatch keeps first node when flattened origins are identical`` () =
    // Arrange
    let outputSignature = FunctionType(ValueType typeof<string>, ValueType typeof<bool>)
    let sharedOrigin = DerivedByComposition(Registered(ValueType typeof<int>), Registered(ValueType typeof<string>))
    let firstNode = makeNode outputSignature sharedOrigin
    let secondNode = makeNode outputSignature sharedOrigin

    // Act
    let result = deduplicateBatch mockFormatSignature mockFormatOrigin [ firstNode; secondNode ]

    // Assert
    Assert.Equal<Node list>([ firstNode ], result)

[<Fact>]
let ``deduplicateBatch deduplicates left and right associated compositions of same building blocks`` () =
    // Arrange
    let originA = Registered(ValueType typeof<int>)
    let originB = Registered(ValueType typeof<string>)
    let originC = Registered(ValueType typeof<float>)
    let outputSignature = FunctionType(ValueType typeof<int>, ValueType typeof<float>)

    let leftAssociated = DerivedByComposition(DerivedByComposition(originA, originB), originC)
    let rightAssociated = DerivedByComposition(originA, DerivedByComposition(originB, originC))
    let firstNode = makeNode outputSignature leftAssociated
    let secondNode = makeNode outputSignature rightAssociated

    // Act
    let result = deduplicateBatch mockFormatSignature mockFormatOrigin [ firstNode; secondNode ]

    // Assert
    Assert.Equal<Node list>([ firstNode ], result)

[<Fact>]
let ``deduplicateBatch throws when mixed origins produce same signature`` () =
    // Arrange
    let signature = ValueType typeof<int>
    let registeredNode = makeNode signature (Registered signature)
    let compositionOrigin = DerivedByComposition(Registered(ValueType typeof<string>), Registered(ValueType typeof<bool>))
    let composedNode = makeNode signature compositionOrigin

    // Act
    let action = fun () -> deduplicateBatch mockFormatSignature mockFormatOrigin [ registeredNode; composedNode ] |> ignore

    // Assert
    let exn = Assert.Throws<exn>(action)
    Assert.Contains("Ambiguous", exn.Message)

[<Fact>]
let ``deduplicateBatch error message reports the actual conflicting node`` () =
    // Arrange
    let signature = ValueType typeof<int>
    let registeredOrigin = Registered signature
    let partialApplicationOrigin = DerivedByPartialApplication(Registered(ValueType typeof<string>), Registered(ValueType typeof<bool>))

    let identicalNode1 = makeNode signature registeredOrigin
    let identicalNode2 = makeNode signature registeredOrigin
    let conflictingNode = makeNode signature partialApplicationOrigin

    let formatOrigin (origin: NodeOrigin) : string =
        match origin with
        | Registered _ -> "REGISTERED"
        | DerivedByPartialApplication _ -> "PARTIAL_APPLICATION"
        | DerivedByComposition _ -> "COMPOSITION"

    // Act
    let action = fun () ->
        deduplicateBatch mockFormatSignature formatOrigin [ identicalNode1; identicalNode2; conflictingNode ] |> ignore

    // Assert
    let exn = Assert.Throws<exn>(action)
    Assert.Contains("REGISTERED", exn.Message)
    Assert.Contains("PARTIAL_APPLICATION", exn.Message)

[<Fact>]
let ``deduplicateBatch returns empty list for empty input`` () =
    // Arrange
    let emptyNodeList: Node list = []

    // Act
    let result = deduplicateBatch mockFormatSignature mockFormatOrigin emptyNodeList

    // Assert
    Assert.Empty(result)
