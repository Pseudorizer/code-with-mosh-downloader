const ForkTsCheckerWebpackPlugin = require('fork-ts-checker-webpack-plugin');
const webpack = require('webpack');

module.exports = [
  new ForkTsCheckerWebpackPlugin(),
  new webpack.IgnorePlugin(/^node-html-parser$/)
];
