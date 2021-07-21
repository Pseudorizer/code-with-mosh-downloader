import {HTMLElement} from 'node-html-parser';

export interface ITypeParser {
  parse(html: HTMLElement): Promise<ParsedItem[]>
}

export type ParsedItem = {
  nextUrl: string,
  extraData?: Record<string, unknown>
}
