import {downloadEvent, getNextDownloadItem} from 'Main/downloadQueue';
import AsyncLock from 'async-lock';
import {DownloadQueueItem} from 'Types/types';
import {parsePageFromUrl} from 'Main/pageParser';
import {ParsedAttachment, ParsedItem} from 'MainTypes/types';
import {getVideoIfAvailable} from 'Main/utilityFunctions';
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
  let downloadItem: DownloadQueueItem;

  downloadEvent.on('itemEnqueued', async () => {
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

	while (videoUrls && !videoUrls.every(x => ['end', 'video'].includes(x.nextType))) {
	  videoUrls = await parsePageFromUrl(videoUrls[0].nextUrl, videoUrls[0].nextType);
	}

	for (const video of videoUrls) {
	  const p = video.nextType === 'end' ? video : (
		await parsePageFromUrl(video.nextUrl, video.nextType)
	  )[0];

	  const y = await getVideoIfAvailable(p);

	  const downloadDirectory = settings.downloadDir || path.join(os.homedir(), 'codewithmosh-downloads');

	  const downloadSaveDirectory = path.join(
		downloadDirectory,
		sanitize(p.extraData.courseTitle as string, {replacement: '_'}),
		sanitize(p.extraData.courseSectionHeading as string, {replacement: '_'}),
		sanitize(p.extraData.videoTitle as string, {replacement: '_'})
	  );

	  const downloadSavePath = path.join(
		downloadSaveDirectory,
		sanitize(p.extraData.videoTitle as string + '.mp4', {replacement: '_'})
	  );

	  try {
		await fs.mkdir(downloadSaveDirectory, {recursive: true});
	  } catch (e) {
		console.log(e);
		continue;
	  }

	  if (y) {
		try {
		  await fs.writeFile(downloadSavePath, y);
		} catch (e) {
		  console.log(e);
		}
	  }

	  if (p.extraData.attachments) {
		for (const x of (p.extraData.attachments as ParsedAttachment[])) {
		  switch (x.type) {
			case 'text': {
			  const savePath = path.join(
				downloadSaveDirectory,
				sanitize(`${p.extraData.videoTitle}.html`)
			  );

			  try {
				await fs.writeFile(savePath, x.data as string);
			  } catch (e) {
				console.log(e);
			  }
			  break;
			}
			case 'download': {
			  const fileData = await get(x.data as string);

			  const i = await fileData.buffer();

			  const savePath = path.join(
				downloadSaveDirectory,
				sanitize(x.name)
			  );

			  try {
				await fs.writeFile(savePath, i);
			  } catch (e) {
				console.log(e);
			  }

			  break;
			}
			case 'pdf': {
			  const k = await get(x.data as string);

			  const y = await k.buffer();

			  const savePath = path.join(
				downloadSaveDirectory,
				sanitize(x.name)
			  );

			  try {
				await fs.writeFile(savePath, y);
			  } catch (e) {
				console.log(e);
			  }
			  break;
			}
		  }
		}
	  }

	  const x = 1;
	}
  }

  await lock.acquire(WATCHER_KEY, () => {
	downloadActive = false;
  });
}
