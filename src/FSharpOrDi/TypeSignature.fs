module internal FSharpOrDi.TypeSignature

[<CustomComparison; CustomEquality>]
type TypeSignature =
    | ValueType of System.Type
    | FunctionType of input: TypeSignature * output: TypeSignature

    interface System.IComparable with
        member this.CompareTo(other) =
            match other with
            | :? TypeSignature as otherSignature -> TypeSignature.compare this otherSignature
            | _ -> invalidArg "other" "Cannot compare to non-TypeSignature"

    override this.Equals(other) =
        match other with
        | :? TypeSignature as otherSignature -> TypeSignature.compare this otherSignature = 0
        | _ -> false

    override this.GetHashCode() =
        match this with
        | ValueType systemType -> hash systemType.AssemblyQualifiedName
        | FunctionType(input, output) -> hash (input.GetHashCode(), output.GetHashCode())

    static member private compare (left: TypeSignature) (right: TypeSignature) : int =
        match left, right with
        | ValueType leftType, ValueType rightType ->
            compare leftType.AssemblyQualifiedName rightType.AssemblyQualifiedName
        | ValueType _, FunctionType _ -> -1
        | FunctionType _, ValueType _ -> 1
        | FunctionType(leftInput, leftOutput), FunctionType(rightInput, rightOutput) ->
            let inputComparison = TypeSignature.compare leftInput rightInput

            if inputComparison <> 0 then
                inputComparison
            else
                TypeSignature.compare leftOutput rightOutput

