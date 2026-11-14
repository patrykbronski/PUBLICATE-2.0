const instance=require('../instance.json');
// Only intercepted local API; no provider calls.
const {chromium}=require('../.tools/browser/node_modules/playwright');
const assert=require('node:assert/strict');
(async()=>{const browser=await chromium.launch({headless:true});try{const page=await browser.newPage();let statusCalls=0;
await page.route('**/api/**',async route=>{const path=new URL(route.request().url()).pathname;let data=[];
if(path==='/api/session')data={id:'test',name:'Tester',email:'test@example.test'};
if(path==='/api/platforms')data={platforms:[]};
if(path==='/api/preferences/facebook-target')data={targetId:null};
if(path==='/api/jobs')data=['one','two'].map(id=>({id,platform:'X',targetName:id,state:'Published',externalId:'123',createdAt:new Date().toISOString(),snapshotJson:'{}',safeToRetry:false}));
if(path.endsWith('/attempts'))data=[{detail:path.includes('/one/')?'First attempt':'Second attempt',httpStatus:201}];
if(path.endsWith('/status'))statusCalls++;
await route.fulfill({status:200,contentType:'application/json',body:JSON.stringify(data)});});
await page.goto(`http://localhost:${instance.frontendPort}`);await page.getByRole('button',{name:/Historia/}).first().click();
const rows=page.locator('.history tbody tr');await rows.first().getByRole('button',{name:'Szczegóły',exact:true}).click();await rows.first().getByText(/First attempt/).waitFor();assert.equal(await rows.first().getByRole('button',{name:'Ukryj szczegóły'}).getAttribute('aria-expanded'),'true');
await rows.nth(1).getByRole('button',{name:'Szczegóły',exact:true}).click();await rows.nth(1).getByText(/Second attempt/).waitFor();assert.equal(await rows.first().locator('pre').count(),0);
await rows.nth(1).getByRole('button',{name:'Ukryj szczegóły'}).click();assert.equal(await page.locator('.history pre').count(),0);assert.equal(statusCalls,0);console.log('PASS: details visible in selected row, switch/hide, no status/provider calls.');
}finally{await browser.close();}})().catch(e=>{console.error(e.message);process.exit(1);});
