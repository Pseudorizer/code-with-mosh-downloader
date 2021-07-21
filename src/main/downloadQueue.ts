import {DownloadQueueItem} from 'Types/types';
import AsyncLock from 'async-lock';
import * as events from 'events';

let queue: DownloadQueueItem[];
const lock = new AsyncLock();

const QUEUE_KEY = 'QUEUE_KEY';

export const downloadEvent = new events.EventEmitter();

export async function enqueueDownload(downloadQueueItem: DownloadQueueItem) {
  await lock.acquire(QUEUE_KEY, () => {
	queue.push(downloadQueueItem);
  });

  downloadEvent.emit('itemEnqueued');
}

export async function getNextDownloadItem() {
  let downloadItem: DownloadQueueItem;

  await lock.acquire(QUEUE_KEY, () => {
    downloadItem = queue.shift();
  });

  return downloadItem;
}
