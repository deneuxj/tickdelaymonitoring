// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open InjectPrio.Build

type Options =
    { InputFilename : string
      OutputFilename : string
      PrimaryLanguage : string
      AdditionalLanguages : string list
      InstancesFilename : string
      ReqKill : string
    }

let rec tryParse args opt =
    match args with
    | [] -> opt
    | "-i" :: filename :: rest ->
        tryParse rest { opt with InputFilename = filename }
    | "-o" :: filename :: rest ->
        tryParse rest { opt with OutputFilename = filename }
    | "-l" :: languages :: rest ->
        let langs = languages.Split(',')
        match List.ofArray langs with
        | [] -> failwith "Missing list of languages after -l"
        | primary :: additional -> tryParse rest { opt with PrimaryLanguage = primary; AdditionalLanguages = additional }
    | "-x" :: filename :: rest ->
        tryParse rest { opt with InstancesFilename = filename }
    | "-k" :: reqKill :: rest ->
        tryParse rest { opt with ReqKill = reqKill }
    | other :: _ ->
        failwithf "Unknown argument '%s'" other

let validateOpts (opt : Options) =
    if not <| System.IO.File.Exists(opt.InputFilename) then
        printfn "Input mission file '%s' cannot be opened for reading" opt.InputFilename
        false
    elif not <| System.IO.File.Exists(opt.InstancesFilename) then
        printfn "Instance specification file '%s' cannot be opened for reading" opt.InstancesFilename
        false
    else
        true

let usage = """
Usage: InjectPrio.exe -i <input mission file> -o <output mission file>
                      -l <languages> -x <instance specification file>
                      -k <name of kill request MCU>

where <input mission file> is the path to an existing Mission file,
      <output mission file> is the path to a Mission file to be produced;
        existing file is replaced without warning, and language files with
        extensions .eng, .rus... are produced alongside.
      <languages> is a comma-separated of languages to be copied to the
        output mission file. The first language in the list is the
        language used to produce default files for those languages that
        are missions. For instance, "-l eng,fra" will copy English and
        French string files, and the English file will be used for the
        remaining languages (Russian, Polish, Spanish, German...).
      <instance specification file> is the path to a text file that lists
        nodes in the input mission file to which injected priority logic
        will be connected. See below for an example.
      <name of kill request MCU> is the name of the server input MCU
        in the input mission file.

Instance specification file example
-----------------------------------
Each non-blank line consists of up to five MCU names separated by commas.
Comment lines are allowed and start with #. Note that this means you cannot
use # or , in the names of your MCU nodes.
Here is an example of such a file

# kill, set no prio, set low prio, set medium prio, set high prio
# kill -> DecoKill, set low prio -> SartDeco, other fields unset
KillDeco1,,StartDeco1
KillDeco2,,StartDeco2
KillDeco3,,StartDeco3
"""

let rec tryParseInstances linenum (lines : string list) instances =
    match lines with
    | [] -> instances
    | comment :: rest when comment.StartsWith("#") ->
        tryParseInstances (linenum + 1) rest instances
    | empty :: rest when empty.Trim() = "" ->
        tryParseInstances (linenum + 1) rest instances
    | spec :: rest ->
        let fields = spec.Split(',')
        let tryGet n =
            if fields.Length > n then
                Some fields.[n]
            else
                None
        let kill, noPrio, lowPrio, medPrio, hiPrio =
            tryGet 0, tryGet 1, tryGet 2, tryGet 3, tryGet 4
        match kill with
        | None ->
            printfn "Line %d: Name of MCU must be provided for kill, missing in '%s'. Skipping" linenum spec
            tryParseInstances (linenum + 1) rest instances
        | Some kill ->
            tryParseInstances
                (linenum + 1)
                rest
                ({ Kill = kill; SetHighPrio = hiPrio; SetLowPrio = lowPrio; SetMedPrio = medPrio; SetNoPrio = noPrio } :: instances)


[<EntryPoint>]
let main argv =        
    let defaultOpts =
        { InputFilename = ""
          OutputFilename = ""
          PrimaryLanguage = "eng"
          AdditionalLanguages = []
          InstancesFilename = ""
          ReqKill = "ReqKill"
        }
    let opts =
        try
            tryParse (List.ofArray argv) defaultOpts
            |> Some
        with
        | exc ->
            printfn "Error: %s" exc.Message
            None
    match opts with
    | Some opts ->
        if not <| validateOpts opts then
            printfn "Version : %s" InjectPrio.AssemblyInfo.Constants.libraryVersion
            printfn "%s" usage
            1
        else
            let lines = System.IO.File.ReadAllLines(opts.InstancesFilename)
            let instances = tryParseInstances 1 (List.ofArray lines) [] |> List.rev
            build(opts.InputFilename, opts.OutputFilename, opts.PrimaryLanguage, opts.AdditionalLanguages, instances, opts.ReqKill)
            0
    | None ->
        printfn "Version : %s" InjectPrio.AssemblyInfo.Constants.libraryVersion
        printfn "%s" usage
        1