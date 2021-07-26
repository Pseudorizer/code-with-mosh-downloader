import {downloadEvent, getNextDownloadItem} from 'Main/downloadQueue';
import AsyncLock from 'async-lock';
import {DownloadQueueItem} from 'Types/types';
import {parsePageFromUrl} from 'Main/pageParser';
import {ParsedItem} from 'MainTypes/types';
import {getClosestQuality, getMediaOptionsForVideo} from 'Main/utilityFunctions';
import {settings} from 'Main/loadSettings';
import {get} from 'Main/client';
import * as os from 'os';
import path from 'path';
import sanitize from 'sanitize-filename';
import * as fs from 'fs/promises';

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
	  const p = await parsePageFromUrl(video.nextUrl, video.nextType);
	  const f = await getMediaOptionsForVideo(p);
	  const k = getClosestQuality(f, settings.resolution);
	  const g = await get(k.url);

	  if (!g) {
		continue;
	  }

	  const y = await g.buffer();

	  const downloadDirectory = settings.downloadDir || path.join(os.homedir(), 'codewithmosh-downloads');

	  const downloadSaveDirectory = path.join(
		downloadDirectory,
		sanitize(p[0].extraData.courseTitle as string, {replacement: '_'}),
		sanitize(p[0].extraData.courseSectionHeading as string, {replacement: '_'})
	  );

	  const downloadSavePath = path.join(
	    downloadSaveDirectory,
		sanitize(p[0].extraData.videoTitle as string + '.mp4', {replacement: '_'})
	  );

	  try {
	    await fs.mkdir(downloadSaveDirectory, {recursive: true});
	  } catch (e) {
	    console.log(e);
	    continue;
	  }

	  try {
	    await fs.writeFile(downloadSavePath, y);
	  } catch (e) {
	    console.log(e);
	  }

	  const x = 1;
	}
  }

  await lock.acquire(WATCHER_KEY, () => {
	downloadActive = false;
  });
}
