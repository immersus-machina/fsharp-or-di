module internal FSharpOrDi.Configuration.SingleCaseUnionBinding

open BindingError
open ConfigurationReader

let bindSingleCaseUnion
    (bindInnerValue: string -> System.Type -> ConfigurationReader -> Result<obj, BindingError list>)
    (makeUnionCase: Microsoft.FSharp.Reflection.UnionCaseInfo -> obj array -> obj)
    (caseInfo: Microsoft.FSharp.Reflection.UnionCaseInfo)
    (innerType: System.Type)
    (fieldName: string)
    (reader: ConfigurationReader)
    : Result<obj, BindingError list> =
    bindInnerValue fieldName innerType reader
    |> Result.map (fun innerValue -> makeUnionCase caseInfo [| innerValue |])
