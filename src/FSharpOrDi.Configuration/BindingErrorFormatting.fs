module FSharpOrDi.Configuration.BindingErrorFormatting

open BindingError

let formatError (error: BindingError) : string =
    match error with
    | MissingRequiredValue fieldName ->
        $"Missing required value: '%s{fieldName}'"
    | ValueConversionFailed(fieldName, rawValue, targetTypeName) ->
        $"Cannot convert '%s{rawValue}' to %s{targetTypeName} for field '%s{fieldName}'"
    | UnsupportedTargetType typeName ->
        $"Unsupported target type: '%s{typeName}'"

let formatErrors (errors: BindingError list) : string =
    errors
    |> List.map (fun error -> $"- %s{formatError error}")
    |> String.concat "\n"
    |> fun lines -> $"Configuration binding failed:\n%s{lines}"
