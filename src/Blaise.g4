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
	| functionCall
	| block;

block
	: 'begin' (
		st += stat SEMI
	)* 'end';

write
	: 'write' '(' expression ')';

writeln
	: 'writeln' '(' expression ')';

assignment
	: IDENTIFIER ':=' expression;

routines
	: (
		procs += procedure
		| funcs += function
	)+;

procedure
	: 'procedure' IDENTIFIER argsList SEMI varBlock? stat SEMI;

function
	: 'function' IDENTIFIER argsList ':' typeExpr SEMI varBlock? stat SEMI;

argsList
	: '(' ')'
	| '(' v += varDecl (SEMI v += varDecl)* ')';

expression
	: lhs = expression op = POW rhs = expression
	| lhs = expression op = (
		TIMES
		| DIV
	) rhs = expression
	| lhs = expression op = (
		PLUS
		| MINUS
	) rhs = expression
	| lhs = expression boolop = (
		GT
		| LT
		| EQ
		| NE
		| GTE
		| LTE
	) rhs = expression
	| LPAREN inner = expression RPAREN
	| sign = (PLUS | MINUS)? numericAtom
	| atom;

numericAtom
	: INTEGER
	| REAL;

atom
	: STRING
	| functionCall
	| IDENTIFIER;

functionCall
	: IDENTIFIER '(' (
		arg += expression (
			',' arg += expression
		)*
	)? ')';

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
	: [0-9]+;
REAL
	: NUMBER+;

STRING
	: '\'' (
		.*?
	) '\'';

CHAR
	: '\'' . '\'';
BOOLEAN
	: TRUE
	| FALSE;

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
GT
	: '>';
LT
	: '<';
EQ
	: '=';
NE
	: '<>';
GTE
	: '>=';
LTE
	: '<=';
POINT
	: '.';
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
fragment NUMBER
	: [0-9]+ (
		'.' [0-9]+
	)?;
fragment UNSIGNED_INTEGER
	: [0-9]+;
fragment E
	: 'E'
	| 'e';
fragment SIGN
	: (
		'+'
		| '-'
	);
fragment TRUE
	: 'true';
fragment FALSE
	: 'false';
