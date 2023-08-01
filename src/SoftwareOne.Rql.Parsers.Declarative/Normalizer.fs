module Normalizer
open System.Text.RegularExpressions

let normalize query =
    let keyRegExGroup = "(?<key>[\w\.]+)"
    let valueRegExGroup = "(?<value>[\w\-\*\:\+]+)"
    let valueRegExGroupAmpersand = "(?<value>('){1}[\w\-\*\:\+\&\^\! \"]+('){1})"
    let normalizeOperatorsBetweenEquals x = Regex.Replace(x, $"{keyRegExGroup}=(?<op>\w+)={valueRegExGroup}", "${op}(${key},${value})")
    let normalizeOperatorsBetweenEqualsAmperand x = Regex.Replace(x, $"{keyRegExGroup}=(?<op>\w+)={valueRegExGroupAmpersand}", "${op}(${key},${value})")
    let normalizeShortEqual x = Regex.Replace(x, $"{keyRegExGroup}={valueRegExGroup}", "eq(${key},${value})") 
    let normalizeShortEqualAmperand x = Regex.Replace(x, $"{keyRegExGroup}={valueRegExGroupAmpersand}", "eq(${key},${value})") 

    query 
    |> normalizeOperatorsBetweenEquals
    |> normalizeOperatorsBetweenEqualsAmperand
    |> normalizeShortEqual
    |> normalizeShortEqualAmperand
    