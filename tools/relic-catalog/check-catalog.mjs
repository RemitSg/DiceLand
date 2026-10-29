import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import {pathToFileURL} from 'node:url';
import {chromium} from 'playwright';
const dir='outputs/relic-catalog',qa='outputs/merge-dev-relic-catalog-20261029';
await fs.mkdir(qa,{recursive:true});
const data=JSON.parse(await fs.readFile(`${dir}/catalog.json`,'utf8'));
const browser=await chromium.launch({headless:true,executablePath:process.env.BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
const page=await browser.newPage({viewport:{width:1440,height:1050},acceptDownloads:true});
const errors=[],remote=[];page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(/^https?:/.test(r.url()))remote.push(r.url());});
const ids=()=>page.locator('#results .relic').evaluateAll(nodes=>nodes.map(n=>Number(n.dataset.id)));
const reset=()=>page.locator('#reset').click();
const screenshot=async name=>page.screenshot({path:`${dir}/${name}-preview.png`});
let checks=0;function equal(a,b){assert.deepEqual(a,b);checks++;}
try{
 await page.goto(pathToFileURL(path.resolve(`${dir}/遗物完整图鉴.html`)).href);
 equal((await ids()).length,159);equal((await ids())[0],1);equal((await ids()).at(-1),159);
 await page.locator('#results img').evaluateAll(nodes=>Promise.all(nodes.map(n=>{n.loading='eager';return n.decode();})));
 equal(await page.locator('#results img').evaluateAll(ns=>ns.every(n=>n.complete&&n.naturalWidth>0)),true);
 await screenshot('desktop');
 for(const group of data.groups){await page.locator(`[data-category="${group.name}"]`).click();equal(await ids(),data.relics.filter(r=>r.category===group.name).map(r=>r.id));}
 await reset();await page.locator('#new-only').click();equal(await ids(),data.newIds);await page.locator('#direction').click();equal(await ids(),[...data.newIds].reverse());await screenshot('new-relics');
 const downloadPromise=page.waitForEvent('download');await page.locator('#export').click();const download=await downloadPromise;await download.saveAs(`${qa}/new-relics.csv`);
 await page.locator('[data-open="159"]').click();equal(await page.locator('#detail').evaluate(d=>d.open),true);assert.match(await page.locator('#detail-content').textContent(),/yin_yang_fish/);checks++;
 await page.reload();equal(await page.locator('#detail').evaluate(d=>d.open),true);equal(await page.locator('#detail-name').textContent(),'阴阳鱼');
 await page.locator('[data-related="157"]').click();equal(await page.locator('#detail-name').textContent(),'阴鱼');await page.keyboard.press('Escape');equal(await page.locator('#detail').evaluate(d=>d.open),false);
 await reset();await page.locator('#rarity').selectOption('5');equal(await ids(),data.relics.filter(r=>r.rarity===5).map(r=>r.id));
 await reset();await page.locator('#query').fill('１４３–１５９');equal(await ids(),data.newIds);
 await page.locator('#query').fill('#159');equal(await ids(),[159]);
 await page.locator('#query').fill('护盾');const shield=data.relics.filter(r=>[r.id,r.relicId,r.relicName,r.description,r.category,r.role,r.tags,r.note,r.triggerNames].join(' ').includes('护盾'));equal(await ids(),shield.map(r=>r.id));
 await page.locator('#rarity').selectOption('2');equal(await ids(),shield.filter(r=>r.rarity===2).map(r=>r.id));
 await reset();await page.locator('#sort').selectOption('name');const names=[...data.relics].sort((a,b)=>a.relicName.localeCompare(b.relicName,'zh-CN')||a.id-b.id).map(r=>r.id);equal(await ids(),names);await page.locator('#direction').click();equal(await ids(),names.reverse());
 await reset();await page.locator('#sort').selectOption('rarity');equal(await ids(),[...data.relics].sort((a,b)=>a.rarity-b.rarity||a.id-b.id).map(r=>r.id));
 await reset();await page.locator('#sort').selectOption('category');const groupOrder=new Map(data.groups.map((g,i)=>[g.name,i]));equal(await ids(),[...data.relics].sort((a,b)=>groupOrder.get(a.category)-groupOrder.get(b.category)||a.id-b.id).map(r=>r.id));
 await reset();await page.locator('#issues-only').check();equal(await ids(),[146]);await page.locator('[data-open="146"]').click();assert.match(await page.locator('.note.issue').textContent(),/代码.*每次防御骰拼点胜利/);checks++;await screenshot('detail');await page.locator('#close').click();
 await reset();await page.locator('[data-fav="144"]').click();await page.locator('#favorites-only').check();equal(await ids(),[144]);await page.reload();equal(await ids(),[144]);await page.locator('[data-fav="144"]').click();equal((await ids()).length,0);await page.locator('#empty-reset').click();equal((await ids()).length,159);
 await page.locator('#query').fill('不存在的遗物zz');equal((await ids()).length,0);equal(await page.locator('#export').isDisabled(),true);await page.locator('#empty-reset').click();
 await page.locator('#query').fill('143');await page.locator('[data-open="143"]').click();await page.locator('[data-tag="商店"]').click();equal(await page.locator('#query').inputValue(),'商店');assert.ok((await ids()).length>0);checks++;
 await reset();await page.locator('#new-only').click();await page.locator('#view-list').click();await screenshot('list');
 await page.locator('[data-open="143"]').click();await page.locator('#next').click();equal(await page.locator('#detail-name').textContent(),'能量保险');await page.locator('#prev').click();equal(await page.locator('#detail-name').textContent(),'进货证明');await page.locator('#close').click();
 for(const width of [320,375,768]){await page.setViewportSize({width,height:900});await page.evaluate(()=>window.scrollTo(0,0));equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true);if(width===375){await screenshot('mobile-list');await page.locator('#view-grid').click();await screenshot('mobile');await page.locator('[data-open="146"]').click();await screenshot('mobile-detail');equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true);await page.locator('#close').click();}}
 equal(remote.length,0);equal(errors,[]);
 const summary={checks,status:'passed',count:data.count,categories:data.groups.length,newCount:data.newIds.length,offlineRemoteRequests:remote.length,pageErrors:errors,responsiveWidths:[320,375,768,1440],csv:`${qa}/new-relics.csv`};
 await fs.writeFile(`${qa}/browser-check.json`,JSON.stringify(summary,null,2));console.log(JSON.stringify(summary));
}finally{await browser.close();}
