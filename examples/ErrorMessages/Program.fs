open ErrorMessages
open ErrorMessages.Functions
open FSharpOrDi.FunctionRegistry

printfn "FSharpOrDi — Error Message Examples"
printfn "====================================\n"

printfn "1. Duplicate Registration"
printfn "-------------------------"

try
    empty
    |> register readTemperature
    |> register readTemperatureAlternate
    |> ignore
with ex ->
    printfn "%s\n" ex.Message

printfn "2. Missing Dependency"
printfn "---------------------"

try
    let registry =
        empty
        |> register readTemperature
        |> register readHumidity
        |> register combineSensors
        |> register combineWeather

    resolve<SensorId -> CombinedReading> registry |> ignore
with ex ->
    printfn "%s\n" ex.Message

printfn "3. Ambiguous Resolution (partial application)"
printfn "----------------------------------------------"

try
    let registry =
        empty
        |> register makeRouteA
        |> register makeRouteB
        |> register viaRouteA
        |> register viaRouteB

    resolve<SensorId -> FinalResult> registry |> ignore
with ex ->
    printfn "%s\n" ex.Message

printfn "4. Ambiguous Composition"
printfn "------------------------"

try
    let registry =
        empty
        |> register stepOne
        |> register chainViaA
        |> register chainViaB
        |> register finishFromA
        |> register finishFromB

    resolveComposed<SensorId -> FinalResult> registry |> ignore
with ex ->
    printfn "%s\n" ex.Message

printfn "5. Cycle Detection"
printfn "-------------------"

try
    let registry =
        empty
        |> register produceA
        |> register produceB
        |> register viaRouteA

    resolve<SensorId -> FinalResult> registry |> ignore
with ex ->
    printfn "%s\n" ex.Message
