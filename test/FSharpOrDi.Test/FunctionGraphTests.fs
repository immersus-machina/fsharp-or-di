module FSharpOrDi.Test.FunctionGraphTests

open Xunit
open FSharpOrDi.TypeSignature
open FSharpOrDi.ResolutionGraph
open FSharpOrDi.GrowthPlan
open FSharpOrDi.FunctionGraph

let private signatureA = ValueType typeof<int>
let private signatureB = ValueType typeof<string>

let private makeNode signature origin =
    { Signature = signature; Implementation = obj (); Origin = origin }

let private alwaysValidStage (_stage: Stage) : Result<unit, string> = Ok()

let private alwaysAcceptFilter (_stage: Stage) (node: Node) : Node option = Some node

let private identityDeduplication (nodes: Node list) : Node list = nodes

[<Fact>]
let ``growFromRegistrations adds nodes produced by single growth plan`` () =
    // Arrange
    let nodeToAdd = makeNode signatureA (Registered signatureA)

    let mutable callCount = 0
    let growthPlanThatProducesOnce: GrowthPlan =
        fun _question ->
            callCount <- callCount + 1
            if callCount = 1 then [ nodeToAdd ] else []

    let initialStage = { Nodes = Map.empty }

    // Act
    let resultStage =
        growFromRegistrations [ growthPlanThatProducesOnce ] alwaysValidStage alwaysAcceptFilter identityDeduplication initialStage

    // Assert
    Assert.True(resultStage.Nodes.ContainsKey(signatureA))
    Assert.Equal(1, resultStage.Nodes.Count)

[<Fact>]
let ``growFromRegistrations iterates until no new nodes are produced`` () =
    // Arrange
    let nodeA = makeNode signatureA (Registered signatureA)
    let nodeB = makeNode signatureB (Registered signatureB)

    let mutable iterationCount = 0
    let growthPlanWithTwoIterations: GrowthPlan =
        fun _question ->
            iterationCount <- iterationCount + 1
            match iterationCount with
            | 1 -> [ nodeA ]
            | 2 -> [ nodeB ]
            | _ -> []

    let initialStage = { Nodes = Map.empty }

    // Act
    let resultStage =
        growFromRegistrations [ growthPlanWithTwoIterations ] alwaysValidStage alwaysAcceptFilter identityDeduplication initialStage

    // Assert
    Assert.Equal(2, resultStage.Nodes.Count)
    Assert.True(resultStage.Nodes.ContainsKey(signatureA))
    Assert.True(resultStage.Nodes.ContainsKey(signatureB))

[<Fact>]
let ``growFromRegistrations throws when stage validator returns error`` () =
    // Arrange
    let emptyGrowthPlan: GrowthPlan = fun _question -> []

    let failingStageValidator (_stage: Stage) : Result<unit, string> =
        Error "Cycle detected at stable state"

    let initialStage = { Nodes = Map.empty }

    // Act
    let thrownException =
        Assert.Throws<System.Exception>(fun () ->
            growFromRegistrations [ emptyGrowthPlan ] failingStageValidator alwaysAcceptFilter identityDeduplication initialStage
            |> ignore)

    // Assert
    Assert.Contains("Cycle detected at stable state", thrownException.Message)

[<Fact>]
let ``growFromRegistrations collects nodes from multiple growth plans`` () =
    // Arrange
    let nodeA = makeNode signatureA (Registered signatureA)
    let nodeB = makeNode signatureB (Registered signatureB)

    let mutable firstPlanCallCount = 0
    let firstGrowthPlan: GrowthPlan =
        fun _question ->
            firstPlanCallCount <- firstPlanCallCount + 1
            if firstPlanCallCount = 1 then [ nodeA ] else []

    let mutable secondPlanCallCount = 0
    let secondGrowthPlan: GrowthPlan =
        fun _question ->
            secondPlanCallCount <- secondPlanCallCount + 1
            if secondPlanCallCount = 1 then [ nodeB ] else []

    let initialStage = { Nodes = Map.empty }

    // Act
    let resultStage =
        growFromRegistrations [ firstGrowthPlan; secondGrowthPlan ] alwaysValidStage alwaysAcceptFilter identityDeduplication initialStage

    // Assert
    Assert.Equal(2, resultStage.Nodes.Count)
    Assert.True(resultStage.Nodes.ContainsKey(signatureA))
    Assert.True(resultStage.Nodes.ContainsKey(signatureB))

[<Fact>]
let ``growFromRegistrations respects the injected filter`` () =
    // Arrange
    let existingNode = makeNode signatureA (Registered signatureA)

    let growthPlanThatReproducesExistingNode: GrowthPlan =
        fun _question -> [ makeNode signatureA (Registered signatureA) ]

    let filterThatSkipsSameOriginDuplicates (stage: Stage) (node: Node) : Node option =
        match Map.tryFind node.Signature stage.Nodes with
        | Some existingNode when existingNode.Origin = node.Origin -> None
        | _ -> Some node

    let initialStage = { Nodes = Map.ofList [ (signatureA, existingNode) ] }

    // Act
    let resultStage =
        growFromRegistrations [ growthPlanThatReproducesExistingNode ] alwaysValidStage filterThatSkipsSameOriginDuplicates identityDeduplication initialStage

    // Assert
    Assert.Equal(1, resultStage.Nodes.Count)

[<Fact>]
let ``growFromRegistrations respects the injected deduplication`` () =
    // Arrange
    let node = makeNode signatureA (Registered signatureA)

    let mutable callCount = 0
    let growthPlan: GrowthPlan =
        fun _question ->
            callCount <- callCount + 1
            if callCount = 1 then [ node; node; node ] else []

    let deduplicateThatKeepsFirst (nodes: Node list) : Node list =
        nodes |> List.distinctBy (fun n -> n.Signature)

    let initialStage = { Nodes = Map.empty }

    // Act
    let resultStage =
        growFromRegistrations [ growthPlan ] alwaysValidStage alwaysAcceptFilter deduplicateThatKeepsFirst initialStage

    // Assert
    Assert.Equal(1, resultStage.Nodes.Count)
    Assert.True(resultStage.Nodes.ContainsKey(signatureA))
