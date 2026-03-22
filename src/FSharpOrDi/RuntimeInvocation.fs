module internal FSharpOrDi.RuntimeInvocation

let private findInvokeMethod (instance: obj) =
    instance.GetType().GetMethods()
    |> Array.tryFind (fun method -> method.Name = "Invoke" && method.GetParameters().Length = 1)
    |> Option.defaultWith (fun () ->
        failwithf "Expected an F# function but received %s" (instance.GetType().Name))

let applyFunction (functionImplementation: obj) (argumentImplementation: obj) : obj =
    let invokeMethod = findInvokeMethod functionImplementation
    invokeMethod.Invoke(functionImplementation, [| argumentImplementation |])

let composeFunctions
    (makeFSharpFunction: System.Type -> (obj -> obj) -> obj)
    (firstImplementation: obj)
    (secondImplementation: obj)
    (composedSystemType: System.Type)
    : obj =
    let firstInvoke = findInvokeMethod firstImplementation
    let secondInvoke = findInvokeMethod secondImplementation

    makeFSharpFunction composedSystemType (fun argument ->
        let intermediate = firstInvoke.Invoke(firstImplementation, [| argument |])
        secondInvoke.Invoke(secondImplementation, [| intermediate |]))
