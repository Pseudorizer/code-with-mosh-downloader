const path = require('path');

module.exports = {
  entry: './src/preload.ts',
  target: 'electron-preload',
  module: {
    rules: require('./webpack.rules'),
  },
  resolve: {
    extensions: ['.js', '.ts', '.jsx', '.tsx', '.json'],
    alias: {
      'Src': path.resolve(__dirname, './src'),
      'Main': path.resolve(__dirname, './src/main'),
      'MainTypes': path.resolve(__dirname, './src/main/types')
    }
  }
};
