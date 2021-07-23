export type Settings = {
  sessionCookie: string,
  defaultResolution: DefaultResolution
}

export type DefaultResolution = {
  width: number,
  height: number
}

export type DownloadQueueItem = {
  url: string,
  type: DownloadQueueItemType
}

export type DownloadQueueItemType = 'everything' | 'course' | 'video';
