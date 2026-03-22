module internal FSharpOrDi.ResolutionGraph

open TypeSignature

type NodeOrigin =
    | Registered of TypeSignature
    | DerivedByPartialApplication of functionOrigin: NodeOrigin * argumentOrigin: NodeOrigin
    | DerivedByComposition of firstOrigin: NodeOrigin * secondOrigin: NodeOrigin

type Node =
    {
        Signature: TypeSignature
        Implementation: obj
        Origin: NodeOrigin
    }

type Stage = { Nodes: Map<TypeSignature, Node> }

// In a curried language, composition and partial application are the same operation
// with different grouping. Same building blocks in the same order always produce the
// same result. Flattening to the ordered list of leaves is sufficient for equality.
let rec flattenOrigin (origin: NodeOrigin) : NodeOrigin list =
    match origin with
    | Registered _ -> [ origin ]
    | DerivedByPartialApplication(functionOrigin, argumentOrigin) ->
        flattenOrigin argumentOrigin @ flattenOrigin functionOrigin
    | DerivedByComposition(firstOrigin, secondOrigin) ->
        flattenOrigin firstOrigin @ flattenOrigin secondOrigin

let doNodesConflict (nodeA: Node) (nodeB: Node) : bool =
    if nodeA.Signature <> nodeB.Signature then false
    else flattenOrigin nodeA.Origin <> flattenOrigin nodeB.Origin

/// Not part of core resolution — used by formatOriginShallow for concise error messages
let rec signatureOf (origin: NodeOrigin) : TypeSignature =
    match origin with
    | Registered signature -> signature
    | DerivedByPartialApplication(functionOrigin, _) ->
        match signatureOf functionOrigin with
        | FunctionType(_, output) -> output
        | ValueType _ -> failwith "Cannot derive signature from partial application of non-function origin"
    | DerivedByComposition(firstOrigin, secondOrigin) ->
        match signatureOf firstOrigin, signatureOf secondOrigin with
        | FunctionType(firstInput, _), FunctionType(_, secondOutput) ->
            FunctionType(firstInput, secondOutput)
        | _ -> failwith "Cannot derive signature from composition of non-function origins"

// Convenience wrappers over Map — no logic, just domain vocabulary
let emptyStage: Stage = { Nodes = Map.empty }

let addNode (node: Node) (stage: Stage) : Stage =
    { stage with
        Nodes = Map.add node.Signature node stage.Nodes
    }

let tryFindNode (signature: TypeSignature) (stage: Stage) : Node option = Map.tryFind signature stage.Nodes
