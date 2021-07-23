import {HTMLElement} from 'node-html-parser';
import {DownloadQueueItemType} from 'Types/types';

export interface ITypeParser {
  parse(html: HTMLElement): Promise<ParsedItem[]>
}

export type ParsedItem = {
  nextUrl: string,
  nextType?: DownloadQueueItemType,
  extraData?: Record<string, unknown>
}
