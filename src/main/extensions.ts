import {HTMLElement, parse} from 'node-html-parser';

export default function defineExtensions() {
  Object.assign(String.prototype, {
	toHtml() {
	  return parse(this);
	},
	fixTitleHyphen() {
	  return this.replace(/(.+)(-)(.+)/gmi, '$1 $2$3');
	}
  });

  Object.assign(HTMLElement.prototype, {
	safeAccess<T>(func: (htmlElement: HTMLElement) => T) {
	  return !this ? null : func(this);
	}
  });

  Object.assign(Array.prototype, {
	safeAccess<T>(func: (htmlElement: HTMLElement[]) => T) {
	  return !this ? null : func(this);
	}
  });
}
