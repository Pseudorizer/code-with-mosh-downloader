import {DownloadQueueItemType} from 'Types/types';

export type ParsedAttachment = {
  type: 'text' | 'download' | 'pdf',
  data: unknown,
  name?: string
}

export type ParsedItem = {
  nextUrl: string,
  nextType?: DownloadQueueItemType,
  extraData?: Record<string, unknown>
}

export type WistiaMedia = {
  media: WistiaAssets
}

export type WistiaAssets = {
  assets: WistiaAsset[]
}

export type WistiaAsset = {
  slug: string,
  display_name: string,
  ext: string,
  size: number,
  bitrate: number,
  codec: string,
  url: string,
  width: number,
  height: number,
  type: string
}
