import {Settings} from 'Types/types';
import fsAsync from 'fs/promises';
import {setSessionCookie, validateSession} from 'Main/client';
import {dialog} from 'electron';

export let settings: Settings;

export async function loadSettings() {
  try {
	await fsAsync.stat('settings.json');
  } catch (e) {
	await fsAsync.writeFile(
	  'settings.json',
	  JSON.stringify({sessionCookie: '', defaultResolution: {width: 1920, height: 1080}}, null, 2)
	);
  }

  const settingsBuffer = await fsAsync.readFile('settings.json');
  settings = JSON.parse(settingsBuffer.toString());

  setSessionCookie(settings.sessionCookie);

  if (!await validateSession()) {
	dialog.showErrorBox('Error', 'Session cookie was invalid');
	throw new Error('Session cookie was invalid');
  }
}
