namespace nugetrefs

// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.

open SturmovikMissionTypes
open SturmovikMission
open SturmovikMission.DataProvider
open SturmovikMission.DataProvider.Mcu
open SturmovikMission.DataProvider.McuUtil
open SturmovikMission.DataProvider.NumericalIdentifiers
open System.Text.RegularExpressions

type T = Provider<"Sample.Mission", "Sample.Mission">


type Class1() = 
    member this.X = "F#"
