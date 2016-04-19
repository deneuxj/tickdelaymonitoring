// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.


#I __SOURCE_DIRECTORY__

#r @"..\InjectPrio\bin\Release\InjectPrio.dll"

open InjectPrio.Build

// Names of MCUs that control starting and stopping vehicle groups
let instances =
    [
        for i in 1..3 do
            yield UnitPrioApi.Create(sprintf "KillDecoTraffic%d" i, setLowPrio = sprintf "StartDecoTraffic%d" i)
            yield UnitPrioApi.Create(sprintf "KillDecoTraffic%db" i, setLowPrio = sprintf "StartDecoTraffic%db" i)
        for i in 1..3 do
            yield UnitPrioApi.Create(sprintf "KillDeco%d" i, setLowPrio = sprintf "StartDeco%d" i)
        for i in 1..2 do
            yield UnitPrioApi.Create(sprintf "KillPe2-%d" i, setHighPrio = sprintf "StartPe2-%d" i, setLowPrio = sprintf "SetLowPrioPe2-%d" i)
        for i in 1..2 do
            yield UnitPrioApi.Create(sprintf "KillHe111-%d" i, setHighPrio = sprintf "StartHe111-%d" i, setLowPrio = sprintf "SetLowPrioHe111-%d" i)
        for i in 1..5 do
            yield UnitPrioApi.Create(sprintf "KillJu87-%d" i, setHighPrio = sprintf "StartJu87-%d" i, setLowPrio = sprintf "SetLowPrioJu87-%d" i)
        for i in 1..6 do
            yield UnitPrioApi.Create(sprintf "KillF4-%d" i, setMedPrio = sprintf "StartF4-%d" i)
        for i in 1..7 do
            yield UnitPrioApi.Create(sprintf "KillLagg3-%d" i, setMedPrio = sprintf "StartLagg3-%d" i)
        for i in 1..6 do
            yield UnitPrioApi.Create(sprintf "Kill%d" i, setHighPrio = sprintf "Start%d" i)
            yield UnitPrioApi.Create(sprintf "Kill%db" i, setHighPrio = sprintf "Start%db" i)
            yield UnitPrioApi.Create(sprintf "Kill%dc" i, setHighPrio = sprintf "Start%dc" i)
        yield UnitPrioApi.Create("EvacKill", setNoPrio = "EvacStopped", setLowPrio = "EvacStarted")
        for i in 1..3 do
            yield UnitPrioApi.CreateWithPath([sprintf "PatrolGer%d" i], "Despawn", setLowPrio = "Spawn")
            yield UnitPrioApi.CreateWithPath([sprintf "PatrolRus%d" i], "Despawn", setLowPrio = "Spawn")
    ]

let filename = __SOURCE_DIRECTORY__ + "/../../NightAttackOnKalach/NightAttackOnKalach-in.Mission"
let outputFilename = __SOURCE_DIRECTORY__ + "/../../NightAttackOnKalach-out.Mission"
build(filename, outputFilename, "eng", ["fra"], instances, "ReqKill")