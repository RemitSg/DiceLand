import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';

// Reuse a supplied Playwright installation; no browser dependencies enter the Unity project.
const require = createRequire(import.meta.url);
let playwright;
if (process.env.PLAYWRIGHT_PACKAGE_PATH) playwright = require(process.env.PLAYWRIGHT_PACKAGE_PATH);
else {
  try { playwright = require('playwright'); }
  catch { playwright = createRequire(path.resolve('tools/relic-catalog/check-catalog.mjs'))('playwright'); }
}
const {chromium} = playwright;
const url = process.env.MERCHANT_EDITOR_URL || 'http://127.0.0.1:8092/';
const file = path.resolve('Assets/Data/Economy/MerchantCatalog.asset');
const output = path.resolve('outputs/agent-battle/merchant-live-editor-20261105');
await fs.mkdir(output,{recursive:true});
const originalBytes = await fs.readFile(file);
const get = async () => {
  const response = await fetch(new URL('api/catalog',url),{signal:AbortSignal.timeout(12000)});
  assert.equal(response.status,200); return response.json();
};
const baseline = await get();
const equal = (actual,expected,message) => { assert.deepEqual(actual,expected,message); checks++; };
const first = doc => doc.rows.find(r => r.category === 'loot' && r.asset.id === '11');
const patchRow = async (values,status = 200,revision) => {
  const doc = await get(), row = first(doc);
  const response = await fetch(new URL('api/catalog',url),{method:'POST',headers:{'Content-Type':'application/json','X-DiceLand-Editor':'merchant'},
    body:JSON.stringify({baseRevision:revision || doc.revision,changes:[{operation:'entry',category:row.category,index:row.index,assetGuid:row.asset.guid,
      goodsId:row.goodsId,minPrice:row.minPrice,maxPrice:row.maxPrice,weight:row.weight,...values}]})});
  equal(response.status,status); return response.json();
};
const until = async (condition,message) => {
  const deadline = Date.now()+18000;
  while(Date.now()<deadline) { if(await condition()) return; await new Promise(resolve => setTimeout(resolve,160)); }
  throw new Error(message);
};
const rawWeight = text => {
  const guid = first(baseline).asset.guid;
  const match = text.match(new RegExp(`lootItem: \\{fileID: 11400000, guid: ${guid}, type: 2\\}\\r?\\n    minPrice: (\\d+)\\r?\\n    maxPrice: (\\d+)\\r?\\n    weight: (\\d+)`));
  assert.ok(match,'Loot asset reference was preserved in serialized configuration'); return Number(match[3]);
};
let checks=0;
const browser = await chromium.launch({headless:true,executablePath:process.env.BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
const context = await browser.newContext({viewport:{width:1512,height:1100}});
const page = await context.newPage();
const errors=[],foreign=[];
page.on('pageerror',e => errors.push(e.message));
page.on('request',request => { if(/^https?:/.test(request.url()) && new URL(request.url()).origin !== new URL(url).origin) foreign.push(request.url()); });
try {
  const loot = baseline.categories.find(c => c.key === 'loot');
  equal(loot.coveredCount,loot.availableCount); equal(loot.entryCount,27);
  for (const [id,min,max,weight] of [['11',8,12,10],['12',18,26,4],['22',18,26,4],['23',18,26,4],['25',18,26,4],['26',30,40,2],['27',18,26,4]]) {
    const row = baseline.rows.find(r => r.category==='loot' && r.asset.id===id);
    equal([row.minPrice,row.maxPrice,row.weight],[min,max,weight],`New loot ${id} has the intended defaults`);
  }
  equal((await fetch(new URL('api/catalog',url),{method:'POST',headers:{'Content-Type':'application/json'},body:'{}'})).status,403);
  equal((await fetch(new URL('../Assets/Data/Economy/MerchantCatalog.asset',url))).status,404);
  await patchRow({weight:0},400); equal((await get()).revision,baseline.revision);
  await patchRow({weight:15},409,'stale-revision'); equal((await get()).revision,baseline.revision);

  await page.goto(url); await page.waitForFunction(() => document.querySelector('#coverage').textContent==='27 / 27');
  equal(await page.locator('#rows tr:not(.detail-row)').count(),27);
  await page.screenshot({path:path.join(output,'desktop.png'),fullPage:true});
  await page.locator('#query').fill('棺钉'); equal(await page.locator('#rows tr:not(.detail-row)').count(),1);
  equal(await page.locator('#rows .asset-name').textContent(),'棺钉'); await page.locator('#query').fill('');
  await page.locator('#rarity').selectOption('2'); equal(await page.locator('#rows .asset-name').textContent(),'交辉棱片');
  await page.locator('#rarity').selectOption('');
  await page.locator('#sort').selectOption('weight'); await page.locator('#direction').click();
  equal(await page.locator('#rows .asset-name').first().textContent(),'铁矿');
  await page.locator('#sort').selectOption('id'); await page.locator('#direction').click();

  const weight = page.getByRole('spinbutton',{name:'秽蚀尖牙权重',exact:true});
  await weight.fill('17'); await until(async () => first(await get()).weight===17,'HTML edit did not reach the asset');
  equal(rawWeight(await fs.readFile(file,'utf8')),17);
  const disk = await fs.readFile(file,'utf8');
  const guid = first(baseline).asset.guid;
  const expression = new RegExp(`(lootItem: \\{fileID: 11400000, guid: ${guid}, type: 2\\}[\\s\\S]*?\\r?\\n    weight: )\\d+`);
  await fs.writeFile(file,disk.replace(expression,(_,prefix) => prefix+'21'));
  await until(async () => await weight.inputValue()==='21','External asset file change did not refresh the HTML'); checks++;
  equal(first(await get()).weight,21);

  const minimum = page.getByRole('spinbutton',{name:'秽蚀尖牙价格下限',exact:true});
  const revision = (await get()).revision;
  await minimum.fill('0'); await page.waitForTimeout(1100);
  equal(await minimum.getAttribute('aria-invalid'),'true'); equal((await get()).revision,revision);
  await minimum.fill(String(first(baseline).minPrice));

  // The external writer changes another field on the same row; retry must merge only locally edited fields.
  await weight.fill('22'); await patchRow({maxPrice:14});
  await page.locator('#conflict').waitFor({state:'visible'}); equal(await weight.inputValue(),'22');
  await page.locator('#retry-changes').click();
  await until(async () => {const row=first(await get());return row.weight===22 && row.maxPrice===14;},'Conflict retry overwrote an external field'); checks++;
  await page.waitForFunction(() => document.querySelector('#conflict').hidden);
  await weight.fill('23'); await patchRow({weight:25});
  await page.locator('#conflict').waitFor({state:'visible'}); await page.locator('#use-latest').click();
  equal(await weight.inputValue(),'25');
  await patchRow({weight:first(baseline).weight,maxPrice:first(baseline).maxPrice});
  await until(async () => await weight.inputValue()===String(first(baseline).weight),'External restore was not observed');

  const last = page.locator('tr[data-row="loot:26"]');
  await last.getByRole('button',{name:'移出',exact:true}).click();
  await page.waitForFunction(() => document.querySelector('#coverage').textContent==='26 / 27'); checks++;
  await page.locator('#open-add').click();
  equal(await page.locator('#add-asset option').count(),1);
  equal(await page.locator('#add-min').inputValue(),'18'); equal(await page.locator('#add-weight').inputValue(),'4');
  await page.locator('#confirm-add').click();
  await page.waitForFunction(() => document.querySelector('#coverage').textContent==='27 / 27'); checks++;
  for (const cat of baseline.categories.filter(c => c.key!=='loot')) {
    await page.locator(`[data-category="${cat.key}"]`).click(); equal(await page.locator('#rows tr:not(.detail-row)').count(),cat.entryCount);
  }
  await page.locator('[data-category="loot"]').click();
  await page.locator('#total-max').fill(String(baseline.maxTotalGoodsCount+1));
  await until(async () => (await get()).maxTotalGoodsCount===baseline.maxTotalGoodsCount+1,'Global stock setting was not saved'); checks++;
  await page.locator('#total-max').fill(String(baseline.maxTotalGoodsCount));
  await until(async () => (await get()).maxTotalGoodsCount===baseline.maxTotalGoodsCount,'Global stock setting did not restore');

  await context.setOffline(true); await weight.fill('33'); await page.waitForTimeout(1500);
  equal(await weight.inputValue(),'33'); equal(first(await get()).weight,first(baseline).weight);
  await context.setOffline(false);
  await until(async () => first(await get()).weight===33,'Pending edits did not save after reconnection'); checks++;
  await patchRow({weight:first(baseline).weight});
  await until(async () => await weight.inputValue()===String(first(baseline).weight),'Restored price was not reflected');

  await page.setViewportSize({width:390,height:844}); await page.screenshot({path:path.join(output,'mobile.png'),fullPage:true});
  equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),true,'Mobile page should not overflow horizontally');
  equal(errors,[]); equal(foreign,[]);
  await page.goto(pathToFileURL(path.resolve('tools/merchant-catalog/dist/index.html')).href);
  await page.locator('#offline').waitFor({state:'visible'}); checks++;
} finally {
  await context.setOffline(false);
  await browser.close();
  // Restore exact original bytes, including authored goods IDs and formatting, then let Unity import them.
  await fs.writeFile(file,originalBytes);
  await until(async () => {
    const restored = await get();
    return JSON.stringify(restored.rows)===JSON.stringify(baseline.rows) && restored.maxTotalGoodsCount===baseline.maxTotalGoodsCount;
  },'Verification did not restore the original merchant configuration');
  const report={checks,errors,foreignRequests:foreign,configurationRestored:true};
  await fs.writeFile(path.join(output,'browser-validation.json'),JSON.stringify(report,null,2));
  console.log(JSON.stringify(report));
}
