import {downloadEvent, getNextDownloadItem} from 'Main/downloadQueue';
import AsyncLock from 'async-lock';
import {DownloadQueueItem} from 'Types/types';
import {parsePageFromUrl} from 'Main/pageParser';
import {ParsedItem} from 'MainTypes/types';

let downloadActive = false;
const lock = new AsyncLock();

const WATCHER_KEY = 'WATCHER_KEY';

export function loadDownloadWatcherHandlers() {
  downloadEvent.on('itemEnqueued', async () => {
	let downloadItem: DownloadQueueItem;

	await lock.acquire(WATCHER_KEY, async () => {
	  if (downloadActive) {
		return;
	  }

	  downloadItem = await getNextDownloadItem();
	});

	await startNewDownload(downloadItem);

	while ((
	  downloadItem = await getNextDownloadItem()
	) !== undefined) {
	  await startNewDownload(downloadItem);
	}
  });
}

async function startNewDownload(downloadItem: DownloadQueueItem) {
  await lock.acquire(WATCHER_KEY, () => {
	downloadActive = true;
  });

  const initialParse = await parsePageFromUrl(downloadItem.url, downloadItem.type);

  for (const item of initialParse) {
	let videoUrls: ParsedItem[] = [];

	while (videoUrls !== null) {
	  videoUrls = await parsePageFromUrl(item.nextUrl, item.nextType);
	}


  }

  await lock.acquire(WATCHER_KEY, () => {
	downloadActive = false;
  });
}
