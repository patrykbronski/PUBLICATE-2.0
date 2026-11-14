const instance=require('../instance.json');
// Isolated browser fixtures: all /api requests intercepted; no real publication.
const {chromium}=require('../.tools/browser/node_modules/playwright');
const assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({headless:true});const page=await browser.newPage({viewport:{width:1600,height:1000}});
 const targets=[{id:'fb',platform:'Facebook',name:'Testowa strona',accountId:'a'},{id:'ig',platform:'Instagram',name:'testowy.profil',accountId:'b'}];
 const caps=['Facebook','Instagram','TikTok','X','LinkedIn'].map(platform=>({platform,configured:true,capabilities:{text:true,textLimit:platform==='Instagram'?5:5000,images:10,video:true,note:'Fixture',requiredScopes:[]}}));
 let saved=null,publishes=0;const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.route('**/api/**',async route=>{
 const path=new URL(route.request().url()).pathname,method=route.request().method();let data=[];
 if(path==='/api/session')data={id:'fixture',name:'Tester',email:'fixture@example.test'};
 else if(path==='/api/platforms')data={platforms:caps,aiAvailable:false};
 else if(path==='/api/composer/rules')data=caps.map(c=>({platform:c.platform,note:'Wymagania testowe',apiVerification:'Fixture, nie wynik rzeczywistego API',videoMinSeconds:3,videoMaxSeconds:c.platform==='TikTok'?null:900,maxFileBytes:67108864,maxImageBytes:8000000}));
 else if(path==='/api/preferences/facebook-target')data={targetId:'fb'};
 else if(path==='/api/composer/targets'||path==='/api/targets')data=targets;
 else if(path==='/api/composer/validate'){const s=route.request().postDataJSON();data=s.platforms.map(platform=>({platform,errors:platform==='Instagram'&&(s.variants.find(v=>v.platform===platform)?.description??s.description).length>5?['Opis za długi']:[]}));}
 else if(path==='/api/drafts'&&method==='PUT'){const d=route.request().postDataJSON();saved={id:d.id,revision:d.revision+1,state:d.state,jobs:[],createdAt:new Date().toISOString(),expiresAt:new Date(Date.now()+48*3600e3).toISOString(),archived:false};data={draft:saved,evictedId:null};}
 else if(path==='/api/drafts')data=saved?[{...saved,description:saved.state.description}]:[];
 else if(path.startsWith('/api/drafts/')&&path.endsWith('/publish')){publishes++;data={jobs:[],archived:false};}
 else if(path.startsWith('/api/drafts/'))data=saved;
 await route.fulfill({status:200,contentType:'application/json',body:JSON.stringify(data)});
 });
 await page.goto(`http://localhost:${instance.frontendPort}/`);await page.getByRole('button',{name:/Kreator postów/}).click();
 await page.getByLabel('Treść posta').fill('Wspólna treść testowa');
 const choices=page.locator('.platform-choice');assert.equal(await choices.count(),6);
 await choices.filter({hasText:'Facebook'}).getByRole('checkbox').check();await choices.filter({hasText:'Instagram'}).getByRole('checkbox').check();
 assert.equal(await choices.filter({hasText:'TikTok'}).getByRole('checkbox').isDisabled(),true);
 await page.getByRole('button',{name:'Przejdź dalej →'}).click();await page.locator('.variant-card').first().waitFor();assert.equal(await page.locator('.variant-card').count(),2);
 const fb=page.locator('.variant-card').filter({has:page.getByRole('heading',{name:'Facebook',exact:false})});
 const ig=page.locator('.variant-card').filter({has:page.getByRole('heading',{name:'Instagram',exact:false})});
 await ig.locator('textarea').fill('OK');await page.waitForTimeout(1400);assert.equal(await fb.locator('textarea').inputValue(),'Wspólna treść testowa');
 await page.screenshot({path:'.data/composer-step2.png',fullPage:true});
 page.once('dialog',d=>d.dismiss());await page.getByRole('button',{name:'← Wróć do posta bazowego'}).click();assert.equal(await page.locator('.variant-card').count(),2);
 page.once('dialog',d=>d.accept());await page.goBack();await page.getByLabel('Treść posta').waitFor();assert.equal(await page.getByLabel('Treść posta').inputValue(),'Wspólna treść testowa');
 await page.getByRole('button',{name:'Przejdź dalej →'}).click();await page.locator('.variant-card').first().waitFor();assert.equal(await ig.locator('textarea').inputValue(),'Wspólna treść testowa');
 await ig.locator('textarea').fill('Nowe');await page.waitForTimeout(1400);await page.getByRole('button',{name:/Kopie robocze/}).click();await page.getByRole('button',{name:'Kontynuuj →'}).click();await page.locator('.variant-card').first().waitFor();assert.equal(await ig.locator('textarea').inputValue(),'Nowe');
 targets[1]={...targets[1],id:'ig-new',name:'nowy.profil'};
 await page.getByRole('button',{name:/Kopie robocze/}).click();await page.getByRole('button',{name:'Kontynuuj →'}).click();await page.locator('.variant-card').filter({hasText:'nowy.profil'}).waitFor();assert.equal(await ig.locator('textarea').inputValue(),'Nowe');
 assert.ok(await page.getByRole('alert').filter({hasText:'Zmieniono konto docelowe'}).count());
 await page.setViewportSize({width:390,height:844});await page.screenshot({path:'.data/composer-mobile.png',fullPage:true});
 assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth+2),'Page must not overflow on mobile');
 assert.equal(publishes,0);assert.deepEqual(errors,[]);console.log('PASS: disabled platforms, nonblocking step 1, independent variants, cancelled back, browser-back reset, preserved base, draft reopen, target reassignment notice, mobile layout; no publish calls.');
 await browser.close();
})().catch(e=>{console.error(e);process.exitCode=1;});
