namespace SoftwareOne.Rql.Parsers.Declarative

open FParsec
open Ast

module Parser =
    let identifier = many1Chars (letter <|> pchar '.')

    let valueAmpersand = between (pchar ''') (pchar ''') (many1Chars (letter <|> digit <|> anyOf "*-:+&^ !\""))
    let value = (attempt (many1Chars (letter <|> digit <|> anyOf "*-:+"))) <|> valueAmpersand

    let optimizationValue: Parser<string, unit> = pipe2 ( letter <|> anyOf "-+") (many1Chars (letter <|> digit <|> anyOf "* ."))
                                                        (fun head rest -> $"{head}{rest}")
                                
    let left name = (pstring $"{name}(") .>> spaces
    let right = (pstring ")") .>> spaces
    let comma = pchar ',' .>> spaces

    let valuesF v = between (left "") right (sepBy v comma)
    let values =  valuesF value
    let optValues =  valuesF optimizationValue

    let stringConstant = pipe3 identifier comma value
                               (fun id _ str -> {Key = (QIdentifier id); Value = (Value str)})

    let multiStringConstant = pipe3 identifier comma values
                               (fun id _ str -> {Key = (QIdentifier id); Values = (str |>  List.map Value)})

    let optMultiStringConstant = (sepBy optimizationValue comma)
                                    |>> fun x -> {Values = (x |> List.map Value)}

    let predefinedStringConstant name = pipe3 (pstring name) comma value (fun id _ str -> { Value = (Value str)})

    let mutable qValue, qValueRef = createParserForwardedToRef()
    let parseValue parseConst name t = 
        (between (left name) right parseConst)
        |>> t 
        <?> name

    let parsePredefinedValue name t =
        (between (left "eq") right (predefinedStringConstant name))
        |>> t 
        <?> name

    let comparision name t = parseValue stringConstant name t

    let multiComparision name t = parseValue multiStringConstant name t
    let optMultiComparision name t = parseValue optMultiStringConstant name t

    let qEq = comparision "eq" QEq
    let qNEq = comparision "neq" QNeq
    let qGt = comparision "gt" QGt
    let qGe = comparision "ge" QGe
    let qLe = comparision "le" QLe
    let qLt = comparision "lt" QLt
    let qLike = comparision "like" QLike
    let qILike = comparision "ilike" QILike
    let qIn = multiComparision "in" QIn
    let qOut = multiComparision "out" QOut
    let qOrder = optMultiComparision "ordering" QOrder
    let qSelect = optMultiComparision "select" QSelect

    let qLimit = parsePredefinedValue "limit" QLimit
    let qOffset = parsePredefinedValue "offset" QOffset
    let qSearch = parsePredefinedValue "search" QSearch

    let betweenQuery values2 name t  = 
        between (left name) right values2
        |>> t
        <?> name


    let str_ws s = pstring s >>. spaces
    let isSymbolicOperatorChar = isAnyOf "&|,;"
    let remainingOpChars_ws = manySatisfy isSymbolicOperatorChar .>> spaces

    let opp = new OperatorPrecedenceParser<QValue, string, unit>()
    let expr = opp.ExpressionParser
    opp.TermParser <- qValue .>> spaces <|> between (str_ws "(") (str_ws ")") expr

    let addSymbolicInfixOperators prefix precedence associativity =
        let op = InfixOperator(prefix, remainingOpChars_ws,
                               precedence, associativity, (),
                               fun remOpChars expr1 expr2 ->
                                  match prefix with
                                          | "&" -> QAnd [ expr1; expr2 ]
                                          | "," -> QAnd [ expr1; expr2 ]
                                          | "|" -> QOr [ expr1; expr2 ]
                                          | ";" -> QOr [ expr1; expr2 ]
                                            // todo implement
                                )
        opp.AddOperator(op)

    addSymbolicInfixOperators "&"  10 Associativity.Left
    addSymbolicInfixOperators "|" 1 Associativity.Left
    addSymbolicInfixOperators "," 10 Associativity.Left
    addSymbolicInfixOperators ";" 1 Associativity.Left
    let logicalMulti name t =
        let value = (qValue .>> spaces) //<|> opp.ExpressionParser)
        let values2 = sepBy value comma
        betweenQuery values2 name t

    let logicalSingle name t =
        let value = qValue .>> spaces
        betweenQuery value name t
    let qAnd t = logicalMulti "and" QAnd t
    let qOr t = logicalMulti "or" QOr t
    let qNot t = logicalSingle "not" QNot t

    qValueRef.Value <-
        choice [
          (attempt qAnd)
          (attempt qLimit)
          (attempt qOffset)
          (attempt qSearch)
          (attempt qOr)
          qEq
          qGt
          qGe
          qLt
          qLe
          qNEq
          qNot
          qIn
          qOut
          qLike
          qILike
          qOrder
          qSelect
        ]
            
    
    let Parse x = run opp.ExpressionParser (Normalizer.normalize x)

    let Compile x = match (Parse x) with
                    | Success(x, _, _)   -> (Interpreter.interpreteToRqlExpression x)
                    | Failure(errorMsg, _, _) -> null
