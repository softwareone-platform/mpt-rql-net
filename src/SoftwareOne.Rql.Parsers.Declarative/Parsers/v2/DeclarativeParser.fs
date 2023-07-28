namespace SoftwareOne.Rql.Parsers.Declarative

open FParsec
open SoftwareOne.Rql.Abstractions
open SoftwareOne.Rql.Abstractions.Group

exception ParsingError of string

type DeclarativeParser() =
    interface IRqlParser with
        member this.Parse(x) = 
            match (Parser.Parse x) with
            | Success(x, _, _)   -> 
                let f = Interpreter.interpreteToRqlExpression x
                let group = match f with
                            | :? RqlGroup as x -> x
                            | x -> RqlExpression.Group("", x)
                group
            | Failure(errorMsg, _, _) -> raise (ParsingError(errorMsg))