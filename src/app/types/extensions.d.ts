import {HTMLElement} from 'node-html-parser';

declare global {
  interface String {
    toHtml(): HTMLElement,
    fixTitleHyphen(): string
  }

  interface Array {
    safeAccess<T>(func: (htmlElement: HTMLElement[]) => T): T | null
  }

  interface Element {
    safeAccess<T>(func: (htmlElement: HTMLElement) => T): T | null
  }
}

declare module 'node-html-parser' {
  interface HTMLElement {
    safeAccess<T>(func: (htmlElement: HTMLElement) => T): T | null
  }
}
