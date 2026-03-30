module FSharpOrDi.Configuration.ConfigurationBinding

open Microsoft.Extensions.Configuration

let bind<'T> (section: IConfigurationSection) : Result<'T, BindingError.BindingError list> =
    let reader = CompositionRoot.fromConfigurationSection section
    CompositionRoot.bindValue "" typeof<'T> reader
    |> Result.map (fun value -> value :?> 'T)

let registerBind<'T> (section: IConfigurationSection) (registry: FSharpOrDi.FunctionRegistry.Registry) : FSharpOrDi.FunctionRegistry.Registry =
    match section |> bind<'T> with
    | Ok value -> FSharpOrDi.FunctionRegistry.register value registry
    | Error errors -> failwith (BindingErrorFormatting.formatErrors errors)
