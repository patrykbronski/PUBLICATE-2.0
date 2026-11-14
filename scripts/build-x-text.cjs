// Reproducible bundle of the same official twitter-text parser used by the browser.
const path=require('node:path');const fs=require('node:fs');
const root=path.resolve(__dirname,'..');const web=path.join(root,'src/publicate-web');
require(path.join(web,'node_modules/esbuild')).buildSync({stdin:{contents:'import parseTweet from "twitter-text/dist/esm/parseTweet.js"; export function analyze(text){const p=parseTweet(text); return JSON.stringify({length:p.weightedLength,valid:p.valid});}',resolveDir:web},bundle:true,format:'iife',globalName:'PublicateXText',platform:'browser',target:'es2020',minify:true,outfile:path.join(root,'src/Publicate.Api/Vendor/x-text.js'),legalComments:'eof'});
fs.copyFileSync(path.join(web,'node_modules/twitter-text/LICENSE'),path.join(root,'src/Publicate.Api/Vendor/twitter-text.LICENSE'));
