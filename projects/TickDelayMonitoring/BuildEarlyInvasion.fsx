// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.


#I __SOURCE_DIRECTORY__

#r @"..\InjectPrio\bin\Release\InjectPrio.dll"

open InjectPrio.Build

// Names of MCUs that control starting and stopping vehicle groups
let instances =
    [
        for i in 1..3 do
            yield UnitPrioApi.Create(sprintf "KillBf109-%d" i, setHighPrio = sprintf "HiPrioBf109-%d" i, setMedPrio = sprintf "MedPrioBf109-%d" i)
            yield UnitPrioApi.Create(sprintf "KillLagg3-%d" i, setHighPrio = sprintf "HiPrioLagg3-%d" i, setMedPrio = sprintf "MedPrioLagg3-%d" i)
            yield UnitPrioApi.Create(sprintf "KillPz3-%d" i, setLowPrio = sprintf "StartPz3-%d" i)
        for i in 1..5 do
            yield UnitPrioApi.Create(sprintf "KillBf110-%d" i, setHighPrio = sprintf "StartBf110-%d" i, setMedPrio = sprintf "LowPrioBf110-%d" i)
        for i in 1..2 do
            yield UnitPrioApi.Create(sprintf "KillIL2-%d" i, setHighPrio = sprintf "StartIL2-%d" i, setMedPrio = sprintf "LowPrioIL2-%d" i)
        for i in 1..3 do
            yield UnitPrioApi.Create(sprintf "KillAfAAA-%d" i, setMedPrio = sprintf "StartAfAAA-%d" i, setLowPrio = sprintf "LowPrioAfAAA-%d" i)
            yield UnitPrioApi.Create(sprintf "KillAfAAA-%db" i, setMedPrio = sprintf "StartAfAAA-%d" i, setLowPrio = sprintf "LowPrioAfAAA-%db" i)
    ]

let filename = __SOURCE_DIRECTORY__ + "/../../EarlyInvasion-in.Mission"
let outputFilename = __SOURCE_DIRECTORY__ + "/../../EarlyInvasion-out.Mission"
build(filename, outputFilename, "eng", [], instances, "ReqKill")