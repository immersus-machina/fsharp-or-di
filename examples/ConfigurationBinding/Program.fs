open Microsoft.Extensions.Configuration
open FSharpOrDi
open ConfigurationBinding.Types
open ConfigurationBinding.Validation
open ConfigurationBinding.Startup
open FSharpOrDi.Configuration

// ── Build IConfiguration from appsettings.json ──────────────────────

let configuration =
    ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build()

// ── Bind individual sections ────────────────────────────────────────

printfn "=== Binding individual sections ===\n"

let appResult = configuration.GetSection("App") |> ConfigurationBinding.bind<AppConfiguration>
let dbResult = configuration.GetSection("Database") |> ConfigurationBinding.bind<DatabaseConfiguration>
let sensorResult = configuration.GetSection("Sensors") |> ConfigurationBinding.bind<SensorConfiguration>

printfn "App:      %A" appResult
printfn "Database: %A" dbResult
printfn "Sensors:  %A" sensorResult

// ── Error handling ──────────────────────────────────────────────────

printfn "\n=== Binding a missing section ===\n"

let missingResult = configuration.GetSection("DoesNotExist") |> ConfigurationBinding.bind<DatabaseConfiguration>

match missingResult with
| Ok config -> printfn "Got: %A" config
| Error errors -> printfn "%s" (BindingErrorFormatting.formatErrors errors)

// ── FSharpOrDi Valid configuration ─────────────────────────────────────────────

printfn "\n=== Valid configuration ===\n"

let graph =
    ConfigurationBinding.registerBind<AppConfiguration> (configuration.GetSection("App"))
    >> ConfigurationBinding.registerBind<DatabaseConfiguration> (configuration.GetSection("Database"))
    >> FunctionRegistry.register validateDatabaseConfiguration
    >> FunctionRegistry.register printStartup
    |> FunctionRegistry.build

let print: unit -> PrintExecuted = FunctionGraph.resolve graph
print () |> ignore

// ── FSharpOrDi Invalid configuration ───────────────────────────────────────────

printfn "\n=== Invalid configuration ===\n"

try
    ConfigurationBinding.registerBind<AppConfiguration> (configuration.GetSection("App"))
    >> ConfigurationBinding.registerBind<DatabaseConfiguration> (configuration.GetSection("InvalidDatabase"))
    >> FunctionRegistry.register validateDatabaseConfiguration
    >> FunctionRegistry.register printStartup
    |> FunctionRegistry.build
    |> ignore
with ex ->
    let message =
        match ex.InnerException with
        | null -> ex.Message
        | inner -> inner.Message

    printfn "Build failed as expected: %s" message
