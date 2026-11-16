import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import {createRequire} from 'node:module';
import os from 'node:os';
import path from 'node:path';
import {fileURLToPath, pathToFileURL} from 'node:url';

const tools = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(tools, '../..');
const output = path.join(root, 'outputs/encyclopedia');
const readJson = async file => JSON.parse((await fs.readFile(path.join(root, file), 'utf8')).replace(/^\uFEFF/, ''));
const [equipment, snapshots] = await Promise.all([
  readJson('outputs/encyclopedia/equipment.json'),
  readJson('outputs/catalog-snapshots.json')
]);
const dropTables = new Map(equipment.battleLoot.tables.map(table => [table.id, table]));
const sourceDrops = source => [
  ...source.tableIds.flatMap(id => dropTables.get(id).drops),
  ...source.eventPicks.flatMap(pick => pick.drops)
];
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
const temp = await fs.mkdtemp(path.join(os.tmpdir(), 'diceland-encyclopedia-'));
const filename = 'DiceLand完整图鉴.html';
const standalone = path.join(temp, filename);
const context = await browser.newContext({offline: true, viewport: {width: 1360, height: 850}});
const page = await context.newPage();
const errors = [], remoteRequests = [];
page.on('pageerror', error => errors.push(error.message));
page.on('request', request => { if (/^https?:/.test(request.url())) remoteRequests.push(request.url()); });
let checks = 0;
const equal = (actual, expected, message) => { assert.deepEqual(actual, expected, message); checks++; };
const tab = name => page.locator('#tab-' + name);
const results = name => page.locator('#' + name + ' #results');
const cardIds = name => results(name).locator('[data-open]').evaluateAll(cards => cards.map(card => card.dataset.open));
const detailName = page.locator('#detail-name');
const close = () => page.locator('#close').click();
const frame = name => page.frameLocator('#' + name + ' iframe');
const summary = name => page.locator('#detail').getByRole('heading', {name, exact: true});
const gearRelations = (name, target) => results(name).locator('.entry').evaluateAll((cards, target) =>
  cards.map(card => [card.querySelector('[data-open]').dataset.open, [...card.querySelectorAll(`[data-jump="${target}"]`)]
    .map(button => button.dataset.id)]), target);
const detailRelations = target => page.locator(`#detail [data-jump="${target}"]`)
  .evaluateAll(buttons => buttons.map(button => button.dataset.id));

async function decodeImages(name) {
  const count = await results(name).locator('img').evaluateAll(async images => {
    await Promise.all(images.map(image => { image.loading = 'eager'; return image.decode(); }));
    return images.length;
  });
  equal(count, equipment[name].filter(item => item.icon?.key).length, 'All configured icons are drawn offline');
}

async function checkDetails(name, verifyRows) {
  for (const item of equipment[name]) {
    await results(name).locator(`[data-open="${item.detail.id}"]`).click();
    equal(await detailName.textContent(), item.detail.name);
    await verifyRows(item);
    await close();
  }
}

async function expectHash(key, value) {
  await page.waitForFunction(([key, value]) => {
    const params = new URLSearchParams(location.hash.slice(1));
    return new URLSearchParams(params.get('view')).get(key) === value;
  }, [key, value]);
  checks++;
}

