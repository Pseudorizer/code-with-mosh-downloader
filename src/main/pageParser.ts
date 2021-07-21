import {DownloadQueueItem, DownloadQueueItemType} from 'Types/types';
import {get} from 'Main/client';
import {HTMLElement} from 'node-html-parser';
import {ITypeParser, ParsedItem} from 'MainTypes/types';

export async function parsePageFromUrl(downloadItem: DownloadQueueItem) {
  const response = await get(downloadItem.url);

  if (!response) {
	return null;
  }

  const parser = getParser(downloadItem.type);

  if (!parser) {
	return null;
  }

  return await parser.parse(response.toHtml());
}

function getParser(type: DownloadQueueItemType): ITypeParser | null {
  switch (type) {
	case 'course':
	  return new CourseParser();
	case 'everything':
	  return new EverythingParser();
	case 'video':
	  return new VideoParser();
	default:
	  return null;
  }
}

export class EverythingParser implements ITypeParser {
  async parse(html: HTMLElement) {
	const numberOfPages = html.querySelectorAll('nav > .page').length;

	const courses: ParsedItem[] = [];

	for (let i = 0; i < numberOfPages; i++) {
	  if (i > 0) {
		const nextPage = await get(`https://codewithmosh.com/courses?page=${i + 1}`);

		if (!nextPage) {
		  continue;
		}

		html = nextPage.toHtml();
	  }

	  const courseUrls = html.querySelectorAll('.row.course-list.list > div a[data-role="course-box-link"]').map(x => (
		{nextUrl: x.getAttribute('href')} as ParsedItem
	  ));

	  courses.push(...courseUrls);
	}

	return courses;
  }
}

export class CourseParser implements ITypeParser {
  async parse(html: HTMLElement) {
	return [
	  {
		nextUrl: '',
		extraData: {
		  '': ''
		}
	  }
	];
  }
}

export class VideoParser implements ITypeParser {
  async parse(html: HTMLElement) {
	return [
	  {
		nextUrl: '',
		extraData: {
		  '': ''
		}
	  }
	];
  }
}
