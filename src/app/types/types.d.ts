export type Settings = {
  sessionCookie: string,
  resolution: Resolution
}

export type Resolution = {
  width: number,
  height: number
}

export type DownloadQueueItem = {
  url: string,
  type: DownloadQueueItemType
}

export type DownloadQueueItemType = 'everything' | 'course' | 'video';
