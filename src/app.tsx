import * as React from 'react';
import * as ReactDOM from 'react-dom';
import App from 'App/appRoot';

function render() {
  ReactDOM.render(<App/>, document.querySelector('#root'));
}

render();
