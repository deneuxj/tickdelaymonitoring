// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.


#I __SOURCE_DIRECTORY__

#r @"..\InjectPrio\bin\Release\InjectPrio.dll"

open InjectPrio.Build

// Names of MCUs that control starting and stopping vehicle groups
let instances =
    [
        for i in 1..3 do
            yield UnitPrioApi.CreateWithPath([sprintf "Bf110-%d" i], "Kill", setMedPrio = "MedPrio", setLowPrio = "LoPrio")
            yield UnitPrioApi.CreateWithPath([sprintf "IL2-%d" i], "Kill", setMedPrio = "MedPrio", setLowPrio = "LoPrio")
        for i in 1..3 do
            yield UnitPrioApi.CreateWithPath([sprintf "RiverPatrol-%d" i], "Kill", setMedPrio = "MedPrio", setLowPrio = "LoPrio")
    ]

let filename = __SOURCE_DIRECTORY__ + "/../../Krasnoarmeysk/Krasnoarmeysk.Mission"
let outputFilename = __SOURCE_DIRECTORY__ + "/../../Krasnoarmeysk-out.Mission"
build(filename, outputFilename, "eng", ["fra"], instances, "ReqKill")
