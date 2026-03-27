module FSharpOrDi.FunctionRegistry

/// An immutable registry of functions and values for signature-based resolution.
type Registry = private Registry of ResolutionGraph.Stage

let private empty: Registry = Registry ResolutionGraph.emptyStage

/// Registers a function or value for signature-based resolution.
let register (value: 'a) (registry: Registry) : Registry =
    let (Registry stage) = registry
    let boxedValue = box value
    let signature = CompositionRoot.decomposeType (boxedValue.GetType())

    CompositionRoot.failIfAlreadyRegistered signature stage

    let node: ResolutionGraph.Node =
        {
            Signature = signature
            Implementation = boxedValue
            Origin = ResolutionGraph.Registered signature
        }

    Registry(ResolutionGraph.addNode node stage)

let private buildWithGrowthPlans
    (growthPlans: GrowthPlan.GrowthPlan list)
    (registrations: Registry -> Registry)
    : FunctionGraph.Graph =
    let (Registry stage) = registrations empty

    let stableStage =
        GraphGrowth.growFromRegistrations
            growthPlans
            CompositionRoot.cycleDetector
            CompositionRoot.filterCandidateAgainstExistingStage
            CompositionRoot.deduplicateBatch
            stage

    FunctionGraph.fromStage stableStage

/// Builds a function graph using partial application to wire dependencies.
let build (registrations: Registry -> Registry) : FunctionGraph.Graph =
    let growthPlans =
        [ GrowthPlan.partialApplicationGrowth RuntimeInvocation.applyFunction ]

    buildWithGrowthPlans growthPlans registrations

/// Builds a function graph using partial application and composition chaining.
let buildComposed (registrations: Registry -> Registry) : FunctionGraph.Graph =
    let growthPlans =
        [
            GrowthPlan.partialApplicationGrowth RuntimeInvocation.applyFunction
            GrowthPlan.compositionGrowth CompositionRoot.composeFunctions CompositionRoot.reconstructType
        ]

    buildWithGrowthPlans growthPlans registrations
