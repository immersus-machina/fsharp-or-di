module internal FSharpOrDi.GrowthPlan

open TypeSignature
open ResolutionGraph

type GrowthQuestion = { NewNodes: Node list; CurrentStage: Stage }

type GrowthPlan = GrowthQuestion -> Node list

let private tryPartiallyApply
    (applyFunction: obj -> obj -> obj)
    (functionNode: Node)
    (argumentNode: Node)
    : Node option =
    match functionNode.Signature with
    | FunctionType(inputSignature, outputSignature) when argumentNode.Signature = inputSignature ->
        let resultImplementation =
            applyFunction functionNode.Implementation argumentNode.Implementation

        Some
            {
                Signature = outputSignature
                Implementation = resultImplementation
                Origin = DerivedByPartialApplication(functionNode.Origin, argumentNode.Origin)
            }
    | _ -> None

let partialApplicationGrowth (applyFunction: obj -> obj -> obj) : GrowthPlan =
    fun question ->
        let allNodes = allNodes question.CurrentStage

        question.NewNodes
        |> List.collect (fun newNode ->
            let asFunction = allNodes |> List.choose (fun existing -> tryPartiallyApply applyFunction newNode existing)
            let asArgument = allNodes |> List.choose (fun existing -> tryPartiallyApply applyFunction existing newNode)
            asFunction @ asArgument)

let private tryComposeNodes
    (composeFunctions: obj -> obj -> System.Type -> obj)
    (reconstructType: TypeSignature -> System.Type)
    (firstNode: Node)
    (secondNode: Node)
    : Node option =
    match firstNode.Signature, secondNode.Signature with
    | FunctionType(firstInput, firstOutput), FunctionType(secondInput, secondOutput) when secondInput = firstOutput ->
        let composedSignature = FunctionType(firstInput, secondOutput)
        let composedSystemType = reconstructType composedSignature
        let composedImplementation =
            composeFunctions firstNode.Implementation secondNode.Implementation composedSystemType

        Some
            {
                Signature = composedSignature
                Implementation = composedImplementation
                Origin = DerivedByComposition(firstNode.Origin, secondNode.Origin)
            }
    | _ -> None

let compositionGrowth
    (composeFunctions: obj -> obj -> System.Type -> obj)
    (reconstructType: TypeSignature -> System.Type)
    : GrowthPlan =
    fun question ->
        let allNodes = allNodes question.CurrentStage

        question.NewNodes
        |> List.collect (fun newNode ->
            let asFirst = allNodes |> List.choose (tryComposeNodes composeFunctions reconstructType newNode)
            let asSecond = allNodes |> List.choose (fun existing -> tryComposeNodes composeFunctions reconstructType existing newNode)
            asFirst @ asSecond)
