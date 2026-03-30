module FSharpOrDi.Configuration.Test.TestReaders

open FSharpOrDi.Configuration.ConfigurationReader

let internal emptySubReader =
    {
        GetValue = fun _ -> None
        GetSection = fun _ -> failwith "no deeper sections"
        GetChildren = fun () -> []
    }

let internal emptyReader =
    {
        GetValue = fun _ -> None
        GetSection = fun _ -> emptySubReader
        GetChildren = fun () -> []
    }
