module internal FSharpOrDi.Configuration.CollectionBinding

open BindingError
open ConfigurationReader

let bindCollection
    (bindElement: string -> System.Type -> ConfigurationReader -> Result<obj, BindingError list>)
    (buildCollection: System.Type -> obj list -> obj)
    (fieldName: string)
    (elementType: System.Type)
    (reader: ConfigurationReader)
    : Result<obj, BindingError list> =
    let children = reader.GetChildren()

    let results =
        children
        |> List.mapi (fun index childReader ->
            bindElement $"{fieldName}[{index}]" elementType childReader)

    let errors =
        results
        |> List.choose (fun result ->
            match result with
            | Error childErrors -> Some childErrors
            | Ok _ -> None)
        |> List.concat

    if errors.IsEmpty then
        let values =
            results
            |> List.map (fun result ->
                match result with
                | Ok value -> value
                | Error _ -> failwith "bindCollection: all results should be Ok when errors list is empty (bug in error accumulation)")

        Ok(buildCollection elementType values)
    else
        Error errors
