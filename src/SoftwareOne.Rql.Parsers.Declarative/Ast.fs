module Ast

type QIdentifier = string
type Value = string
   
type QConst = 
    {  
        Key: QIdentifier
        Value : Value
    } 

type QPredefinedConst = 
    {
        Value : Value
    }

type QPredefinedMultiConst = 
    {
        Values :  List<Value>
    }

type QMultiConst = 
    {  
        Key: QIdentifier
        Values : List<Value>
    } 

type QValue = 
    | QEq of QConst
    | QNeq of QConst
    | QGt of QConst
    | QGe of QConst
    | QLt of QConst
    | QLe of QConst
    | QLike of QConst
    | QILike of QConst
    | QIn of QMultiConst
    | QOut of QMultiConst
    | QOrder of QPredefinedMultiConst
    | QSelect of QPredefinedMultiConst
    | QAnd  of List<QValue>
    | QOr  of List<QValue>
    | QNot of QValue
    | QLimit of QPredefinedConst
    | QOffset of QPredefinedConst
    | QSearch of QPredefinedConst
    | NoValue