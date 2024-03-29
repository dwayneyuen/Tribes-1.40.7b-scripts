assert( "string::findsubstr( \"abcdefGHIjkl\", \"cdefgI\" )", -1 );
assert( "string::findsubstr( \"abcdefGHIjkl\", \"cdefg\" )", 2 );
assert( "string::getsubstr( \"abcdefGHIjkl\", 0, 1000 )", "abcdefGHIjkl" );
assert( "string::getsubstr( \"abcdefGHIjkl\", 5, 2 )", "fG" );
assert( "string::compare( \"abcd\", \"ABCD\" )", 1 );
assert( "string::compare( \"abcd\", \"ABCE\" )", 1 );
assert( "string::ncompare( \"abcdE\", \"abcdF\", 4 )", 0 );
assert( "string::icompare( \"abcdE\", \"abcde\" )", 0 );
assert( "string::empty( \"             \" )", true );
assert( "string::left( \"abcdefghi\", 3 )", "abc" );
assert( "string::left( \"abcdefghi\", 1000 )", "abcdefghi" );
assert( "string::right( \"abcdefghi\", 3 )", "ghi" );
assert( "string::right( \"abcdefghi\", 1000 )", "abcdefghi" );
assert( "string::starts( \"abcdefghi\", \"abcd\" )", true );
assert( "string::starts( \"abcdefghi\", \"bcd\" )", false );
assert( "string::ends( \"abcdefghi\", \"abcd\" )", false );
assert( "string::ends( \"abcdefghi\", \"fghi\" )", true );
assert( "string::starts( \"abcdefghi\", \"abcd\" )", true );
assert( "string::insert( \"abcdefghi\", \"XXX\", 3 )", "abcXXXdefghi" );
assert( "string::len( \"abcdefghi\" )", 9 );
assert( "string::length( \"abcdefghi\" )", 9 );
assert( "chr( \"97\" )", "a" );
assert( "ord( \"a\" )", 97 );
assert( "string::rpad( \"abc\", 10 )", "abc       " );
assert( "string::lpad( \"abc\", 10 )", "       abc" );
assert( "string::cpad( \"abc\", 10 )", "    abc   " );
assert( "string::trim( \"\" )", "" );
assert( "string::trim( \"a\" )", "a" );
assert( "string::trim( \"                         a b                             \" )", "a b" );
assert( "string::toupper( \"aBc\" )", "ABC" );
assert( "string::tolower( \"aBc\" )", "abc" );
assert( "string::replace( \"a b c d e f g\", \" \", \"::\" )", "a::b::c::d::e::f::g" );
assert( "string::char( \"aBc\", 0 )", "a" );
assert( "string::char( \"aBc\", 1 )", "B" );
assert( "string::char( \"aBc\", 2 )", "c" );
assert( "string::char( \"aBc\", 3 )", "" );
assert( "string::char( \"aBc\", -1 )", "" );