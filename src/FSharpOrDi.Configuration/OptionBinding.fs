module internal FSharpOrDi.Configuration.OptionBinding

open BindingError
open ConfigurationReader

let bindOptionValue
    (bindInnerValue: string -> System.Type -> ConfigurationReader -> Result<obj, BindingError list>)
    (makeNone: System.Type -> obj)
    (makeSome: System.Type -> obj -> obj)
    (hasValue: ConfigurationReader -> bool)
    (innerType: System.Type)
    (fieldName: string)
    (reader: ConfigurationReader)
    : Result<obj, BindingError list> =
    if hasValue reader then
        bindInnerValue fieldName innerType reader
        |> Result.map (makeSome innerType)
    else
        Ok(makeNone innerType)
