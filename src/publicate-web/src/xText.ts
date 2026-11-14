import parseTweet from 'twitter-text/dist/esm/parseTweet.js';
export const xTextLength=(text:string)=>parseTweet(text.normalize('NFC')).weightedLength;
