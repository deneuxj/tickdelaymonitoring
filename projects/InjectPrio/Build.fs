// InjectPrio Copyright (C) 2016 Johann Deneux <johann.deneux@gmail.com>
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.

module InjectPrio.Build

// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.

open SturmovikMissionTypes
open SturmovikMission
open SturmovikMission.DataProvider
open SturmovikMission.DataProvider.Mcu
open SturmovikMission.DataProvider.McuUtil
open SturmovikMission.DataProvider.NumericalIdentifiers
open System.Text.RegularExpressions
open System.IO

type T = Provider<"Sample.Mission", "ResourceDespawn.Group">

type UnitPrioApi =
    { SetHighPrio : string option
      SetLowPrio : string option
      SetMedPrio : string option
      SetNoPrio : string option
      Kill : string
    }
with
    static member Create(kill, ?setHighPrio, ?setLowPrio, ?setMedPrio, ?setNoPrio) =
        { SetHighPrio = setHighPrio
          SetLowPrio = setLowPrio
          SetMedPrio = setMedPrio
          SetNoPrio = setNoPrio
          Kill = kill
        }

let allLanguages = [ "eng"; "ger"; "pol"; "rus" ; "spa" ; "fra" ]

let build(filename, outputFilename, primaryLanguage, additionalLanguages, instances, reqKillName) =
    // The stores that are responsible for providing collision-free new identifiers.
    /// MCU Index store.
    let idStore = new IdStore()
    /// Localized string index store.
    let lcIdStore = new IdStore()
    lcIdStore.SetNextId(3) // 0, 1, 2 reserved for mission title, briefing and author

    /// Convenience function that creates id allocators and assigns fresh ids to a given sequence of MCUs.
    let subst (mcus : #McuBase seq) =
        let getNewId = idStore.GetIdMapper()
        let getNewLcId = lcIdStore.GetIdMapper()
        for mcu in mcus do
            substId getNewId mcu
            substLCId getNewLcId mcu
        getNewLcId

    let mission =
        try
            T.GroupData(Parsing.Stream.FromFile (filename))
        with
        | :? Parsing.ParseError as e ->
            Parsing.printParseError e
            |> String.concat "\n"
            |> printfn "%s"
            failwith "Parse error"
    let mission = mission.CreateMcuList()
    let getLcId0 = subst mission
    let getLcId x =
        if x < 3 then x
        else getLcId0 x
    let missionLcStrings =
        primaryLanguage :: additionalLanguages
        |> List.map (fun language -> language, Localization.transfer true getLcId (Path.ChangeExtension(filename, language)))
        |> dict

    // Create blocks, connect to each vehicle group
    let blocks =
        [
            for api in instances do
                let exists =
                    try
                        getCommandByName api.Kill mission |> ignore
                        true
                    with
                    | _ -> false
                if exists then
                    let mcuKill = getCommandByName api.Kill mission
                    let mcuLowPrio =
                        api.SetLowPrio
                        |> Option.map (fun name -> getCommandByName name mission)
                    let mcuMedPrio =
                        api.SetMedPrio
                        |> Option.map (fun name -> getCommandByName name mission)
                    let mcuHighPrio =
                        api.SetHighPrio
                        |> Option.map (fun name -> getCommandByName name mission)
                    let block = T.ResourceDespawn.CreateMcuList()
                    subst block |> ignore
                    let onKilled = getCommandByName "OnKilled" block
                    let setHighPrio = getCommandByName "SetHighPrio" block
                    let setMedPrio = getCommandByName "SetMedPrio" block
                    let setLowPrio = getCommandByName "SetLowPrio" block
                    addTargetLink onKilled mcuKill.Index
                    mcuLowPrio |> Option.iter (fun mcu -> addTargetLink mcu setLowPrio.Index)
                    mcuMedPrio |> Option.iter (fun mcu -> addTargetLink mcu setMedPrio.Index)
                    mcuHighPrio |> Option.iter (fun mcu -> addTargetLink mcu setHighPrio.Index)
                    yield block
                else
                    printfn "%s does not exist" api.Kill
        ]
    // Connect blocks to form a queue
    for curr, next in Seq.pairwise blocks do
        let f level =
            let onNext = getCommandByName (sprintf "OnNext%s" level) curr
            let reqNext = getCommandByName (sprintf "ReqKill%s" level) next
            addTargetLink onNext reqNext.Index
        f "Low"
        f "Med"
        f "High"
    // Wrap-around
    let first, last = List.head blocks, Seq.last blocks
    let f level1 =
        let level2 =
            match level1 with
            | "Low" -> "Med"
            | "Med" -> "High"
            | _ -> failwith "Bad level1"
        let onNext = getCommandByName (sprintf "OnNext%s" level1) last
        let reqNext = getCommandByName (sprintf "ReqKill%s" level2) first
        addTargetLink onNext reqNext.Index
    f "Low"
    f "Med"
    // Server input -> ReqKillLow of first
    let serverInput = getCommandByName reqKillName mission
    let reqKill = getCommandByName "ReqKillLow" first
    addTargetLink serverInput reqKill.Index

    // Write mission file
    using (File.CreateText(Path.ChangeExtension(outputFilename, ".Mission"))) (fun file ->
        file.WriteLine "# Mission File Version = 1.0;"
        file.WriteLine ""
        let options =
            let parser = T.Parser()
            try
                let s = Parsing.Stream.FromFile(filename)
                match s with
                | Parsing.ReLit "Options" s ->
                    parser.Parse_Options(s)
                | _ ->
                    Parsing.parseError("Expected 'Options'", s)
            with
            | :? Parsing.ParseError as err ->
                let msg = Parsing.printParseError err
                eprintfn "Parse error: %s" (String.concat "\n" msg)
                raise err
            |> fst
        file.Write(options.AsString())
        file.WriteLine ""
        let missionStr =
            mission
            |> McuUtil.moveEntitiesAfterOwners
            |> Seq.map (fun mcu -> mcu.AsString())
            |> String.concat "\n"
        file.Write(missionStr)
        for block in blocks do
            let blockStr =
                block
                |> McuUtil.moveEntitiesAfterOwners
                |> Seq.map (fun mcu -> mcu.AsString())
                |> String.concat "\n"
            file.Write(blockStr)
        file.WriteLine ""
        file.WriteLine "# end of file"
    )

    let createLcFile filename allLcStrings =
        use file = new StreamWriter(filename, false, System.Text.UnicodeEncoding(false, true))
        for (idx, s) in allLcStrings do
            file.WriteLine(sprintf "%d:%s" idx s)

    let allLcStrings = missionLcStrings

    for lang in [ "eng"; "ger"; "pol"; "rus" ; "spa" ; "fra" ] do
        match allLcStrings.TryGetValue(lang) with
        | true, lcStrings ->
            lcStrings
        | false, _ ->
            allLcStrings.["eng"]
        |> createLcFile (Path.ChangeExtension(outputFilename, lang))

