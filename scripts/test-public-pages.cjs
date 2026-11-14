const instance=require('../instance.json');
// Public content and mocked session only. No OAuth or publication requests.
const {chromium}=require('../.tools/browser/node_modules/playwright');
const assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({headless:true});
 try{const page=await browser.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));let apiCalls=0;
 await page.route('**/api/**',async route=>{apiCalls++;await route.fulfill({status:401,contentType:'application/json',body:'{"error":"Zaloguj się"}'});});
 for(const path of ['terms','privacy','about']){await page.goto(`http://localhost:${instance.frontendPort}/`+path);await page.locator('h1').waitFor();assert.ok((await page.locator('main').innerText()).length>600);assert.ok(await page.locator('a[href="/privacy"]').count());assert.ok(await page.locator('a[href="/terms"]').count());assert.equal(apiCalls,0);await page.reload();await page.locator('h1').waitFor();}
 await page.goto(`http://localhost:${instance.frontendPort}/`);await page.getByRole('heading',{name:'Dobrze Cię widzieć.'}).waitFor();assert.ok(await page.locator('.public-intro').count());
 await page.setViewportSize({width:390,height:844});await page.goto(`http://localhost:${instance.frontendPort}/privacy`);await page.locator('h1').waitFor();assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true);
 assert.deepEqual(errors,[]);console.log('PASS: public pages without session/API, reload, root information plus login, mobile width.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e.message);process.exit(1);});
