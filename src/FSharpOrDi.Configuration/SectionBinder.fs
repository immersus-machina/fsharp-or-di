module internal FSharpOrDi.Configuration.SectionBinder

open BindingError
open ConfigurationReader
open TypeClassification

let bindValue
    (classifyType: System.Type -> TypeClassification)
    (handlePrimitive: string -> System.Type -> ConfigurationReader -> Result<obj, BindingError list>)
    (handleRecord: System.Type -> (string * System.Type) array -> ConfigurationReader -> Result<obj, BindingError list>)
    (handleUnion: Microsoft.FSharp.Reflection.UnionCaseInfo -> System.Type -> string -> ConfigurationReader -> Result<obj, BindingError list>)
    (handleOption: System.Type -> string -> ConfigurationReader -> Result<obj, BindingError list>)
    (handleList: string -> System.Type -> ConfigurationReader -> Result<obj, BindingError list>)
    (handleArray: string -> System.Type -> ConfigurationReader -> Result<obj, BindingError list>)
    (fieldName: string)
    (targetType: System.Type)
    (reader: ConfigurationReader)
    : Result<obj, BindingError list> =
    match classifyType targetType with
    | PrimitiveType ->
        handlePrimitive fieldName targetType reader
    | RecordType fields ->
        handleRecord targetType fields reader
    | SingleCaseDiscriminatedUnion(caseInfo, innerType) ->
        handleUnion caseInfo innerType fieldName reader
    | OptionType innerType ->
        handleOption innerType fieldName reader
    | ListType elementType ->
        handleList fieldName elementType reader
    | ArrayType elementType ->
        handleArray fieldName elementType reader
    | UnsupportedType ->
        Error [ UnsupportedTargetType targetType.Name ]
