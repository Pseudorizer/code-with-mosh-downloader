import {DownloadQueueItemType} from 'Types/types';
import {get} from 'Main/client';
import {HTMLElement} from 'node-html-parser';
import {ITypeParser, ParsedItem} from 'MainTypes/types';

export async function parsePageFromUrl(url: string, type: DownloadQueueItemType) {
  if (!type) {
	return null;
  }

  if (url.indexOf('http://') === -1 || url.indexOf('https://') === -1) {
	url = new URL(url, 'https://codewithmosh.com').href;
  }

  const response = await get(url);

  if (!response) {
	return null;
  }

  const parser = getParser(type);

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

	  let courseUrls = html.querySelectorAll('.row.course-list.list > div a[data-role="course-box-link"]').map(x => (
		{nextUrl: x.getAttribute('href'), nextType: 'course'} as ParsedItem
	  ));

	  if (i === 0) {
		// skip first course which is the all access one
		courseUrls = courseUrls.slice(1);
	  }

	  courses.push(...courseUrls);
	}

	return courses;
  }
}

export class CourseParser implements ITypeParser {
  async parse(html: HTMLElement) {
	const rows = html.querySelectorAll('.course-mainbar > .row');

	const parsedRows: ParsedItem[] = [];

	rows.forEach(row => {
	  const titleElement = row.querySelector('.section-title').textContent.trimLeft();

	  // spaces used in html aren't actual spaces but some other character
	  const title = /^([\w\s]+) \(/g.exec(titleElement)[1].replace(' ', ' ');

	  const lectures = row.querySelectorAll('.section-list > li > a').map(x => x.getAttribute('href'));

	  const parsedUrls = lectures.map(x => (
		{nextUrl: x, nextType: 'video'} as ParsedItem
	  ));

	  parsedRows.push(...parsedUrls);
	});

	return parsedRows;
  }
}

export class VideoParser implements ITypeParser {
  async parse(html: HTMLElement) {
	const lectureId = html.querySelector('#lecture_heading').getAttribute('data-lecture-id');
	const videoTitle = html.querySelector('#lecture_heading').textContent.trim().replace(/(\d+)(-)(.+)/gmi, '$1 $2$3');
	const courseSections = html.querySelectorAll('.course-section');

	const courseSection = courseSections.find(x => x.querySelector(`#sidebar_link_${lectureId}`) !== undefined);
	const initialCourseSectionHeading = courseSection.querySelector('.section-title').textContent.trim();
	const courseSectionHeading = /(.+)\s\(\d+m\)/gmi.exec(initialCourseSectionHeading)[1];

	const wistiaId = html.querySelector('.attachment-wistia-player').getAttribute('data-wistia-id');

	return [
	  {
	    nextUrl: `https://fast.wistia.com/embed/medias/${wistiaId}.json`,
		extraData: {
		  courseSectionHeading: courseSectionHeading,
		  videoTitle
		}
	  }
	] as ParsedItem[];
  }
}
