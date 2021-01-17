const ForkTsCheckerWebpackPlugin = require('fork-ts-checker-webpack-plugin');
const CopyWebpackPlugin = require('copy-webpack-plugin');

module.exports = [
  new CopyWebpackPlugin({
					patterns: [
									{from: "./binaries", to: "binaries"}
					]
	}),
  new ForkTsCheckerWebpackPlugin()
];
