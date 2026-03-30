module internal FSharpOrDi.Configuration.TypeClassification

open Microsoft.FSharp.Reflection

type TypeClassification =
    | PrimitiveType
    | OptionType of innerType: System.Type
    | SingleCaseDiscriminatedUnion of caseInfo: UnionCaseInfo * innerType: System.Type
    | RecordType of fields: (string * System.Type) array
    | ListType of elementType: System.Type
    | ArrayType of elementType: System.Type
    | UnsupportedType

let private isPrimitiveType (targetType: System.Type) =
    targetType = typeof<string>
    || targetType = typeof<int>
    || targetType = typeof<int64>
    || targetType = typeof<float>
    || targetType = typeof<decimal>
    || targetType = typeof<bool>
    || targetType = typeof<System.Guid>
    || targetType = typeof<System.TimeSpan>
    || targetType = typeof<System.DateTimeOffset>

let private isOptionType (targetType: System.Type) =
    targetType.IsGenericType
    && targetType.GetGenericTypeDefinition() = typedefof<option<_>>

let private isListType (targetType: System.Type) =
    targetType.IsGenericType
    && targetType.GetGenericTypeDefinition() = typedefof<list<_>>

let classifyType
    (isRecord: System.Type -> bool)
    (getRecordFields: System.Type -> System.Reflection.PropertyInfo array)
    (isUnion: System.Type -> bool)
    (getUnionCases: System.Type -> UnionCaseInfo array)
    (targetType: System.Type)
    : TypeClassification =
    if isPrimitiveType targetType then
        PrimitiveType
    elif targetType.IsArray then
        ArrayType(targetType.GetElementType())
    elif isOptionType targetType then
        OptionType(targetType.GetGenericArguments()[0])
    elif isListType targetType then
        ListType(targetType.GetGenericArguments()[0])
    elif isRecord targetType then
        let fields =
            getRecordFields targetType
            |> Array.map (fun propertyInfo -> (propertyInfo.Name, propertyInfo.PropertyType))

        RecordType fields
    elif isUnion targetType then
        match getUnionCases targetType with
        | [| singleCase |] ->
            match singleCase.GetFields() with
            | [| singleField |] ->
                SingleCaseDiscriminatedUnion(singleCase, singleField.PropertyType)
            | _ -> UnsupportedType
        | _ -> UnsupportedType
    else
        UnsupportedType
