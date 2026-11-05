import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import {createRequire} from 'node:module';
import path from 'node:path';
import {fileURLToPath, pathToFileURL} from 'node:url';

const tools = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(tools, '../..');
const catalog = JSON.parse((await fs.readFile(path.join(root, 'outputs/enemy-catalog/catalog.json'), 'utf8')).replace(/^\uFEFF/, ''));
const url = pathToFileURL(path.join(root, 'outputs/enemy-catalog/敌人与技能完整图鉴.html')).href;
const evidence = path.join(root, 'outputs/agent-battle/enemy-catalog-ui-20261105');
await fs.mkdir(evidence, {recursive: true});
const require = createRequire(import.meta.url);
let playwright;
try {
  playwright = require('playwright');
} catch (error) {
  if (error.code !== 'MODULE_NOT_FOUND') throw error;
  playwright = createRequire(path.join(tools, '../relic-catalog/check-catalog.mjs'))('playwright');
}
const browser = await playwright.chromium.launch({
  headless: true,
  executablePath: process.env.BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'
});
const context = await browser.newContext({viewport: {width: 1512, height: 960}});
await context.setOffline(true);
const page = await context.newPage();
const requests = [], errors = [];
page.on('pageerror', error => errors.push(error.message));
page.on('request', request => {
  if (/^https?:/.test(request.url())) requests.push(request.url());
});
let checks = 0;
const equal = (actual, expected, message) => { assert.deepEqual(actual, expected, message); checks++; };
const ids = () => page.locator('#results .enemy').evaluateAll(cards => cards.map(card => card.dataset.enemy));
const card = guid => page.locator(`[data-open="${guid}"]`);
const fill = value => page.locator('#query').fill(value);
const reset = () => page.locator('#reset').click();
const capture = name => page.screenshot({path: path.join(evidence, name + '.png')});

