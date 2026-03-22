module internal FSharpOrDi.TypeDecomposition

open TypeSignature

let rec decomposeType
    (isFunction: System.Type -> bool)
    (getFunctionElements: System.Type -> System.Type * System.Type)
    (systemType: System.Type)
    : TypeSignature =
    if isFunction systemType then
        let inputType, outputType = getFunctionElements systemType
        let input = decomposeType isFunction getFunctionElements inputType
        let output = decomposeType isFunction getFunctionElements outputType
        FunctionType(input, output)
    else
        ValueType systemType

let rec reconstructType
    (makeFunctionType: System.Type -> System.Type -> System.Type)
    (signature: TypeSignature)
    : System.Type =
    match signature with
    | ValueType systemType -> systemType
    | FunctionType(input, output) ->
        let inputType = reconstructType makeFunctionType input
        let outputType = reconstructType makeFunctionType output
        makeFunctionType inputType outputType
