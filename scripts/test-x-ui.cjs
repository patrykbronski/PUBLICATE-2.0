const instance=require('../instance.json');
// All API requests intercepted. No Google calls or real publications.
const {chromium}=require('../.tools/browser/node_modules/playwright');
const assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({headless:true});const page=await browser.newPage();
 const order=['Facebook','Instagram','TikTok','YouTube','X','LinkedIn'];let saved=null;let publishes=0;const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.route('**/api/**',async route=>{const path=new URL(route.request().url()).pathname;const method=route.request().method();let data=[];
 if(path==='/api/session')data={id:'test',name:'Tester',email:'test@example.test'};
 else if(path==='/api/platforms')data={platforms:[...order].reverse().map(platform=>({platform,configured:true,capabilities:{text:false,textLimit:280,images:4,video:true,requiredScopes:[],note:'Fixture'}}))};
 else if(path==='/api/targets'||path==='/api/composer/targets')data=[{id:'x',platform:'X',name:'X test',accountId:'a'}];
 else if(path==='/api/composer/storage')data={usedBytes:0,limitBytes:1073741824};
 else if(path==='/api/preferences/facebook-target')data={targetId:null};
 else if(path==='/api/composer/validate')data=route.request().postDataJSON().platforms.map(platform=>({platform,errors:[]}));
 else if(path==='/api/drafts'&&method==='PUT'){const d=route.request().postDataJSON();saved={id:d.id,revision:d.revision+1,state:d.state,jobs:[],createdAt:new Date().toISOString(),expiresAt:new Date(Date.now()+172800000).toISOString(),archived:false};data={draft:saved,evictedId:null};}
 else if(path==='/api/drafts')data=saved?[{...saved,description:saved.state.description}]:[];
 else if(path.endsWith('/publish'))publishes++;
 await route.fulfill({status:200,contentType:'application/json',body:JSON.stringify(data)});
 });
 await page.goto(`http://localhost:${instance.frontendPort}`);await page.locator('.platform-grid h2').first().waitFor();
 assert.deepEqual(await page.locator('.platform-grid h2').allTextContents(),['Facebook','Instagram','TikTok','YouTube Shorts','X','LinkedIn']);
 await page.getByRole('button',{name:/Kreator postów/}).click();
 await page.getByRole('checkbox',{name:/X$/}).check();await page.getByRole('button',{name:'Przejdź dalej →'}).click();
 const card=page.locator('.variant-card');
 await card.getByLabel('Opis',{exact:true}).fill('Zażółć gęślą jaźń 👨‍👩‍👧‍👦 https://example.com/long/path');
 await page.getByText('44 / 280 · opis z CTA (znaki ważone)',{exact:true}).waitFor();
 await page.getByRole('button',{name:'Zapisz szkic',exact:true}).click();await page.waitForTimeout(1000);
 assert.equal(saved.state.variants[0].description,'Zażółć gęślą jaźń 👨‍👩‍👧‍👦 https://example.com/long/path');
 assert.equal(publishes,0);assert.deepEqual(errors,[]);await browser.close();console.log('PASS: X weighted Unicode/emoji/URL counter and persisted text; no publication.');
})().catch(e=>{console.error(e.message);process.exit(1);});
