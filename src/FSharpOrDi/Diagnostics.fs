module internal FSharpOrDi.Diagnostics

open TypeSignature
open ResolutionGraph

type private ProducerMatch =
    | DirectProducer
    | EventualProducer

let rec private eventualReturnType (signature: TypeSignature) : TypeSignature =
    match signature with
    | FunctionType(_, output) -> eventualReturnType output
    | ValueType _ -> signature

let private findPotentialProducers (targetSignature: TypeSignature) (stage: Stage) : (Node * ProducerMatch) list =
    allNodes stage
    |> List.choose (fun node ->
        match node.Signature with
        | FunctionType(_, output) when output = targetSignature ->
            Some(node, DirectProducer)
        | FunctionType(_, output) when eventualReturnType output = targetSignature ->
            Some(node, EventualProducer)
        | _ -> None)

let private indent (depth: int) : string =
    String.replicate (depth * 2) " "

let describeMissingDependencies
    (formatSignature: TypeSignature -> string)
    (targetSignature: TypeSignature)
    (stage: Stage)
    : string =

    let rec describeWhyMissing (visited: Set<TypeSignature>) (depth: int) (signature: TypeSignature) : string list =
        if Set.contains signature visited then
            [ sprintf "%sCyclic dependency on: %s" (indent depth) (formatSignature signature) ]
        else
            let visited = Set.add signature visited
            let producers = findPotentialProducers signature stage

            match producers with
            | [] ->
                [ sprintf "%sNo registered function produces %s" (indent depth) (formatSignature signature) ]
            | producers ->
                producers
                |> List.collect (fun (producer, producerMatch) ->
                    let matchDescription =
                        match producerMatch with
                        | DirectProducer -> "Could be produced by"
                        | EventualProducer -> "Could eventually be produced by (after partial application of)"

                    let producerLine =
                        sprintf "%s%s: %s" (indent depth) matchDescription (formatSignature producer.Signature)

                    match producer.Signature with
                    | FunctionType(inputSignature, _) ->
                        match tryFindNode inputSignature stage with
                        | Some _ ->
                            [ producerLine
                              sprintf "%s  But input %s is available (unexpected)" (indent depth) (formatSignature inputSignature) ]
                        | None ->
                            [ producerLine
                              sprintf "%s  Missing: %s" (indent depth) (formatSignature inputSignature) ]
                            @ describeWhyMissing visited (depth + 2) inputSignature
                    | ValueType _ -> [ producerLine ])

    let header = sprintf "Cannot resolve: %s" (formatSignature targetSignature)
    let trace = describeWhyMissing Set.empty 1 targetSignature
    header :: trace |> String.concat "\n"
