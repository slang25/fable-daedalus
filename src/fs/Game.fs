namespace Daedalus

open System
open System.Collections.Generic

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import

open Fable.Helpers.Virtualdom
open Fable.Helpers.Virtualdom.App
open Fable.Helpers.Virtualdom.Html

open Daedalus.Js

module Game =
    type Coords = int * int
    type Direction = | North | East | South | West

    type Action =
        | InitAt of Coords
        | MoveTo of Coords * Direction * Coords

    type Room = 
        {
            Position: Coords
            mutable Visited: bool
            mutable Exits: Direction list
        }

    type World = 
        {
            Size: Coords
            Rooms: Room[][]
        }

    let newRoom x y = { 
        Position = (x, y)
        Visited = false
        Exits = []
    }

    let createRooms width height =
        Array.init height (fun y -> Array.init width (fun x -> newRoom x y))

    let newWorld width height = {
        Size = (width, height)
        Rooms = createRooms width height 
    }

    let opposite direction = 
        match direction with 
        | North -> South | South -> North 
        | West -> East | East -> West

    let buildMaze x y (world: World) = 
        let w, h = world.Size

        let valid (x, y) = x >= 0 && y >= 0 && x < w && y < h

        let shift direction (x, y) =
            match direction with
            | North -> (x, y - 1) | South -> (x, y + 1) 
            | East -> (x + 1, y) | West -> (x - 1, y)
            |> Some |> Option.filter valid

        let sourcexy action = 
            match action with | MoveTo (xy, _, _) -> Some xy | _ -> None 

        let targetxy action = 
            match action with | InitAt xy -> xy | MoveTo (_, _, xy) -> xy

        let next location direction = 
            location |> shift direction |> Option.map (fun xy -> MoveTo (location, direction, xy))

        let fanout action = 
            [| West; North; East; South |] |> Array.choose (action |> targetxy |> next)

        let encode (x, y) = y*w + x 

        let visited = HashSet()
        let mark = targetxy >> encode >> visited.Add >> ignore
        let test = targetxy >> encode >> visited.Contains
        let path = InitAt (x, y) |> DFS.traverse mark test (fanout >> Array.shuffle)

        let updateSource action (rooms: Room[][]) =
            match action with
            | InitAt _ -> ()
            | MoveTo ((x, y), direction, _) ->
                let room = rooms.[y].[x] 
                room.Exits <- direction :: room.Exits 

        let updateTarget action (rooms: Room[][]) =
            match action with
            | InitAt (x, y) ->
                let room = rooms.[y].[x]
                room.Visited <- true 
            | MoveTo (_, direction, (x, y)) ->
                let room = rooms.[y].[x]
                room.Visited <- true
                room.Exits <- (opposite direction) :: room.Exits

        seq {
            yield world
            for action in path do
                world.Rooms |> updateSource action
                world.Rooms |> updateTarget action
                yield world
        }
