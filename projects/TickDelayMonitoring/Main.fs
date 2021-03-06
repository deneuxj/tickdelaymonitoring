﻿// TickDelayMonitoring Copyright (C) 2016 Johann Deneux <johann.deneux@gmail.com>
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

module Main

open System.Net
open System.Text
open System.Web
open System
open RConClient

type Options =
    { Login : string
      Password : string
      Period : int
      ReqKill : string
      Address : string
      Port : int
    }
with
    static member Default =
        { Login = "commander"
          Password = "com123"
          Period = 5000
          ReqKill = "ReqKill"
          Address = "127.0.0.1"
          Port = 8991
        }

let rec tryParse args opts =
    match args with
    | [] -> Some opts
    | "-l" :: login :: rest ->
        tryParse rest { opts with Login = login }
    | "-p" :: password :: rest ->
        tryParse rest { opts with Password = password }
    | "-f" :: period :: rest ->
        match System.Int32.TryParse(period) with
        | true, period ->
            tryParse rest { opts with Period = period }
        | false, _ ->
            printfn "Not a valid integer '%s'" period
            None
    | "-q" :: reqKill :: rest ->
        tryParse rest { opts with ReqKill = reqKill }
    | "-a" :: addr :: port :: rest ->
        match System.Int32.TryParse(port) with
        | (true, port)  ->
            tryParse rest { opts with Address = addr; Port = port }
        | (false, _) ->
            printfn "Not a valid port number '%s'" port
            None
    | other :: _ ->
        printfn "Unknown parameter '%s'" other
        None

let usage = """
Usage: TickDelayMonitoring.exe -l <login> -p <password> [-f <update period>]
                               [-q <kill request MCU>] [-a <ip address> <port>]

where <login> is the login to DServer's Remote Console control
      <password> is the password to DServer's Remote Console control
      <update period> is the update period in milliseconds
        Default: 5000
      <kill request MCU> is the name of the server input MCU to trigger when
        heavy load is detected.
        Default: ReqKill
      <ip address> is the IP address of DServer (where listening for Remote
        Console)
        Default: 127.0.0.1
      <port> is the port number where DServer is listening for Remote Console
        Default: 8991
"""

[<EntryPoint>]
let main argv =
    match tryParse (List.ofArray argv) Options.Default with
    | Some opts ->
        async {
            use queue = new ClientMessageQueue(opts.Address, opts.Port, opts.Login, opts.Password)
            // Run for ever. This is a console tool, and to close it the user should simply close the window.
            while true do
                let! response = queue.Run(lazy queue.Client.ResetSPS())
                match response with
                | Some _ ->
                    do! Async.Sleep(opts.Period)
                    let! stepPerSec = queue.Run(lazy queue.Client.GetSPS())
                    match stepPerSec with
                    | Some stepPerSec ->
                        if stepPerSec.Average < 49.0f && stepPerSec.Minimum > 0.0f then
                            let! response = queue.Run(lazy queue.Client.ServerInput(opts.ReqKill))
                            printfn "Overload detected, %s sent, reponse: %A" opts.ReqKill response
                        printfn "SPS: %4.1f" stepPerSec.Average
                    | None ->
                        ()
                | None ->
                    // Error when reseting SPS count, sleeping for a minute.
                    // Happens e.g. when the server is shut down.
                    do! Async.Sleep(60000)
        }
        |> Async.RunSynchronously
        0
    | None ->
        printfn "Version: %s" TickDelayMonitoring.AssemblyInfo.Constants.version
        printfn "%s" usage
        1