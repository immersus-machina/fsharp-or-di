module ConfigurationBinding.Validation

open ConfigurationBinding.Types

// Register this function alongside the configuration it validates.
// FSharpOrDi.FunctionRegistry.build will partially apply it at startup,
// executing the validation before the app starts serving.
let validateDatabaseConfiguration (config: DatabaseConfiguration) : DatabaseConfigurationValidation =
    let (Port port) = config.Port

    if System.String.IsNullOrWhiteSpace(config.Host) then
        failwith "Database host must not be empty"

    if port < 1 || port > 65535 then
        failwith $"Invalid database port: %d{port}"

    DatabaseConfigurationValidation()
