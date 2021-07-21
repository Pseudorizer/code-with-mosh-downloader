export type Settings = {
  sessionCookie: string
}

export type DownloadQueueItem = {
  url: string,
  type: DownloadQueueItemType
}

export type DownloadQueueItemType = 'everything' | 'course' | 'video';
