// See https://aka.ms/new-console-template for more information
using SoftwareOne.Rql.Core.Parsers.v1;

var p = new RqlParser();
// var res = p.Parse("status=eq=processing");
// var res = p.Parse("id=PRD-0000-0001&status=active|mode=1&(mode=2|x=3&y=eq=4)");
var res = p.Parse("in(id,(1))");

Console.WriteLine("Parsed!");