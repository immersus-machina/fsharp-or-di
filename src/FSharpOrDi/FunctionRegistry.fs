module FSharpOrDi.FunctionRegistry

open Microsoft.FSharp.Reflection

/// An immutable registry of functions and values for signature-based resolution.
type Registry = private Registry of ResolutionGraph.Stage

/// An empty function registry with no registrations.
let empty: Registry = Registry ResolutionGraph.emptyStage

let private decomposeType =
    TypeDecomposition.decomposeType FSharpType.IsFunction FSharpType.GetFunctionElements

let private reconstructType =
    TypeDecomposition.reconstructType (fun inputType outputType ->
        FSharpType.MakeFunctionType(inputType, outputType))

let private composeFunctions =
    RuntimeInvocation.composeFunctions (fun systemType converter ->
        FSharpValue.MakeFunction(systemType, converter))

let private formatOriginShallow =
    Formatting.formatOriginShallow (ResolutionGraph.signatureOf >> Formatting.formatSignature)

let private failIfAlreadyRegistered =
    AmbiguityDetection.failIfAlreadyRegistered Formatting.formatSignature

let private filterCandidateAgainstExistingStage =
    AmbiguityDetection.filterCandidateAgainstExistingStage Formatting.formatSignature formatOriginShallow

let private deduplicateBatch =
    AmbiguityDetection.deduplicateBatch Formatting.formatSignature formatOriginShallow

let private describeMissingDependencies =
    Diagnostics.describeMissingDependencies Formatting.formatSignature

let private cycleDetector = CycleDetection.detectCycles Formatting.formatSignature

let private resolveWithGrowthPlans
    (growthPlans: GrowthPlan.GrowthPlan list)
    (targetSignature: TypeSignature.TypeSignature)
    (registry: Registry)
    : obj =
    let (Registry stage) = registry

    let stableStage =
        FunctionGraph.growFromRegistrations
            growthPlans
            cycleDetector
            filterCandidateAgainstExistingStage
            deduplicateBatch
            stage

    match ResolutionGraph.tryFindNode targetSignature stableStage with
    | Some node -> node.Implementation
    | None -> failwith (describeMissingDependencies targetSignature stableStage)

/// Registers a function or value for signature-based resolution.
let register (value: 'a) (registry: Registry) : Registry =
    let (Registry stage) = registry
    let boxedValue = box value
    let signature = decomposeType (boxedValue.GetType())

    failIfAlreadyRegistered signature stage

    let node: ResolutionGraph.Node =
        {
            Signature = signature
            Implementation = boxedValue
            Origin = ResolutionGraph.Registered signature
        }

    Registry(ResolutionGraph.addNode node stage)

/// Resolves a function by System.Type using partial application.
let resolveByType (targetType: System.Type) (registry: Registry) : obj =
    let targetSignature = decomposeType targetType

    let growthPlans =
        [ GrowthPlan.partialApplicationGrowth RuntimeInvocation.applyFunction ]

    resolveWithGrowthPlans growthPlans targetSignature registry

/// Resolves a function by System.Type using partial application and composition chaining.
let resolveComposedByType (targetType: System.Type) (registry: Registry) : obj =
    let targetSignature = decomposeType targetType

    let growthPlans =
        [
            GrowthPlan.partialApplicationGrowth RuntimeInvocation.applyFunction
            GrowthPlan.compositionGrowth composeFunctions reconstructType
        ]

    resolveWithGrowthPlans growthPlans targetSignature registry

/// Resolves a function by type parameter using partial application.
let inline resolve<'a> (registry: Registry) : 'a =
    resolveByType typeof<'a> registry :?> 'a

/// Resolves a function by type parameter using partial application and composition chaining.
let inline resolveComposed<'a> (registry: Registry) : 'a =
    resolveComposedByType typeof<'a> registry :?> 'a