try {
  await page.goto(url);
  await page.waitForFunction(() => document.querySelectorAll('#results .enemy').length > 0);
  equal((await ids()).length, catalog.summary.enemy_assets);
  equal(await page.locator('#skill-total').textContent(), String(catalog.summary.related_unique_skills));
  const embedded = await page.locator('#catalog-data').textContent().then(JSON.parse);
  equal(embedded.enemies, catalog.enemies.map(({raw, skills, ...enemy}) => ({
    ...enemy, portrait: raw.enemySprite?.guid || '', skills: skills.map(({raw, ...skill}) => skill)
  })), 'Every recorded enemy and skill field survives the build');
  await page.locator('#results img').evaluateAll(images => Promise.all(images.map(image => {
    image.loading = 'eager';
    return image.decode();
  })));
  equal(await page.locator('#results img').evaluateAll(images => images.length), catalog.enemies.length);
  equal(await page.locator('#results img').evaluateAll(images => images.every(image => {
    const rect = image.getBoundingClientRect();
    const frame = image.parentElement.getBoundingClientRect();
    return rect.top >= frame.top && rect.bottom <= frame.bottom && rect.left >= frame.left && rect.right <= frame.right;
  })), true, 'Portraits stay inside their frames');
  await capture('desktop');

  const dark = catalog.enemies.find(enemy => enemy.number === 9);
  await fill('迫近的黑暗 希望');
  equal(await ids(), [dark.guid]);
  await card(dark.guid).click();
  equal(await page.locator('#detail-name').textContent(), dark.name);
  const granted = dark.skills.find(skill => skill.ally_grant);
  const grant = page.locator(`[data-skill="${granted.guid}"]`);
  equal(await grant.evaluate(element => element.open), true);
  equal((await grant.locator('.skill-roles').textContent()).includes('玩家能耗'), true);
  await page.keyboard.press('Escape');
  equal(await page.locator('#detail').evaluate(dialog => dialog.open), false);

  await fill('０３７ — ０４０');
  equal(new Set(await ids()), new Set(catalog.enemies.filter(enemy => enemy.number >= 37 && enemy.number <= 40).map(enemy => enemy.guid)));
  await fill('id:3001');
  equal(await ids(), [catalog.enemies.find(enemy => enemy.enemy_id === 3001).guid]);
  await fill('#015');
  const twins = catalog.enemies.filter(enemy => enemy.number === 15);
  equal(new Set(await ids()), new Set(twins.map(enemy => enemy.guid)));
  for (const enemy of twins) {
    await card(enemy.guid).click();
    equal((await page.locator('#encounters .technical').textContent()).includes(enemy.path), true);
    await page.locator('#close').click();
  }
  await reset();
  await page.locator('[data-category="外圈普通"]').click();
  equal(new Set(await ids()), new Set(catalog.enemies.filter(enemy => enemy.placements.includes('外圈普通')).map(enemy => enemy.guid)));
  await page.locator('#attribute').selectOption('炎');
  equal(new Set(await ids()), new Set(catalog.enemies.filter(enemy => enemy.placements.includes('外圈普通') && enemy.attributes.includes('炎')).map(enemy => enemy.guid)));
  await reset();
  await page.locator('[data-category="dungeon"]').click();
  equal(await page.locator('#results').getByText('陌路残影', {exact: true}).count(), 1);
  await reset();

  await page.locator('#sort').selectOption('hp');
  await page.locator('#direction').click();
  const hpByGuid = new Map(catalog.enemies.map(enemy => [enemy.guid, enemy.hp]));
  const health = (await ids()).map(guid => hpByGuid.get(guid));
  equal(health, [...health].sort((a, b) => b - a));
  await page.locator('#view-list').click();
  equal(await page.locator('#results').getAttribute('class'), 'grid list');
  await capture('list');
  await page.locator('#view-grid').click();
  await reset();

  await page.locator(`[data-favorite="${dark.guid}"]`).click();
  await page.locator('#favorites-only').click();
  equal(await ids(), [dark.guid]);
  await page.reload();
  equal(await ids(), [dark.guid]);
  await page.locator(`[data-favorite="${dark.guid}"]`).click();
  equal((await ids()).length, 0);
  await page.locator('[data-reset]').click();
  equal((await ids()).length, catalog.enemies.length);

  const order = await ids();
  const byGuid = new Map(catalog.enemies.map(enemy => [enemy.guid, enemy]));
  await card(order[0]).click();
  for (let index = 0; index < order.length; index++) {
    const enemy = byGuid.get(order[index]);
    equal(await page.locator('#detail-name').textContent(), enemy.name);
    equal(await page.locator('.skill-card .technical pre').allTextContents(), enemy.skills.map(skill => skill.detail));
    equal(await page.locator('.phase').count(), enemy.stages.length);
    if (index + 1 < order.length) await page.locator('#next').click();
  }
  equal(await page.locator('#next').isDisabled(), true);
  await page.locator('#prev').click();
  equal(await page.locator('#detail-name').textContent(), byGuid.get(order.at(-2)).name);
  await page.locator('#close').click();

  await page.goto(url + '#enemy-2');
  await page.waitForFunction(() => document.querySelector('#detail')?.open);
  equal(await page.locator('#detail-name').textContent(), catalog.enemies[2].name);
  await page.reload();
  equal(await page.locator('#detail-name').textContent(), catalog.enemies[2].name);
  await page.locator('#close').click();

  await fill('红灯笼 燃烧');
  const lamp = catalog.enemies.find(enemy => enemy.number === 8);
  await card(lamp.guid).click();
  equal(await page.locator('.skill-card.matched[open]').count() > 0, true);
  equal(await page.locator('.skill-body mark', {hasText: '燃烧'}).count() > 0, true);
  await page.locator('[data-section="overview"]').click();
  await capture('detail');
  await page.locator('[data-section="skills"]').click();
  await capture('skill');
  await page.locator('#close').click();
  await page.evaluate(() => window.dispatchEvent(new Event('beforeprint')));
  await page.emulateMedia({media: 'print'});
  equal(await page.locator('#print-view .print-enemy').count(), 1);
  equal(await page.locator('#print-view .print-enemy').isVisible(), true);
  equal(await page.locator('#print-view .print-skill').count(), lamp.skills.length);
  await page.emulateMedia({media: 'screen'});
  await reset();

  for (const width of [390, 320]) {
    await page.setViewportSize({width, height: 844});
    equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true, `No page overflow at ${width}px`);
    if (width === 390) await capture('mobile');
    await fill('迫近的黑暗');
    await card(dark.guid).click();
    equal(await page.locator('#detail').evaluate(dialog => dialog.scrollWidth <= dialog.clientWidth + 1), true);
    if (width === 390) await capture('mobile-detail');
    await page.locator('#close').click();
    await reset();
  }
  equal(requests, []);
  equal(errors, []);
  const report = {checks, enemies: catalog.enemies.length, uniqueSkills: catalog.summary.related_unique_skills, remoteRequests: requests.length, pageErrors: errors.length};
  await fs.writeFile(path.join(evidence, 'validation.json'), JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify(report));
} finally {
  await browser.close();
}
