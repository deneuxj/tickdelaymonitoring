// TickDelayMonitoring Copyright (C) 2016 Johann Deneux <johann.deneux@gmail.com>
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
      Address : IPAddress
      Port : int
    }
with
    static member Default =
        { Login = "commander"
          Password = "com123"
          Period = 5000
          ReqKill = "ReqKill"
          Address = IPAddress.Loopback
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
    | "-a" :: ipaddr :: port :: rest ->
        match IPAddress.TryParse(ipaddr), System.Int32.TryParse(port) with
        | (true, ipaddr), (true, port)  ->
            tryParse rest { opts with Address = ipaddr; Port = port }
        | (false, _), _ ->
            printfn "Not a valid ip address '%s'" ipaddr
            None
        | _, (false, _) ->
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
        use client =
            let rec f() =
                try
                    new Client(opts.Address, opts.Port, opts.Login, opts.Password)
                with
                | :? ConnectionException ->
                    printfn "Failed to connect to DServer. Retry in 10s."
                    System.Threading.Thread.Sleep(10000)
                    f()
            f()
        async {
            let! response = client.Auth()
            printfn "Auth response: %s" response
            while true do
                let! response = client.ResetSPS()
                do! Async.Sleep(opts.Period)
                let! stepPerSec = client.GetSPS()
                if stepPerSec.Average < 49.0f && stepPerSec.Minimum > 0.0f then
                    let! response = client.ServerInput(opts.ReqKill)
                    printfn "Overload detected, %s sent, reponse: %s" opts.ReqKill response
                printfn "SPS: %4.1f" stepPerSec.Average
        }
        |> Async.RunSynchronously
        0
    | None ->
        printfn "Version: %s" TickDelayMonitoring.AssemblyInfo.Constants.version
        printfn "%s" usage
        1