ea.expresser
============

A string to SimpleExpression tree parser for Simple.Data.SqlServer

Examples:

string A = "(ID == 1 || ID > 100) && (Name == "Eugene Alfonso" || Name == "Dave Nicolas" || StartDate > 1/1/2011)"

1st stage: "(E1||E2)&&(E3||E4||E5)"
2nd stage: "E6&&E8"
3rd stage: "E9"

SimpleExpressions: 
E1: ID==1
E2: ID>100
E3: Name=="Eugene Alfonso"
E4: Name=="Dave Nicolas"
E5: StartDate>1/1/2011
E6: E1||E2
E7: E3||E4
E8: E7||E5
E9: E6&&E8

How to use:

Check out EA.Expresser.Nancy for an example building a REST API on top of your database in 4 lines of code
