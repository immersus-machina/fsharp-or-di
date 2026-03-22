module internal FSharpOrDi.AmbiguityDetection

open TypeSignature
open ResolutionGraph

let failIfAlreadyRegistered
    (formatSignature: TypeSignature -> string)
    (signature: TypeSignature)
    (stage: Stage)
    : unit =
    match tryFindNode signature stage with
    | Some _ -> failwithf "Already registered: %s" (formatSignature signature)
    | None -> ()

let filterCandidateAgainstExistingStage
    (formatSignature: TypeSignature -> string)
    (formatOrigin: NodeOrigin -> string)
    (stage: Stage)
    (candidateNode: Node)
    : Node option =
    match Map.tryFind candidateNode.Signature stage.Nodes with
    | None -> Some candidateNode
    | Some existingNode ->
        if doNodesConflict existingNode candidateNode then
            failwithf
                "Ambiguous: %s produced by both %s and %s"
                (formatSignature candidateNode.Signature)
                (formatOrigin existingNode.Origin)
                (formatOrigin candidateNode.Origin)
        else
            None

let deduplicateBatch
    (formatSignature: TypeSignature -> string)
    (formatOrigin: NodeOrigin -> string)
    (newNodes: Node list)
    : Node list =
    newNodes
    |> List.groupBy (fun node -> node.Signature)
    |> List.map (fun (signature, nodesWithSameSignature) ->
        match nodesWithSameSignature with
        | [ singleNode ] -> singleNode
        | firstNode :: _ ->
            let conflictingNode =
                nodesWithSameSignature |> List.tryFind (fun node -> doNodesConflict firstNode node)

            match conflictingNode with
            | Some conflicting ->
                failwithf
                    "Ambiguous: %s produced by both %s and %s"
                    (formatSignature signature)
                    (formatOrigin firstNode.Origin)
                    (formatOrigin conflicting.Origin)
            | None -> firstNode
        | [] -> failwith "Unexpected empty group")
