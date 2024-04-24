// For more information see https://aka.ms/fsharp-console-apps
open Suave
open Suave.Operators
open Suave.Filters
open Suave.Successful
open System
open Newtonsoft.Json
open Suave.Writers

type TZInfo = {tzName: string; minDiff: float; localTime: string; utcOffset: float}

let getClosest () = 

    // This gets all the time zones into a List-like object
    let tzs = TimeZoneInfo.GetSystemTimeZones()

    // List comprehension + type inference allows us to easily perform conversions
    let tzList = [

        for tz in tzs do

        // convert the current time to the local time zone
        let localTz = TimeZoneInfo.ConvertTime(DateTime.Now, tz) 

        // Get the datetime object if it was 5:00pm 
        let fivePM = DateTime(localTz.Year, localTz.Month, localTz.Day, 17, 0, 0)

        // Get the difference between now local time and 5:00pm local time.
        let minDifference = (localTz - fivePM).TotalMinutes

        yield {

                tzName=tz.StandardName;

                minDiff=minDifference;

                localTime=localTz.ToString("hh:mm tt");

                utcOffset=tz.BaseUtcOffset.TotalHours;
             }
    ]

    tzList 

        // filter so that we only get tz after 5pm
        |> List.filter (fun (i:TZInfo) -> i.minDiff >= 0.0) 

        // sort by minDiff
        |> List.sortBy (fun (i:TZInfo) -> i.minDiff) 

        // Get the first item
        |> List.head

let runWebServer (argv:string[]) = 
    // Define the port where you want to serve. We'll hardcode this for now.
    let port = if argv.Length = 0 then 8080 else (int argv.[0])

    // create an app config with the port
    let cfg =
          { defaultConfig with
              bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" port]}

    let app =
          choose
            [ GET >=> choose
                [ 
                    // We are getting the closest time zone, converting it to JSON, then setting the MimeType
                    path "/" >=> request (fun _ -> OK <| JsonConvert.SerializeObject(getClosest()))
                                >=> setMimeType "application/json; charset=utf-8"
                ]
            ]

    // Now we start the server
    printfn "Starting server on http://localhost:%d" port
    startWebServer cfg app




[<EntryPoint>]

let main argv = 
    runWebServer argv
    printfn "%s" <| JsonConvert.SerializeObject(getClosest())

    0