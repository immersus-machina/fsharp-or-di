module FSharpOrDi.Configuration.BindingError

type BindingError =
    | MissingRequiredValue of fieldName: string
    | ValueConversionFailed of fieldName: string * rawValue: string * targetTypeName: string
    | UnsupportedTargetType of typeName: string
