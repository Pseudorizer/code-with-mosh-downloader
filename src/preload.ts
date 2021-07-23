import {contextBridge, ipcRenderer} from 'electron';

declare global {
  interface Window {
	require: NodeRequire;
	api: unknown;
  }
}

const api = {
  send: (channel: string, data: unknown) => {
	// whitelist channels
	const validChannels = [
	  '',
	];
	if (validChannels.includes(channel)) {
	  ipcRenderer.send(channel, data);
	}
  },
  receive: (channel: string, func: (...args: unknown[]) => void) => {
	const validChannels = [
	  ''
	];
	if (validChannels.includes(channel)) {
	  // Deliberately strip event as it includes `sender`
	  ipcRenderer.on(channel, (event, ...args) => func(...args));
	}
  },
  invoke: (channel: string, func: (...args: unknown[]) => void) => {
	const validChannels = [
	  'to-enqueue'
	];
	if (validChannels.includes(channel)) {
	  // Deliberately strip event as it includes `sender`
	  ipcRenderer.on(channel, (event, ...args) => func(...args));
	}
  },
  handle: (channel: string, func: (...args: unknown[]) => void) => {
	const validChannels = [
	  'to-enqueue'
	];
	if (validChannels.includes(channel)) {
	  // Deliberately strip event as it includes `sender`
	  ipcRenderer.on(channel, (event, ...args) => func(...args));
	}
  },
};

if (process.env.NODE_ENV === 'test') {
  window.require = require;
  window.api = api;
} else {
  // Expose protected methods that allow the renderer process to use
// the ipcRenderer without exposing the entire object
  contextBridge.exposeInMainWorld(
	'electron', api
  );
}

