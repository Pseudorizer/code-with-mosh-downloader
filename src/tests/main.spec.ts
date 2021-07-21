import {Application} from 'spectron';
import assert from 'assert';
import {describe} from 'mocha';

declare module 'mocha' {
  export interface Context {
	app: Application;
  }
}

describe('Application launch', function () {
  this.timeout(10000);

  beforeEach(async function () {
    this.app = new Application({
	  path: 'node_modules/.bin/electron',
	  args: [process.cwd()]
	});

    return this.app.start();
  });

  afterEach(function () {
    if (this.app && this.app.isRunning()) {
      return this.app.stop();
	}
  });

  it('shows an initial window', function () {
    return this.app.client.getWindowCount().then(function (count) {
      assert.strictEqual(count, 1);
	});
  });
});
