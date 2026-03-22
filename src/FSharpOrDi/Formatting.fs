module internal FSharpOrDi.Formatting

open TypeSignature
open ResolutionGraph

let rec formatSignature (signature: TypeSignature) : string =
    match signature with
    | ValueType systemType -> systemType.Name
    | FunctionType(input, output) ->
        let inputString = formatSignature input
        let outputString = formatSignature output
        sprintf "(%s -> %s)" inputString outputString

let formatOriginShallow (formatTypeOfOrigin: NodeOrigin -> string) (origin: NodeOrigin) : string =
    match origin with
    | Registered _ -> sprintf "direct registration of %s" (formatTypeOfOrigin origin)
    | DerivedByPartialApplication(functionOrigin, argumentOrigin) ->
        sprintf
            "partial application of %s with %s"
            (formatTypeOfOrigin functionOrigin)
            (formatTypeOfOrigin argumentOrigin)
    | DerivedByComposition(firstOrigin, secondOrigin) ->
        sprintf
            "composition of %s >> %s"
            (formatTypeOfOrigin firstOrigin)
            (formatTypeOfOrigin secondOrigin)

let rec formatOriginDeep (formatSignature: TypeSignature -> string) (origin: NodeOrigin) : string =
    match origin with
    | Registered signature -> sprintf "direct registration of %s" (formatSignature signature)
    | DerivedByPartialApplication(functionOrigin, argumentOrigin) ->
        sprintf
            "partial application of [%s] with [%s]"
            (formatOriginDeep formatSignature functionOrigin)
            (formatOriginDeep formatSignature argumentOrigin)
    | DerivedByComposition(firstOrigin, secondOrigin) ->
        sprintf
            "composition of [%s] >> [%s]"
            (formatOriginDeep formatSignature firstOrigin)
            (formatOriginDeep formatSignature secondOrigin)
