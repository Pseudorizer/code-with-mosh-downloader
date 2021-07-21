const path = require('path');
module.exports = {
  /**
   * This is the main entry point for your application, it's the first file
   * that runs in the main process.
   */
  entry: './src/index.ts',
  // Put your normal webpack config below here
  module: {
    rules: require('./webpack.rules'),
  },
  target: 'electron-main',
  resolve: {
    extensions: ['.js', '.ts', '.jsx', '.tsx', '.json'],
    alias: {
      'Src': path.resolve(__dirname, './src'),
      'Main': path.resolve(__dirname, './src/main'),
      'MainTypes': path.resolve(__dirname, './src/main/types')
    }
  }
};
