namespace SoftwareOne.Rql.Parsers.Declarative
open System
open Ast
open SoftwareOne.Rql.Abstractions

module Interpreter =

    let rec interpreteToRqlExpression el = 
        let groupValues (e:Value list): RqlExpression list = e |> List.map (fun x -> RqlExpression.Constant(x))
        let r:RqlExpression = 
            match el with
                | QEq e -> RqlExpression.Equal(RqlExpression.Constant(e.Key), RqlExpression.Constant(e.Value))
                | QGe e -> RqlExpression.GreaterThanOrEqual(RqlExpression.Constant(e.Key), RqlExpression.Constant(e.Value))
                | QNeq e -> RqlExpression.NotEqual(RqlExpression.Constant(e.Key), RqlExpression.Constant(e.Value))
                | QGt e -> RqlExpression.GreaterThan(RqlExpression.Constant(e.Key), RqlExpression.Constant(e.Value))
                | QLt e -> RqlExpression.LessThan(RqlExpression.Constant(e.Key), RqlExpression.Constant(e.Value))
                | QLe e -> RqlExpression.LessThanOrEqual(RqlExpression.Constant(e.Key), RqlExpression.Constant(e.Value))
                | QLike e -> RqlExpression.Like(RqlExpression.Constant(e.Key), RqlExpression.Constant(e.Value))
                | QILike e -> RqlExpression.LikeInsensitive(RqlExpression.Constant(e.Key), RqlExpression.Constant(e.Value))
                | QNot e -> RqlExpression.Not((interpreteToRqlExpression e))
                | QIn e -> RqlExpression.ListIn(RqlExpression.Constant(e.Key), RqlExpression.Group("", groupValues(e.Values)))
                | QOut e -> RqlExpression.ListIn(RqlExpression.Constant(e.Key), RqlExpression.Group("", groupValues(e.Values)))
                | QAnd e -> RqlExpression.And((e |> Seq.map interpreteToRqlExpression ))
                | QOr e -> RqlExpression.Or((e |> Seq.map interpreteToRqlExpression ))
                | QOrder e -> RqlExpression.Group("order", groupValues(e.Values))
                | QSelect e -> RqlExpression.Group("select", groupValues(e.Values))
        r