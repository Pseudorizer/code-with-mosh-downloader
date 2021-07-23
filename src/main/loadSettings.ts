import {Settings} from 'Types/types';
import fsAsync from 'fs/promises';
import {setSessionCookie, validateSession} from 'Main/client';
import {dialog} from 'electron';

export let settings: Settings;

export async function loadSettings() {
  try {
	await fsAsync.stat('settings.json');
  } catch (e) {
    const err = e as Error;
    dialog.showErrorBox('Error', 'Settings.json missing. Be sure to rename settings.json.template to settings.json');
    throw new RethrownError(err.message, err);
  }

  const settingsBuffer = await fsAsync.readFile('settings.json');
  settings = JSON.parse(settingsBuffer.toString());

  setSessionCookie(settings.sessionCookie);

  if (!await validateSession()) {
	dialog.showErrorBox('Error', 'Session cookie was invalid');
	throw new Error('Session cookie was invalid');
  }
}
