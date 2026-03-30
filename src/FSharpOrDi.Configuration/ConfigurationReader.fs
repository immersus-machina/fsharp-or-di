module internal FSharpOrDi.Configuration.ConfigurationReader

type ConfigurationReader =
    {
        GetValue: string -> string option
        GetSection: string -> ConfigurationReader
        GetChildren: unit -> ConfigurationReader list
    }
