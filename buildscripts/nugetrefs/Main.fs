module Main

open System.Net
open System.Text
open System.Web
open System

type StepPerSecData =
    { Current : float32
      Minimum : float32
      Maximum : float32
      Average : float32
    }

exception ConnectionException of unit

/// <summary>
/// Interaction between the web server and DServer.
/// Provides authentication, triggering server inputs...
/// </summary>
type Client(address : IPAddress, port, login, password) as this =
    inherit Sockets.TcpClient()

    do
        try
            base.Connect(address, port)
        with
        | exc ->
            printfn "Failed to connect to game server: %s" exc.Message
            raise(ConnectionException())
    let stream = this.GetStream()

    let send(buff, idx, len) = Async.FromBeginEnd((fun (cb, par) -> stream.BeginWrite(buff, idx, len, cb, par)), stream.EndWrite)
    let receive(buff, idx, len) = Async.FromBeginEnd((fun (cb, par) -> stream.BeginRead(buff, idx, len, cb, par)), stream.EndRead)

    let encode (cmd : string) =
        let asAscii = Encoding.ASCII.GetBytes(cmd)
        let length = [| byte((asAscii.Length + 1) % 256) ; byte((asAscii.Length + 1) / 256) |]
        let buff = Array.concat [length ; asAscii ; [|0uy|] ]
        buff

    let decodeResponse(response : string) =
        [
            for pair in response.Split('&') do
                match pair.Split('=') with
                | [| key; value |] -> yield (key, value)
                | _ -> failwithf "Could not split key-value pair %s" pair
        ]

    let getResponse(stream : Sockets.NetworkStream) =
        async {
            let buffer : byte[] = Array.zeroCreate 0xffff
            let response = stream.Read(buffer, 0, 2)
            let responseLength = (256 * int buffer.[1]) + int buffer.[0]
            let! response = receive(buffer, 2, responseLength)
            let data =
                if responseLength > 0 then
                    Encoding.ASCII.GetString(buffer, 2, responseLength - 1)
                else
                    ""
            return data
        }

    let parseFloat s =
        System.Single.Parse(s, System.Globalization.CultureInfo.InvariantCulture)

    member this.Auth() =
        async {
            let buff = encode <| sprintf "auth %s %s" login password
            do! send(buff, 0, buff.Length)
            let! response = getResponse stream
            return response
        }

    member this.ServerInput(name) =
        async {
            let buff =
                sprintf "serverinput %s" name
                |> encode
            do! send(buff, 0, buff.Length)
            let! response = getResponse stream
            return response
        }

    member this.MessagePlayer(playerId, msg) =
        async {
            let buff =
                sprintf "chatmsg 3 %d %s" playerId msg
                |> encode
            do! send(buff, 0, buff.Length)
            let! response = getResponse stream
            return response
        }

    member this.MessageTeam(teamId, msg) =
        async {
            let buff =
                sprintf "chatmsg 1 %d %s" teamId msg
                |> encode
            do! send(buff, 0, buff.Length)
            let! response = getResponse stream
            return response
        }

    member this.GetSPS() =
        async {
            let buff = encode "spsget"
            do! send(buff, 0, buff.Length)
            let! response = getResponse stream
            let decoded =
                decodeResponse response
                |> dict
            return
                { Current = decoded.["curSps"] |> parseFloat
                  Minimum = decoded.["minSps"] |> parseFloat
                  Maximum = decoded.["maxSps"] |> parseFloat
                  Average = decoded.["avgSps"] |> parseFloat
                }
        }

    member this.ResetSPS() =
        async {
            let buff = encode "spsreset"
            do! send(buff, 0, buff.Length)
            let! response = getResponse stream
            return response
        }

[<EntryPoint>]
let main argv =
    let login = "commander"
    let password = "com123"
    let period = 5000
    let reqKill = "ReqKill"
    use client =
        let rec f() =
            try
                new Client(IPAddress.Loopback, 8991, login, password)
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
            do! Async.Sleep(period)
            let! stepPerSec = client.GetSPS()
            if stepPerSec.Average < 49.0f && stepPerSec.Average > 0.0f then
                let! response = client.ServerInput(reqKill)
                printfn "Overload detected, %s sent, reponse: %s" reqKill response
            printfn "SPS: %4.1f" stepPerSec.Average
    }
    |> Async.RunSynchronously
    0