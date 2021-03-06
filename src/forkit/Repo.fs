﻿namespace forkit

open Xna
open Microsoft.Xna.Framework
open Movement

/// Each 'tile' in the repository represents a commit.
type Commit =     
    | GrowingCommit of Point
    | BigCommit of Point
    | ShrinkingCommit of Point
    | SmallCommit of Point

/// Commits exist within a branch.
type Branch = {
    Commits: (Commit * float)list
}

/// The player takes the role of a git repository (so this is essentially the 
/// 'hero' of the game, that the player is helping.
type Repository = {
    Branches: Branch list
}

[<RequireQualifiedAccess>]
module Repo =     

    let private updatetime = 75.0

    /// Creates a new commit in the 'growing' state.
    let growingCommit x y = GrowingCommit(point x y)
    
    /// Creates a new commit in the 'big' state.
    let bigCommit x y = BigCommit(point x y)
    
    /// Creates a new commit in the 'shrinking' state.
    let shrinkingCommit x y = ShrinkingCommit(point x y)
    
    /// Creates a new commit in the 'small' state.
    let smallCommit x y = SmallCommit(point x y)

    /// Creates a new branch from a seeding commit.
    let branch seed position =         
        let commit (p:Point) = 
            match seed % 4 with
            | 1 -> growingCommit p.X p.Y, 0.0 
            | 2 -> bigCommit p.X p.Y, 0.0
            | 3 -> shrinkingCommit p.X p.Y, 0.0
            | 0 -> smallCommit p.X p.Y, 0.0
        { Commits = [commit position] }

    /// Initializes a new Repository at a specified set of coordinates.
    let init x y = {
        Branches = 
            [ branch 5 (point x y)
              branch 4 (point x (y + 11))
              branch 4 (point x (y + 22))
              branch 3 (point x (y + 33))
              branch 3 (point x (y + 44))
              branch 2 (point x (y + 55))
              branch 2 (point x (y + 66))
              branch 1 (point x (y + 77))
              branch 1 (point x (y + 88))
              branch 0 (point x (y + 99)) ]
    }

    /// Deconstructs a commit into the point, and the appropriate
    /// function to reconstruct it.
    let revertWithRecommit commit =
        match commit with
        | GrowingCommit p -> (p, growingCommit)
        | BigCommit p -> (p, bigCommit)
        | ShrinkingCommit p -> (p, shrinkingCommit)
        | SmallCommit p -> (p, smallCommit)

    /// Deconstructs a commit to get the point
    let revert commit =
        let r, f = revertWithRecommit commit 
        r        

    /// Move's a commit off to the left.
    /// TODO: Needs some refactoring, lets move this matching and time counting out.
    let moveCommit (gametime:GameTime) (commit, time) =

        let timesofar = time + (gametime.ElapsedGameTime.TotalMilliseconds)

        if timesofar > updatetime then 
            let point, f = revertWithRecommit commit
            f (point.X - 9) point.Y, 0.0
        else 
            commit, timesofar
          
    /// Pushes a new commit onto each of the branches at the head. Each new
    /// commit pushed is a different size to try and get a ripple effect.
    /// TODO: Needs some refactoring, lets move this matching and time counting out.
    let push (gametime:GameTime) movement commits =      
        let timesofar time = time + (gametime.ElapsedGameTime.TotalMilliseconds)              
        
        let applyMovement y = 
            match movement with
            | Some upOrDown ->
                match upOrDown with
                | Up -> y - 2
                | Down -> y + 2
            | None -> y

        match List.head commits with
        | GrowingCommit p, t ->
            if timesofar t > updatetime
                then (bigCommit (p.X + 11) (applyMovement p.Y), 0.0) :: commits
                else List.map(fun (c, _) -> c, timesofar t) commits
        | BigCommit p, t ->
            if timesofar t > updatetime
                then (shrinkingCommit (p.X + 9) (applyMovement p.Y), 0.0) :: commits
                else List.map(fun (c, _) -> c, timesofar t) commits
        | ShrinkingCommit p, t ->
            if timesofar t > updatetime
                then (smallCommit (p.X + 7) (applyMovement p.Y), 0.0) :: commits
                else List.map(fun (c, _) -> c, timesofar t) commits
        | SmallCommit p, t ->
            if timesofar t > updatetime
                then (growingCommit (p.X + 9) (applyMovement p.Y), 0.0) :: commits
                else List.map(fun (c, _) -> c, timesofar t) commits

    /// Pops all commits in a list that are "off screen".
    let pop (gametime:GameTime) commits = 
        let timesofar time = time + (gametime.ElapsedGameTime.TotalMilliseconds)    

        Seq.map(fun ((c:Commit), (t:float)) ->
            if (revert c).X < -20 then false, c, t
            else true, c, t
        ) commits
        |> Seq.filter(fun (keepme, c, t) -> keepme)
        |> Seq.map(fun (keepme, c, t) -> c, t)
        |> List.ofSeq


    /// Moves all the commits on a branch and pushes a new one onto the head and
    /// pops one at the far end of the tail (off screen).
    let moveBranch gametime movement branch = 
        let commits = 
            Seq.map (moveCommit gametime) branch.Commits
            |> (pop gametime)
            |> (push gametime movement)
        { branch with Commits = commits }
      
    /// Moves all the branches in a repo.  
    let moveRepo gametime movement repo =         
        { repo with Branches = List.map (moveBranch gametime movement) repo.Branches }

    /// Draw a single commit to a spritebatch.
    let drawCommit spritebatch (gametime:GameTime) texture (commit, time) = 
        
        let drawone destination color = 
            SpriteBatch.draw spritebatch texture destination (rect 0 0 8 8) color        
        
        match commit with
        | GrowingCommit p ->            
            drawone (rect p.X p.Y 8 8) (Color.GreenYellow * float32 1.05)
        | BigCommit p ->            
            drawone (rect (p.X - 1) (p.Y - 1) 10 10) (Color.GreenYellow * float32 1.25)
        | ShrinkingCommit p ->            
            drawone (rect (p.X) (p.Y) 8 8) (Color.GreenYellow * float32 1.00)
        | SmallCommit p ->            
            drawone (rect (p.X + 1) (p.Y + 1) 6 6) (Color.GreenYellow * float32 0.75)

    /// Draw a single branch to a spritebatch.
    let drawBranch spritebatch gametime texture branch =         
        branch.Commits |> Seq.iter (drawCommit spritebatch gametime texture) 

    /// Draw the entire repo to a spritebatch.
    let draw spritebatch gametime texture repo =         
        repo.Branches|> Seq.iter (drawBranch spritebatch gametime texture)