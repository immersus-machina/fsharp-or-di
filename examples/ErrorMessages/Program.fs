open ErrorMessages
open ErrorMessages.Functions
open FSharpOrDi.FunctionRegistry
open FSharpOrDi.FunctionGraph

printfn "FSharpOrDi — Error Message Examples"
printfn "====================================\n"

printfn "1. Duplicate Registration"
printfn "-------------------------"

try
    register readTemperature
    >> register readTemperatureAlternate
    |> build
    |> ignore
with ex ->
    printfn "%s\n" ex.Message

printfn "2. Missing Dependency"
printfn "---------------------"

try
    let graph =
        register readTemperature
        >> register readHumidity
        >> register combineSensors
        >> register combineWeather
        |> build

    resolve<SensorId -> CombinedReading> graph |> ignore
with ex ->
    printfn "%s\n" ex.Message

printfn "3. Ambiguous Resolution (partial application)"
printfn "----------------------------------------------"

try
    register makeRouteA
    >> register makeRouteB
    >> register viaRouteA
    >> register viaRouteB
    |> build
    |> ignore
with ex ->
    printfn "%s\n" ex.Message

printfn "4. Ambiguous Composition"
printfn "------------------------"

try
    register stepOne
    >> register chainViaA
    >> register chainViaB
    >> register finishFromA
    >> register finishFromB
    |> buildComposed
    |> ignore
with ex ->
    printfn "%s\n" ex.Message

printfn "5. Cycle Detection"
printfn "-------------------"

try
    register produceA
    >> register produceB
    >> register viaRouteA
    |> build
    |> ignore
with ex ->
    printfn "%s\n" ex.Message
