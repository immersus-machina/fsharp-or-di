module internal FSharpOrDi.Configuration.RecordBinding

open BindingError
open ConfigurationReader

let bindRecord
    (bindField: string -> System.Type -> ConfigurationReader -> Result<obj, BindingError list>)
    (makeRecord: System.Type -> obj array -> obj)
    (recordType: System.Type)
    (fields: (string * System.Type) array)
    (reader: ConfigurationReader)
    : Result<obj, BindingError list> =
    let results =
        fields
        |> Array.map (fun (fieldName, fieldType) ->
            let fieldReader = reader.GetSection fieldName
            bindField fieldName fieldType fieldReader)

    let errors =
        results
        |> Array.choose (fun result ->
            match result with
            | Error fieldErrors -> Some fieldErrors
            | Ok _ -> None)
        |> Array.toList
        |> List.concat

    if errors.IsEmpty then
        let values =
            results
            |> Array.map (fun result ->
                match result with
                | Ok value -> value
                | Error _ -> failwith "bindRecord: all results should be Ok when errors list is empty (bug in error accumulation)")

        Ok(makeRecord recordType values)
    else
        Error errors
