import {parsePageFromUrl} from 'Main/pageParser';
import {get} from 'Main/client';
import {WistiaMedia} from 'MainTypes/types';

export async function getMediaOptionsForVideo(url: string) {
  const videoParsed = await parsePageFromUrl(url, 'video');

  const mediaJson = await get(videoParsed[0].nextUrl);

  const wistiaMedia = JSON.parse(mediaJson) as WistiaMedia;
}
