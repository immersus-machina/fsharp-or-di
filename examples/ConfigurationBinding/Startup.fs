module ConfigurationBinding.Startup

open ConfigurationBinding.Types

let printStartup (app: AppConfiguration) (db: DatabaseConfiguration) : unit -> PrintExecuted =
    fun () ->
        printfn "Startup summary:"
        printfn "  App:      %s (retries: %A, verbose: %b)" app.Name app.MaxRetries app.VerboseLogging
        printfn "  Database: %s:%A" db.Host db.Port
        PrintExecuted()
