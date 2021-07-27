import {DownloadQueueItemType} from 'Types/types';
import {getString} from 'Main/client';
import {HTMLElement} from 'node-html-parser';
import {Course, ParsedAttachment, ParsedItem} from 'MainTypes/types';
import {TypeParser} from 'Main/typeParser';

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

  const parser = getParser(type, response.toHtml(), url);

  if (!parser) {
    return null;
  }

  return parser.parse();
}

function getParser(type: DownloadQueueItemType, html: HTMLElement, url: string): TypeParser | null {
  switch (type) {
	case 'course':
	  return new CourseParser(html, url);
	case 'everything':
	  return new EverythingParser(html, url);
	case 'video':
	  return new VideoParser(html, url);
	default:
	  return null;
  }
}

export class EverythingParser extends TypeParser {
  override async parse() {
	const numberOfPages = this._html.querySelectorAll('nav > .page').length;

	const courses: ParsedItem[] = [];

	for (let i = 0; i < numberOfPages; i++) {
	  if (i > 0) {
		const nextPage = await getString(`https://codewithmosh.com/courses?page=${i + 1}`);

		if (!nextPage) {
		  continue;
		}

		this._html = nextPage.toHtml();
	  }

	  let courseUrls = this._html.querySelectorAll('.row.course-list.list > div a[data-role="course-box-link"]').map(x => (
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

export class CourseParser extends TypeParser {
  override async parse() {
	const courseId = /\/(\d+)$/gmi.exec(this._url)[1];

	const jsonData = await getString(`https://codewithmosh.com/layabout/current_user?course_id=${courseId}`);

	const courseData = JSON.parse(jsonData) as Course;

	if (courseData.error) {
	  return null;
	}

	return courseData.lectures.map(x => ({
	  nextUrl: x.url,
	  nextType: 'video'
	} as ParsedItem));
  }
}

export class VideoParser extends TypeParser {
  private getVideo() {
	const wistiaIdElement = this._html.querySelector('.attachment-wistia-player');

	return wistiaIdElement ? wistiaIdElement.getAttribute('data-wistia-id') : null;
  }

  private getAttachments() {
	const attachmentElements = this._html.querySelectorAll('.lecture-attachment:not(.lecture-attachment-type-video)');

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
		const firstId = this._html.querySelector('#fedora-keys').getAttribute('data-filepicker');
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

  override async parse() {
	const lectureId = this._html.querySelector('#lecture_heading').getAttribute('data-lecture-id');
	const videoTitle = this._html.querySelector('#lecture_heading').textContent.trim().fixTitleHyphen();
	const courseSections = this._html.querySelectorAll('.course-section');

	const courseSection = courseSections.find(x => x.querySelector(`#sidebar_link_${lectureId}`) !== undefined);
	const initialCourseSectionHeading = courseSection.querySelector('.section-title').textContent.trim();
	const courseSectionHeading = /(.+)\s\(\d+m\)/gmi.exec(initialCourseSectionHeading)[1];

	const wistiaId = this.getVideo();
	const attachments = this.getAttachments();

	const courseTitle = this._html.querySelector('.course-sidebar-head > h2').textContent;

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
