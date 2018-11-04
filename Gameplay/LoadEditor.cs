$editor::loaded = false;
function mapEditor() {
	if ( !$editor::loaded ) {
		exec( "editor/editor" );
		$editor::loaded = true;
	} else {
		memode();
	}
}
