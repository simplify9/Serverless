import * as React from 'react';
import * as ReactDOM from 'react-dom';
import 'bootstrap/dist/css/bootstrap.min.css';

import Main from "./main"

function render() {
  ReactDOM.render(<Main />, document.getElementById("root"));
}

render();
