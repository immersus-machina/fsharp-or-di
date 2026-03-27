module FSharpOrDi.FunctionGraph

/// A resolved function graph built from registered functions.
type Graph = private Graph of ResolutionGraph.Stage

let internal fromStage (stage: ResolutionGraph.Stage) : Graph = Graph stage

/// Resolves a function by System.Type from the built graph.
let resolveByType (targetType: System.Type) (graph: Graph) : obj =
    let (Graph stage) = graph
    let targetSignature = CompositionRoot.decomposeType targetType

    match ResolutionGraph.tryFindNode targetSignature stage with
    | Some node -> node.Implementation
    | None -> failwith (CompositionRoot.describeMissingDependencies targetSignature stage)

/// Resolves a function by type parameter from the built graph.
let inline resolve<'a> (graph: Graph) : 'a =
    resolveByType typeof<'a> graph :?> 'a
