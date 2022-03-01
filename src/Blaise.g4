grammar Blaise;

file
	: program EOF;

program
	: programDecl varBlock? routines? stat '.';

programDecl
	: 'program' IDENTIFIER SEMI;

varBlock
	: 'var' (
		decl += varDecl SEMI
	)+;

varDecl
	: IDENTIFIER ':' typeExpr;

typeExpr
	: simpleTypeExpr
	| arrayTypeExpr
	| setTypeExpr;

simpleTypeExpr
	: KINTEGER
	| KREAL
	| KBOOLEAN
	| KCHAR
	| KSTRING;

arrayTypeExpr
	: 'array' '[' startIndex = INTEGER '..' endIndex = INTEGER ']' 'of' simpleTypeExpr;

setTypeExpr
	: 'set' 'of' simpleTypeExpr;

stat
	: assignment
	| write
	| writeln
	| procedureCall
	| loop
	| block;

block
	: 'begin' (
		st += stat SEMI
	)* 'end';

write
	: 'write' LPAREN expression RPAREN;

writeln
	: 'writeln' LPAREN expression RPAREN;

assignment
	: IDENTIFIER ':=' expression;

routines
	: (
		procs += procedure
		| funcs += function
	)+;

procedure
	: 'procedure' IDENTIFIER LPAREN paramsList? RPAREN SEMI varBlock? stat SEMI;

function
	: 'function' IDENTIFIER LPAREN paramsList? RPAREN ':' typeExpr SEMI varBlock? stat SEMI;

paramsList
	: var += varDecl (
		SEMI var += varDecl
	)*;

loop
	: whileDo
	| forDo
	| repeatUntil;

whileDo
	: 'while' condition = expression 'do' st = stat SEMI;

forDo
	: 'for' init = assignment direction = (
		'downto'
		| 'to'
	) limit = expression 'do' st = stat SEMI;

repeatUntil
	: 'repeat' (
		st += stat SEMI
	)+ 'until' condition = expression;

expression
	: left = expression binop = POW right = expression
	| left = expression binop = (
		TIMES
		| DIV
	) right = expression
	| left = expression binop = (
		PLUS
		| MINUS
	) right = expression
	| left = expression boolop = COMP right = expression
	| LPAREN inner = expression RPAREN
	| functionCall
	| sign = (PLUS | MINUS)? numericAtom
	| atom;

procedureCall
	: call;

functionCall
	: call;

call
	: IDENTIFIER LPAREN argsList? RPAREN;

argsList
	: args += expression (
		',' args += expression
	)*;

numericAtom
	: INTEGER
	| REAL;

atom
	: IDENTIFIER
	| BOOLEAN
	| CHAR
	| STRING;

KINTEGER
	: 'integer';
KREAL
	: 'real';
KBOOLEAN
	: 'boolean';
KCHAR
	: 'char';
KSET
	: 'set';
KARRAY
	: 'array';
KSTRING
	: 'string';

INTEGER
	: INTEGERPART;
REAL
	: INTEGERPART '.' DECIMALPART;

STRING
	: '\'' (
		.*?
	) '\'';

CHAR
	: '\'' . '\'';
BOOLEAN
	: 'true'
	| 'false';

IDENTIFIER
	: VALID_ID_START VALID_ID_CHAR*;

LPAREN
	: '(';
RPAREN
	: ')';
PLUS
	: '+';
MINUS
	: '-';
TIMES
	: '*';
DIV
	: '/';
COMP
	: GT
	| LT
	| EQ
	| NE
	| GTE
	| LTE;
POW
	: '^';
SEMI
	: ';';

WS
	: [ \r\n\t]+ -> channel(HIDDEN);

COMMENTS
	: '{' .*? '}' -> channel(HIDDEN);

OLD_COMMENTS
	: '(*' .*? '*)' -> channel(HIDDEN);

fragment VALID_ID_START
	: [a-zA-Z_];
fragment VALID_ID_CHAR
	: VALID_ID_START
	| [0-9];
fragment INTEGERPART
	: TERMINALDIGIT DIGIT*
	| ZERO;
fragment DECIMALPART
	: DIGIT* TERMINALDIGIT
	| ZERO;
fragment ZERO
	: '0';
fragment DIGIT
	: [0-9];
fragment TERMINALDIGIT
	: [1-9];
fragment E
	: 'E'
	| 'e';
fragment GT
	: '>';
fragment LT
	: '<';
fragment EQ
	: '=';
fragment NE
	: '<>';
fragment GTE
	: '>=';
fragment LTE
	: '<=';
