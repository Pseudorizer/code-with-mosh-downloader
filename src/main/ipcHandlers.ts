import {ipcMain} from 'electron';
import {enqueueDownload} from 'Main/downloadQueue';
import {DownloadQueueItem} from 'Types/types';

export default function loadHandlers() {
  ipcMain.handle('to-enqueue', async (event, args: DownloadQueueItem) => {
    await enqueueDownload(args);
  });
}
