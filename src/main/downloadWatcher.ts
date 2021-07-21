import {downloadEvent, getNextDownloadItem} from 'Main/downloadQueue';
import AsyncLock from 'async-lock';
import {DownloadQueueItem} from 'Types/types';

let downloadActive = false;
const lock = new AsyncLock();

const WATCHER_KEY = 'WATCHER_KEY';

downloadEvent.on('itemEnqueued', async () => {
  let downloadItem: DownloadQueueItem;

  await lock.acquire(WATCHER_KEY, async () => {
	if (downloadActive) {
	  return;
	}

	downloadItem = await getNextDownloadItem();
  });

  await startNewDownload(downloadItem);

  while ((downloadItem = await getNextDownloadItem()) !== undefined) {
    await startNewDownload(downloadItem);
  }
});

async function startNewDownload(downloadItem: DownloadQueueItem) {
  await lock.acquire(WATCHER_KEY, () => {
	downloadActive = true;
  });
}
