import {DownloadQueueItemType} from 'Types/types';
import {getString} from 'Main/client';
import {HTMLElement} from 'node-html-parser';
import {ITypeParser, ParsedAttachment, ParsedItem} from 'MainTypes/types';

export async function parsePageFromUrl(url: string, type: DownloadQueueItemType) {
  if (!type) {
	return null;
  }

  if (url.indexOf('http://') === -1 || url.indexOf('https://') === -1) {
	url = new URL(url, 'https://codewithmosh.com').href;
  }

  const response = await getString(url);

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
		const nextPage = await getString(`https://codewithmosh.com/courses?page=${i + 1}`);

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
  private static getVideo(html: HTMLElement) {
	const wistiaIdElement = html.querySelector('.attachment-wistia-player');

	return wistiaIdElement ? wistiaIdElement.getAttribute('data-wistia-id') : null;
  }

  private static getAttachments(html: HTMLElement) {
	const attachmentElements = html.querySelectorAll('.lecture-attachment:not(.lecture-attachment-type-video)');

	const attachments: ParsedAttachment[] = [];

	attachmentElements.forEach(x => {
	  if (x.classList.contains((
		'lecture-attachment-type-text'
	  ))) {
		const textContainer = x.querySelector('.lecture-text-container');

		attachments.push({
		  type: 'text',
		  data: textContainer.innerHTML
		});
	  } else if (x.classList.contains('lecture-attachment-type-file')) {
		const downloadLink = x.querySelector('a');
		const filename = downloadLink.textContent.trim();

		attachments.push({
		  type: 'download',
		  data: downloadLink.getAttribute('href'),
		  name: filename.fixTitleHyphen()
		});
	  } else if (x.classList.contains('lecture-attachment-type-pdf_embed')) {
		const firstId = html.querySelector('#fedora-keys').getAttribute('data-filepicker');
		const secondId = x.querySelector('.wrapper > div').getAttribute('data-pdfviewer-id');
        const filename = x.querySelector('.label').textContent.trim();

        attachments.push({
		  type: 'pdf',
		  data: `https://cdn.filestackcontent.com/${firstId}/${secondId}`,
		  name: filename
		});
	  }
	});

	return attachments;
  }

  async parse(html: HTMLElement) {
	const lectureId = html.querySelector('#lecture_heading').getAttribute('data-lecture-id');
	const videoTitle = html.querySelector('#lecture_heading').textContent.trim().fixTitleHyphen();
	const courseSections = html.querySelectorAll('.course-section');

	const courseSection = courseSections.find(x => x.querySelector(`#sidebar_link_${lectureId}`) !== undefined);
	const initialCourseSectionHeading = courseSection.querySelector('.section-title').textContent.trim();
	const courseSectionHeading = /(.+)\s\(\d+m\)/gmi.exec(initialCourseSectionHeading)[1];

	const wistiaId = VideoParser.getVideo(html);
	const attachments = VideoParser.getAttachments(html);

	const courseTitle = html.querySelector('.course-sidebar-head > h2').textContent;

	return [
	  {
		nextUrl: wistiaId ? `https://fast.wistia.com/embed/medias/${wistiaId}.json` : null,
		nextType: 'end',
		extraData: {
		  courseTitle,
		  courseSectionHeading,
		  videoTitle,
		  attachments
		}
	  }
	] as ParsedItem[];
  }
}
