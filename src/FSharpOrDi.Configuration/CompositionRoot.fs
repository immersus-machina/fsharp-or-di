module internal FSharpOrDi.Configuration.CompositionRoot

open Microsoft.FSharp.Reflection
open Microsoft.Extensions.Configuration
open ConfigurationReader

let private classifyType targetType =
    TypeClassification.classifyType
        FSharpType.IsRecord
        FSharpType.GetRecordFields
        FSharpType.IsUnion
        FSharpType.GetUnionCases
        targetType

let private makeRecord (recordType: System.Type) (values: obj array) =
    FSharpValue.MakeRecord(recordType, values)

let private makeUnionCase (caseInfo: UnionCaseInfo) (values: obj array) =
    FSharpValue.MakeUnion(caseInfo, values)

let private makeNone (innerType: System.Type) =
    let optionType = typedefof<option<_>>.MakeGenericType(innerType)
    let noneCase = FSharpType.GetUnionCases(optionType) |> Array.find (fun c -> c.Name = "None")
    FSharpValue.MakeUnion(noneCase, [||])

let private makeSome (innerType: System.Type) (value: obj) =
    let optionType = typedefof<option<_>>.MakeGenericType(innerType)
    let someCase = FSharpType.GetUnionCases(optionType) |> Array.find (fun c -> c.Name = "Some")
    FSharpValue.MakeUnion(someCase, [| value |])

let private buildList (elementType: System.Type) (values: obj list) =
    let listType = typedefof<list<_>>.MakeGenericType(elementType)
    let emptyCase = FSharpType.GetUnionCases(listType) |> Array.find (fun c -> c.Name = "Empty")
    let consCase = FSharpType.GetUnionCases(listType) |> Array.find (fun c -> c.Name = "Cons")

    values
    |> List.rev
    |> List.fold
        (fun accumulator value -> FSharpValue.MakeUnion(consCase, [| value; accumulator |]))
        (FSharpValue.MakeUnion(emptyCase, [||]))

let private buildArray (elementType: System.Type) (values: obj list) =
    let array = System.Array.CreateInstance(elementType, values.Length)

    values
    |> List.iteri (fun index value -> array.SetValue(value, index))

    box array

let private hasValue (reader: ConfigurationReader) =
    reader.GetValue "" |> Option.isSome
    || (reader.GetChildren() |> List.isEmpty |> not)

let private handlePrimitive
    (fieldName: string)
    (targetType: System.Type)
    (reader: ConfigurationReader)
    : Result<obj, BindingError.BindingError list> =
    match reader.GetValue "" with
    | Some rawValue ->
        ValueConversion.convertValue fieldName rawValue targetType
        |> Result.mapError (fun error -> [ error ])
    | None -> Error [ BindingError.MissingRequiredValue fieldName ]

let rec bindValue
    (fieldName: string)
    (targetType: System.Type)
    (reader: ConfigurationReader)
    : Result<obj, BindingError.BindingError list> =
    SectionBinder.bindValue
        classifyType
        handlePrimitive
        (RecordBinding.bindRecord bindValue makeRecord)
        (SingleCaseUnionBinding.bindSingleCaseUnion bindValue makeUnionCase)
        (OptionBinding.bindOptionValue bindValue makeNone makeSome hasValue)
        (CollectionBinding.bindCollection bindValue buildList)
        (CollectionBinding.bindCollection bindValue buildArray)
        fieldName
        targetType
        reader

let fromConfigurationSection (section: IConfigurationSection) : ConfigurationReader =
    let rec buildReader (configSection: IConfigurationSection) =
        {
            GetValue = fun key ->
                let value =
                    if System.String.IsNullOrEmpty(key) then
                        configSection.Value
                    else
                        configSection[key]

                if isNull value then None else Some value
            GetSection = fun key -> buildReader (configSection.GetSection(key))
            GetChildren =
                fun () ->
                    configSection.GetChildren()
                    |> Seq.sortBy (fun child -> child.Key)
                    |> Seq.map buildReader
                    |> Seq.toList
        }

    buildReader section