try {
  await fs.copyFile(path.join(output, filename), standalone);
  await page.goto(pathToFileURL(standalone).href);
  equal(await page.getByRole('tab').allTextContents(), [
    '武器 ' + equipment.weapons.length, '图纸 ' + equipment.blueprints.length,
    '掉落物 ' + equipment.loot.length,
    '遗物 ' + snapshots.relicCatalog.relicCount, '敌人 ' + snapshots.enemyCatalog.enemyAssets
  ]);
  const {icons, enemyIndex, ...packaged} = JSON.parse(await page.locator('#equipment-data').textContent());
  equal(packaged, equipment, 'All exported equipment fields are embedded');
  equal(await cardIds('weapons'), equipment.weapons.map(item => item.detail.id));
  const recipeIds = id => equipment.blueprints.filter(item => item.detail.weapon === id).map(item => item.detail.id);
  equal(await gearRelations('weapons', 'blueprints'), equipment.weapons.map(({detail: {id}}) => [id, recipeIds(id)]));
  equal(await results('weapons').getByText('无对应卷轴', {exact: true}).count(),
    equipment.weapons.filter(({detail: {id}}) => !recipeIds(id).length).length);
  await decodeImages('weapons');
  await checkDetails('weapons', async ({detail}) => {
    equal(await page.locator('#detail .detail-section > h3').first().textContent(), '对应卷轴');
    equal(await detailRelations('blueprints'), recipeIds(detail.id));
    const skills = detail.skills.filter(skill => skill?.id);
    equal(await page.locator('.skill').count(), skills.length);
    equal(await page.locator('.dice li').allTextContents(), skills.flatMap(skill => skill.dice.map((die, index) =>
      `${index + 1} · ${die.category === 'Attack' ? '攻击' : '防御'} ${die.min}–${die.max}`)));
  });

  const staff = equipment.weapons.find(item => item.detail.name === '电鸣杖');
  await page.locator('#query').fill('电鸣屏障 拼点胜利');
  equal(await cardIds('weapons'), [staff.detail.id]);
  await results('weapons').locator('.card').click();
  equal((await page.locator('.skill-rule').allTextContents()).some(line => line.includes('[ 拼点胜利时 ]')), true);
  await page.reload();
  equal(await detailName.textContent(), staff.detail.name, 'A weapon link opens its detail directly');
  await close();

  await tab('weapons').focus();
  await page.keyboard.press('ArrowRight');
  equal(await tab('blueprints').getAttribute('aria-selected'), 'true');
  equal(await cardIds('blueprints'), equipment.blueprints.map(item => item.detail.id));
  equal(await gearRelations('blueprints', 'weapons'), equipment.blueprints.map(({detail}) =>
    [detail.id, detail.weapon ? [detail.weapon] : []]));
  await decodeImages('blueprints');
  await checkDetails('blueprints', async item => {
    equal(await page.locator('#detail .detail-section > h3').first().textContent(), '锻造产物');
    equal(await detailRelations('weapons'), item.detail.weapon ? [item.detail.weapon] : []);
    const rows = await page.locator('.materials [data-jump="loot"]').evaluateAll(buttons => buttons.map(button => [
      button.dataset.id, button.querySelector('.relation-name').textContent, button.querySelector('.relation-count').textContent
    ]));
    equal(rows, item.detail.materials.map(material => [material.id, material.name, '×' + material.amount]));
    equal(await page.locator('.prices li').count(), item.shop.length);
    if (!item.detail.materials.length) equal(await page.getByText('未配置有效材料，当前无法锻造。').isVisible(), true);
  });

  await page.locator('#filter').selectOption('Wand');
  const wands = new Set(equipment.weapons.filter(item => item.detail.weaponType === 'Wand').map(item => item.detail.id));
  equal(await cardIds('blueprints'), equipment.blueprints.filter(item => wands.has(item.detail.weapon)).map(item => item.detail.id));
  await page.locator('#query').fill('磁电晶 避雷针');
  const recipe = equipment.blueprints.find(item => item.detail.name === '避雷之卷');
  equal(await cardIds('blueprints'), [recipe.detail.id]);
  await results('blueprints').locator('.card').click();
  equal(await page.locator('.sources').allTextContents(), ['主图纸池普通宝藏普通商店']);
  equal(await page.locator('.prices').textContent(), '普通商店 · 15–20 金币权重 5');
  await page.locator('#detail').getByRole('button', {name: '避雷针', exact: true}).click();
  equal(await detailName.textContent(), '避雷针');
  equal(await summary('武器技能').isVisible(), true);
  await page.locator('#detail').getByRole('button', {name: '避雷之卷', exact: true}).click();
  equal(await detailName.textContent(), recipe.detail.name);
  await close();
  equal(await page.locator('#query').inputValue(), '磁电晶 避雷针', 'Switching preserves the active filter');
  await page.locator('[data-view="list"]').click();
  const forgeResult = results('blueprints').getByRole('group', {name: '锻造产物'}).getByRole('button', {name: '避雷针'});
  await forgeResult.press('Enter');
  equal(await detailName.textContent(), '避雷针', 'A scroll row links directly to its weapon');
  await close();
  await page.locator('#query').fill(recipe.detail.name);
  equal(await cardIds('weapons'), [recipe.detail.weapon]);
  await results('weapons').getByRole('group', {name: '对应卷轴'}).getByRole('button', {name: recipe.detail.name}).click();
  equal(await detailName.textContent(), recipe.detail.name, 'A weapon card links directly to its scroll');
  await close();
  equal(await page.locator('#query').inputValue(), '磁电晶 避雷针');
  await page.locator('#reset').click();
  await page.locator('#rarity-options [data-rarity="Rare"]').click();
  await page.locator('#direction').click();
  await page.locator('[data-view="list"]').click();
  equal(await results('blueprints').evaluate(element => element.classList.contains('list')), true);
  equal(await cardIds('blueprints'), equipment.blueprints.filter(item => item.detail.rarity === 'Rare').reverse().map(item => item.detail.id));
  await page.locator('#reset').click();

  await tab('loot').click();
  equal(await cardIds('loot'), equipment.loot.map(item => item.detail.id));
  await decodeImages('loot');
  await checkDetails('loot', async ({detail: {id}}) => {
    const related = equipment.battleLoot.sources.filter(source => sourceDrops(source).some(drop => drop.id === id));
    equal(await page.locator('.loot-source').count(), related.length);
    equal(await page.locator('.drop-rows li').count(), related.flatMap(sourceDrops).filter(drop => drop.id === id).length);
    equal(await page.locator('[data-jump="enemies"]').evaluateAll(buttons => buttons.map(button => button.dataset.id)),
      related.flatMap(source => [...new Set(source.enemyIds)]));
    equal(await page.locator('[data-jump="blueprints"]').evaluateAll(buttons => buttons.map(button => [
      button.dataset.id, button.querySelector('.relation-count').textContent
    ])), equipment.blueprints.flatMap(blueprint => blueprint.detail.materials.filter(material => material.id === id)
      .map(material => [blueprint.detail.id, '×' + material.amount])));
  });
  await page.locator('#filter').selectOption('水');
  equal(await cardIds('loot'), equipment.loot.filter(item => item.detail.attribute === '水').map(item => item.detail.id));
  await page.locator('#reset').click();

  await tab('blueprints').click();
  const water = equipment.loot.find(item => item.detail.name === '水球');
  const waterMan = enemyIndex.find(enemy => enemy.name === '水雪人');
  await results('blueprints').locator(`[data-open="${recipe.detail.id}"]`).click();
  await page.locator(`[data-jump="loot"][data-id="${water.detail.id}"]`).click();
  equal(await detailName.textContent(), water.detail.name);
  equal((await page.locator('.drop-rows li strong').allTextContents()).includes('80% · 1 件'), true);
  equal((await page.locator('.drop-rows li strong').allTextContents()).includes('15% · 1 件'), true);
  await page.locator(`[data-jump="enemies"][data-id="${waterMan.guid}"]`).first().click();
  const enemies = frame('enemies');
  await enemies.locator('#battle-loot').waitFor();
  equal(await enemies.locator('dialog #detail-name').textContent(), waterMan.name);
  await expectHash('enemy', waterMan.guid);
  await enemies.locator(`[data-open-loot="${water.detail.id}"]`).first().click();
  equal(await detailName.textContent(), water.detail.name);
  await page.locator(`[data-jump="enemies"][data-id="${waterMan.guid}"]`).first().click();
  await enemies.locator('[data-section="battle-loot"]').click();
  await enemies.locator(`[data-open-loot="${water.detail.id}"]`).first().click();
  await page.locator(`[data-jump="blueprints"][data-id="${recipe.detail.id}"]`).click();
  equal(await detailName.textContent(), recipe.detail.name);
  await close();

  await tab('relics').click();
  const relics = frame('relics');
  equal(await relics.locator('#total').textContent(), String(snapshots.relicCatalog.relicCount));
  equal(await relics.locator('#results .relic').count(), snapshots.relicCatalog.relicCount);
  await relics.locator('#query').fill('159');
  await relics.locator('#results [data-open="159"]').click();
  equal(await relics.locator('#detail-name').textContent(), '阴阳鱼');
  await expectHash('relic', '159');
  await page.reload();
  equal(await relics.locator('#detail-name').textContent(), '阴阳鱼');
  await relics.locator('#close').click();

  await tab('enemies').click();
  equal(await enemies.locator('#enemy-total').textContent(), String(snapshots.enemyCatalog.enemyAssets));
  equal(await enemies.locator('#skill-total').textContent(), String(snapshots.enemyCatalog.relatedUniqueSkills));
  const order = await enemies.locator('#results [data-open]').evaluateAll(buttons => buttons.map(button => button.dataset.open));
  equal(new Set(order), new Set(equipment.battleLoot.enemyIds), 'Every formal enemy has a drop panel');
  await enemies.locator('#results [data-open]').first().click();
  for (const [index, guid] of order.entries()) {
    const expected = equipment.battleLoot.sources.filter(source => source.enemyIds.includes(guid)).flatMap(sourceDrops);
    equal(await enemies.locator('#battle-loot [data-open-loot]').evaluateAll(buttons => buttons.map(button => button.dataset.openLoot)),
      expected.map(drop => drop.id), 'Enemy links retain every configured roll entry');
    if (index + 1 < order.length) await enemies.locator('#next').click();
  }
  await enemies.locator('#close').click();
  await enemies.locator('#query').fill('白蛇妖 绞缠');
  await enemies.locator('#results [data-open]').click();
  equal(await enemies.locator('#detail-name').textContent(), '白蛇妖');
  equal(await enemies.locator('.skill-card').count(), 3);
  equal((await enemies.locator('#battle-loot').textContent()).includes('未为该敌人配置素材掉落'), true);
  await expectHash('q', '白蛇妖 绞缠');
  await page.reload();
  equal(await enemies.locator('#detail-name').textContent(), '白蛇妖');
  await enemies.locator('#close').click();
  await enemies.locator('a[data-open-catalog="relics"]').last().click();
  equal(await tab('relics').getAttribute('aria-selected'), 'true');

  for (const width of [390, 320]) {
    await page.setViewportSize({width, height: 800});
    for (const name of ['weapons', 'blueprints', 'loot', 'relics', 'enemies']) {
      await tab(name).click();
      const noOverflow = () => document.documentElement.scrollWidth <= innerWidth &&
        [...document.querySelectorAll('[role="tabpanel"]:not([hidden])')].every(panel => panel.scrollWidth <= panel.clientWidth);
      equal(await page.evaluate(noOverflow), true, `${name} at ${width}px`);
      if (name === 'relics' || name === 'enemies') equal(await frame(name).locator('body').evaluate(noOverflow), true);
    }
  }
  equal(remoteRequests, []);
  equal(errors, []);
  console.log(`${checks} 项离线图鉴检查通过，远程请求与页面异常均为 0。`);
} finally {
  await browser.close();
  await fs.rm(standalone, {force: true});
  await fs.rmdir(temp);
}
