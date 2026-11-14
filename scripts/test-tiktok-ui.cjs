const instance=require('../instance.json');
// All API requests mocked. No TikTok connection, upload or publication.
const {chromium}=require('../.tools/browser/node_modules/playwright');
const assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({headless:true});const page=await browser.newPage();let saved=null,creators=0,publishes=0;const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.route('**/api/**',async route=>{const path=new URL(route.request().url()).pathname;const method=route.request().method();let data=[];
 if(path==='/api/session')data={id:'test',name:'Tester',email:'test@example.test'};
 else if(path==='/api/platforms')data={platforms:['Facebook','Instagram','TikTok','YouTube','X','LinkedIn'].map(platform=>({platform,configured:true,capabilities:{text:false,textLimit:2200,images:0,video:true,requiredScopes:[],note:'Fixture'}}))};
 else if(path==='/api/targets'||path==='/api/composer/targets')data=[{id:'tt',platform:'TikTok',name:'Test TikTok',accountId:'a'}];
 else if(path==='/api/targets/tt/creator'){creators++;data={creator_nickname:'Test Creator',privacy_level_options:['SELF_ONLY'],max_video_post_duration_sec:60,comment_disabled:false,duet_disabled:true,stitch_disabled:true};}
 else if(path==='/api/composer/storage')data={usedBytes:0,limitBytes:1073741824};
 else if(path==='/api/preferences/facebook-target')data={targetId:null};
 else if(path==='/api/composer/validate')data=route.request().postDataJSON().platforms.map(platform=>({platform,errors:[]}));
 else if(path==='/api/drafts'&&method==='PUT'){const d=route.request().postDataJSON();saved={id:d.id,revision:d.revision+1,state:d.state,jobs:[],createdAt:new Date().toISOString(),expiresAt:new Date(Date.now()+172800000).toISOString(),archived:false};data={draft:saved,evictedId:null};}
 else if(path==='/api/drafts')data=saved?[{...saved,description:saved.state.description}]:[];
 else if(path.endsWith('/publish'))publishes++;
 await route.fulfill({status:200,contentType:'application/json',body:JSON.stringify(data)});
 });
 await page.goto(`http://localhost:${instance.frontendPort}`);await page.locator('.platform-grid h2').first().waitFor();
 await page.getByRole('button',{name:/Kreator postów/}).click();await page.getByRole('checkbox',{name:/TikTok/}).check();await page.getByRole('button',{name:'Przejdź dalej →'}).click();
 const card=page.locator('.variant-card');await card.getByText(/Twórca: Test Creator/).waitFor();assert.ok(creators>=1&&creators<=2);
 assert.equal(await card.getByLabel('Widoczność').inputValue(),'');assert.equal(await card.getByLabel('Zezwól na komentarze').isChecked(),false);assert.equal(await card.getByLabel('Zezwól na Duet').isDisabled(),true);
 await card.getByLabel('Widoczność').selectOption('SELF_ONLY');await card.getByLabel('Zezwól na komentarze').check();await card.getByLabel('Materiał wygenerowany przez AI').check();await card.getByLabel('Treść promuje markę, produkt lub usługę').check();await card.getByLabel(/Własna marka/).check();assert.equal(await card.getByLabel(/Płatna współpraca/).isDisabled(),true);await card.getByLabel(/Publikując, akceptuję/).check();
 await page.getByRole('button',{name:'Zapisz szkic',exact:true}).click();await page.waitForTimeout(1000);const settings=JSON.parse(saved.state.variants[0].settingsJson);assert.equal(settings.privacy,'SELF_ONLY');assert.equal(settings.ai,true);assert.equal(settings.ownBrand,true);assert.equal(settings.comment,true);assert.equal(settings.consent,true);
 await page.getByRole('button',{name:'Opublikuj na 1 platformie',exact:true}).click();await page.getByRole('dialog').waitFor();assert.equal(await page.getByRole('button',{name:'Potwierdzam — publikuj'}).isDisabled(),true);await page.getByLabel(/Zgadzam się na tymczasowe publiczne/).check();assert.equal(await page.getByRole('button',{name:'Potwierdzam — publikuj'}).isEnabled(),true);
 assert.equal(publishes,0);assert.deepEqual(errors,[]);await browser.close();console.log('PASS: TikTok automatic Creator Info, deliberate privacy, interaction restrictions, AI/brand persistence, HTTPS consent; no publication.');
})().catch(e=>{console.error(e.message);process.exit(1);});
