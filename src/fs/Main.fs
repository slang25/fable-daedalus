﻿namespace Daedalus

open System
open System.Collections.Generic

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import

open Daedalus.Js
open Daedalus.Game

module Main =
    let [<Literal>] WORLD_WIDTH = 100
    let [<Literal>] WORLD_HEIGHT = 100
    let [<Literal>] ROOM_SIZE = 5
    let [<Literal>] DOOR_SIZE = 1

    let [<Literal>] ROOM_COLOR = "#fff"
    let [<Literal>] DOOR_COLOR = "#eee"

    let jq = importDefault<obj> "jquery"

    let toPixel (x, y) = x*ROOM_SIZE + (x + 1)*DOOR_SIZE, y*ROOM_SIZE + (y + 1)*DOOR_SIZE 

    let startAnimation canvas =
        let drawBox (color: string) (x: int) (y: int) (w: int) (h: int) =
            let rect = 
                createObj [ 
                    "fillStyle" ==> color; 
                    "x" ==> x; "y" ==> y; "width" ==> w; "height" ==> h; 
                    "fromCenter" ==> false 
                ]
            canvas ? drawRect(rect) |> ignore

        let drawRoom location =
            let x, y = toPixel location in drawBox ROOM_COLOR x y ROOM_SIZE ROOM_SIZE
            
        let drawDoor location direction =
            let x, y = toPixel location
            match direction with
            | North -> drawBox DOOR_COLOR x (y - DOOR_SIZE) ROOM_SIZE DOOR_SIZE
            | South -> drawBox DOOR_COLOR x (y + ROOM_SIZE) ROOM_SIZE DOOR_SIZE
            | East -> drawBox DOOR_COLOR (x + ROOM_SIZE) y DOOR_SIZE ROOM_SIZE
            | West -> drawBox DOOR_COLOR (x - DOOR_SIZE) y DOOR_SIZE ROOM_SIZE

        let mutable action = buildMaze WORLD_WIDTH WORLD_HEIGHT |> Enumerator.create

        let mutable cancel = id
        cancel <- Time.interval (1.0 / 60.0) (fun _ ->
            match action |> Option.map Enumerator.value with
            | None -> cancel ()
            | Some (InitAt location) -> 
                drawRoom location
            | Some (MoveTo (_, direction, location)) -> 
                drawRoom location
                drawDoor location (opposite direction)
            action <- action |> Option.bind Enumerator.next 
        )

        cancel

    let main () =
        printfn "version 4"

        (importDefault<obj> "jcanvas") $ (jq, Browser.window) |> ignore

        let w, h = toPixel (WORLD_WIDTH, WORLD_HEIGHT)
        let canvas = jq $ ("#canvas")
        canvas 
            ? attr("width", w) ? attr("height", h) 
            ? attr("viewbox", sprintf "0 0 %d %d" w h) 
            ? attr("viewport", sprintf "0 0 %d %d" w h) 
            |> ignore

        let mutable cancel = id
        let button = jq $ ("#restart")  
        button ? click(fun _ ->
            cancel ()
            canvas ? clearCanvas () |> ignore
            cancel <- startAnimation canvas
        ) |> ignore
