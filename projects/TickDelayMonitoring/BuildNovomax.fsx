// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.


#I __SOURCE_DIRECTORY__

#r @"..\InjectPrio\bin\Release\InjectPrio.dll"

open InjectPrio.Build

// Names of MCUs that control starting and stopping vehicle groups
let instances =
    [
        for i in 1..4 do
            yield UnitPrioApi.Create(sprintf "KillAA-%d" i, setMedPrio = sprintf "MedPrioAA-%d" i, setLowPrio = sprintf "LoPrioAA-%d" i)
        let i = 1 in
            yield UnitPrioApi.Create(sprintf "KillBf109-%d" i, setHighPrio = sprintf "StartBf109-%d" i, setLowPrio = sprintf "LowPrioBf109-%d" i)
        for i in 1..2 do
            yield UnitPrioApi.Create(sprintf "KillStuka-%d" i, setMedPrio = sprintf "StartStuka-%d" i, setLowPrio = sprintf "LowPrioStuka-%d" i)
        let i = 1 in
            yield UnitPrioApi.Create(sprintf "KillT70-%d" i, setHighPrio = sprintf "StartT70-%d" i)
        for i in 1..3 do
            yield UnitPrioApi.Create(sprintf "KillPz3-%d" i, setHighPrio = sprintf "StartPz3-%d" i)
        let i = 1 in
            yield UnitPrioApi.Create(sprintf "KillConvoy-%d" i, setHighPrio = sprintf "StartConvoy-%d" i, setLowPrio = sprintf "LowPrioConvoy-%d" i)
        let i = 1 in
            yield UnitPrioApi.Create(sprintf "KillBm13-%d" i, setHighPrio = sprintf "StartBm13-%d" i, setLowPrio = sprintf "LowPrioBm13-%d" i)
        let i = 1 in
            yield UnitPrioApi.Create(sprintf "KillBf110-%d" i, setHighPrio = sprintf "StartBf110-%d" i, setLowPrio = sprintf "LowPrioBf110-%d" i)
        let i = 1 in
            yield UnitPrioApi.Create(sprintf "KillIL2-%d" i, setHighPrio = sprintf "StartIL2-%d" i, setLowPrio = sprintf "LowPrioIL2-%d" i)
    ]

let filename = __SOURCE_DIRECTORY__ + "/../../Novomax/Novomax-in.Mission"
let outputFilename = __SOURCE_DIRECTORY__ + "/../../Novomax-out.Mission"
build(filename, outputFilename, "eng", [], instances, "ReqKill")