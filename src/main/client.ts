import {dialog} from 'electron';
import fetch, {RequestInit, Response} from 'node-fetch';

export const options = {
  method: 'GET',
  headers: {
	Cookie: '',
	'User-Agent': 'Mozilla/5.0 (X11; Linux x86_64; rv:90.0) Gecko/20100101 Firefox/90.0',
	'Accept-Encoding': 'gzip, deflate, br',
	'Accept-Language': 'en-GB,en;q=0.5',
	'Cache-Control': 'no-cache',
	Connection: 'keep-alive',
	DNT: '1',
	Pragma: 'no-cache'
  }
};

export async function get(url: string, optionsOverride?: Partial<RequestInit>) {
  let response: Response;

  try {
	response = await fetch(url, optionsOverride ?? options);
  } catch (e) {
	console.log(e);
	return null;
  }

  if (!response.ok) {
	console.log(response.statusText);
	return null;
  }

  return response;
}

export async function getString(url: string, optionsOverride?: Partial<RequestInit>): Promise<string | null> {
  const response = await get(url, optionsOverride);

  return response ? await response.text() : null;
}

export function setSessionCookie(session: string) {
  if (!session) {
    dialog.showErrorBox('Error', 'Session cookie was null or empty, set it in settings.json');
    throw new Error('Session cookie was null or empty, set it in settings.json');
  }

  options.headers.Cookie = `_session_id=${session}`;
}

export async function validateSession() {
  const response = await getString('https://codewithmosh.com/');

  if (!response) {
    return false;
  }

  const html = response.toHtml();

  return html.querySelector('#header-sign-up-btn') === null;
}
