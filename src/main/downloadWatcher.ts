import {downloadEvent, getNextDownloadItem} from 'Main/downloadQueue';
import AsyncLock from 'async-lock';
import {DownloadQueueItem} from 'Types/types';
import {parsePageFromUrl} from 'Main/pageParser';
import {ParsedItem, WistiaMedia} from 'MainTypes/types';
import {get} from 'Main/client';

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
	let videoUrls: ParsedItem[] = [item];

	while (!videoUrls.every(x => x.nextType === 'video')) {
	  videoUrls = await parsePageFromUrl(videoUrls[0].nextUrl, videoUrls[0].nextType);
	}

	for (const video of videoUrls) {
	  const f = await getMediaOptionsForVideo(video.nextUrl);
	}
  }

  await lock.acquire(WATCHER_KEY, () => {
	downloadActive = false;
  });
}

async function getMediaOptionsForVideo(url: string) {
  const videoParsed = await parsePageFromUrl(url, 'video');

  const mediaJson = await get(videoParsed[0].nextUrl);

  const wistiaMedia = JSON.parse(mediaJson) as WistiaMedia;
}
