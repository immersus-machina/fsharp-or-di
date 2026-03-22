module internal FSharpOrDi.CycleDetection

open TypeSignature
open ResolutionGraph

// Standard DFS three-color cycle detection for directed graphs
type private VisitState =
    | Unvisited
    | InProgress
    | Visited

let private detectCycleInDirectedGraph
    (formatNodeLabel: 'a -> string)
    (nodes: 'a list)
    (adjacency: Map<'a, 'a list>)
    : Result<unit, string> =

    let rec visitNode (visitStates: Map<'a, VisitState>) (path: 'a list) (node: 'a) =
        match Map.tryFind node visitStates with
        | Some InProgress ->
            let cycleSteps =
                node :: path |> List.rev |> List.map formatNodeLabel

            let cyclePath =
                cycleSteps |> List.map (sprintf "  %s") |> String.concat "\n    to\n"

            Error(sprintf "Cycle detected:\n%s" cyclePath)
        | Some Visited -> Ok visitStates
        | Some Unvisited | None ->
            let visitStates = Map.add node InProgress visitStates
            let neighbors = Map.tryFind node adjacency |> Option.defaultValue []

            match visitNeighbors visitStates (node :: path) neighbors with
            | Ok visitStates -> Ok(Map.add node Visited visitStates)
            | Error message -> Error message

    and visitNeighbors (visitStates: Map<'a, VisitState>) (path: 'a list) (neighbors: 'a list) =
        match neighbors with
        | [] -> Ok visitStates
        | neighbor :: rest ->
            match visitNode visitStates path neighbor with
            | Ok visitStates -> visitNeighbors visitStates path rest
            | Error message -> Error message

    nodes
    |> List.fold
        (fun state node ->
            match state with
            | Error _ -> state
            | Ok visitStates ->
                match Map.tryFind node visitStates with
                | Some Unvisited | None -> visitNode visitStates [] node
                | _ -> Ok visitStates)
        (Ok(nodes |> List.map (fun node -> node, Unvisited) |> Map.ofList))
    |> Result.map ignore

/// Rejects cyclic type graphs even if a specific request would resolve without traversing the cycle.
let detectCycles (formatSignature: TypeSignature -> string) (stage: Stage) : Result<unit, string> =
    let nodes = allNodes stage

    let allTypeNodes =
        nodes
        |> List.collect (fun node ->
            match node.Signature with
            | FunctionType(input, output) -> [ input; output ]
            | ValueType _ -> [])
        |> List.distinct

    let adjacency =
        nodes
        |> List.fold
            (fun (accumulator: Map<TypeSignature, TypeSignature list>) node ->
                match node.Signature with
                | FunctionType(input, output) ->
                    let existing = Map.tryFind input accumulator |> Option.defaultValue []
                    Map.add input (output :: existing) accumulator
                | ValueType _ -> accumulator)
            Map.empty

    detectCycleInDirectedGraph formatSignature allTypeNodes adjacency
