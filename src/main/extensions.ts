import {parse} from 'node-html-parser';

export default function defineExtensions() {
  Object.assign(String.prototype, {
	toHtml() {
	  return parse(this);
	}
  });
}
