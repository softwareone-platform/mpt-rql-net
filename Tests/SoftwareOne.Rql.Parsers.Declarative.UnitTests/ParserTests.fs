module Tests

open System
open Xunit
open SoftwareOne.Rql.Parsers.Declarative
open FParsec

type TestDataInput = { Input: string;   Value: Ast.QValue }
type TestDataMultiValues = { Value: Ast.QValue; Inputs: string seq }

let basicOperators = 
    [|
        { Value = (Ast.QEq { Key = ("abc"); Value = ("456") }) ; Inputs= [|"eq(abc,456)"; "eq(abc, 456)"; "abc=456"; "abc=eq=456"|] }
        { Value = (Ast.QIn { Key = ("abc"); Values = [ "456"; "fg"; "oo"] }) ; Inputs= [|"in(abc,(456,fg,oo))"; "in(abc, (456, fg, oo))"|] }
    |]

let andOrDifferentSyntax = 
    [|
        { Value = Ast.QOr ([Ast.QAnd [Ast.QEq { Key = "a" ; Value = "4" }; Ast.QEq { Key = "c" ;Value = "6" }]; Ast.QEq { Key = "h"; Value = "6" }] ); Inputs= [|"and(a=4,c=6)|h=6"; "or(h=6,and(a=4,c=6))"; "h=6|(a=4&c=6)"|] }
    |]

let prepareTestData testData : TestDataInput[] seq = testData |> Seq.collect (fun x -> x.Inputs |> Seq.map (fun y -> [|{Input=y; Value=x.Value}|]))

let andOrDifferentSyntaxTestData : TestDataInput[] seq = prepareTestData andOrDifferentSyntax
let basicOperatorsTestData : TestDataInput[] seq = prepareTestData basicOperators

let parse input expected = 
    let a = Parser.Parse input
    match Parser.Parse input with
            | Success(x, _, _)   -> Assert.Equivalent(expected, x)
            | Failure(errorMsg, _, _) -> Assert.Fail(errorMsg)

[<Theory>]
[<MemberData(nameof(andOrDifferentSyntaxTestData))>]
let ``"And" and "or" in different syntax generate equivalent output`` (data: TestDataInput) =
    parse data.Input data.Value

[<Theory>]
[<MemberData(nameof(basicOperatorsTestData))>]
let ``Basic operators in diffent syntax generate equivalent output`` (data: TestDataInput) =
    parse data.Input data.Value