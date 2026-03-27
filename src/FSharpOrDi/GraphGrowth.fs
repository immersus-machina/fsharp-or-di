module internal FSharpOrDi.GraphGrowth

open ResolutionGraph
open GrowthPlan

let growFromRegistrations
    (growthPlans: GrowthPlan list)
    (stageValidator: Stage -> Result<unit, string>)
    (filterCandidateAgainstExistingStage: Stage -> Node -> Node option)
    (deduplicateBatch: Node list -> Node list)
    (registrationStage: Stage)
    : Stage =

    let rec growUntilStable (question: GrowthQuestion) : Stage =
        match stageValidator question.CurrentStage with
        | Error message -> failwith message
        | Ok() ->
            let acceptedNodes =
                growthPlans
                |> List.collect (fun plan -> plan question)
                |> List.choose (filterCandidateAgainstExistingStage question.CurrentStage)
                |> deduplicateBatch

            if List.isEmpty acceptedNodes then
                // Safe to return: current stage was validated at the start of this iteration
                question.CurrentStage
            else
                let nextStage =
                    acceptedNodes
                    |> List.fold (fun stageAccumulator node -> addNode node stageAccumulator) question.CurrentStage

                growUntilStable { NewNodes = acceptedNodes; CurrentStage = nextStage }

    let initialNodes = allNodes registrationStage
    growUntilStable { NewNodes = initialNodes; CurrentStage = registrationStage }
