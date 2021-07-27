import {get, getString} from 'Main/client';
import {ParsedItem, WistiaAsset, WistiaMedia} from 'MainTypes/types';
import {Resolution} from 'Types/types';
import {settings} from 'Main/loadSettings';

// TODO think of a better name for this file

export async function getVideoIfAvailable(parsedItem: ParsedItem) {
  if (!parsedItem.nextUrl) {
    return null;
  }

  const f = await getMediaOptionsForVideo(parsedItem);
  const k = getClosestQuality(f, settings.resolution);
  const g = await get(k.url);

  if (!g) {
    return null;
  }

  return await g.buffer();
}

export async function getMediaOptionsForVideo(videoParsed: ParsedItem) {
  const mediaJson = await getString(videoParsed.nextUrl);

  const wistiaMedia = JSON.parse(mediaJson) as WistiaMedia;

  return wistiaMedia.media.assets.filter(x => x.ext === 'mp4' || x.slug === 'original' || x.type === 'still_image');
}

export function getClosestQuality(assets: WistiaAsset[], resolution: Resolution) {
  const exactMatch = assets.find(x => x.height === resolution.height && x.width === resolution.width);

  if (exactMatch) {
    return exactMatch;
  }

  const referenceAsset = {
    width: resolution.width,
    height: resolution.height,
    bitrate: assets.reduce((a, b) => a + b.bitrate, 0) / assets.length,
    ext: '',
    display_name: '',
    type: '',
    size: 0,
    codec: '',
    url: '',
    slug: ''
  };

  assets.push(referenceAsset);

  assets = assets.sort((a, b) => {
    return (a.height * a.width + a.bitrate) - (b.height * b.width + b.bitrate);
  });

  const insertedPosition = assets.indexOf(referenceAsset);
  const isAtTop = insertedPosition === assets.length - 1;

  return isAtTop ? assets[assets.length - 2] : assets[insertedPosition + 1];
}
