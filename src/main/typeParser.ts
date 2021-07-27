import {HTMLElement} from 'node-html-parser';
import {ParsedItem} from 'MainTypes/types';

export abstract class TypeParser {
  protected _html: HTMLElement;
  protected _url: string;

  constructor(html: HTMLElement, url: string) {
	this._html = html;
	this._url = url;
  }

  abstract parse(): Promise<ParsedItem[]>;
}
