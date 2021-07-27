import {Element} from 'node-html-parser';

export declare global {
  export interface String {
    toHtml(): Element,
    fixTitleHyphen(): string
  }
}
