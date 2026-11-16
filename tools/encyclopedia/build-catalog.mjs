import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import {createRequire} from 'node:module';
import path from 'node:path';
import {fileURLToPath} from 'node:url';

const tools = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(tools, '../..');
const output = path.join(root, 'outputs/encyclopedia');
const read = async file => (await fs.readFile(file, 'utf8')).replaceAll('\r\n', '\n');
const parse = text => JSON.parse(text.replace(/^\uFEFF/, ''));
const jsonForHtml = (value, indent) => JSON.stringify(value, null, indent).replaceAll('<', '\\u003c');
const require = createRequire(import.meta.url);
let sharp;
try {
  sharp = require('sharp');
} catch (error) {
  if (error.code !== 'MODULE_NOT_FOUND') throw error;
  sharp = createRequire(path.join(tools, '../relic-catalog/build-catalog.mjs'))('sharp');
}

const [equipment, snapshots, relicPage, enemyPage, template, style, script, bridge, battleLootScript, enemyLootScript, enemyLootStyle] = await Promise.all([
  read(path.join(output, 'equipment.json')).then(parse),
  read(path.join(root, 'outputs/catalog-snapshots.json')).then(parse),
  read(path.join(root, 'outputs/relic-catalog/遗物完整图鉴.html')),
  read(path.join(root, 'outputs/enemy-catalog/敌人与技能完整图鉴.html')),
  read(path.join(tools, 'catalog.html.in')),
  read(path.join(tools, 'catalog.css')),
  read(path.join(tools, 'catalog.js')),
  read(path.join(tools, 'embedded.js.in')),
  read(path.join(tools, 'battle-loot.js')),
  read(path.join(tools, 'enemy-loot.js.in')),
  read(path.join(tools, 'enemy-loot.css'))
]);

function pageData(html) {
  const match = html.match(/<script id="catalog-data" type="application\/json">([\s\S]*?)<\/script>/);
  assert.ok(match, '嵌入页面缺少内置目录数据');
  return parse(match[1]);
}

const relicData = pageData(relicPage);
const enemyData = pageData(enemyPage);
assert.equal(relicData.relics.length, snapshots.relicCatalog.relicCount, '请先重新生成遗物图鉴');
assert.equal(enemyData.enemies.length, snapshots.enemyCatalog.enemyAssets, '请先重新生成敌人图鉴');
assert.equal(enemyData.summary.related_unique_skills, snapshots.enemyCatalog.relatedUniqueSkills, '请先重新生成敌人技能数据');
const dates = snapshots.equipmentCatalog;
assert.deepEqual([equipment.date, equipment.weapons.length, equipment.blueprints.length, equipment.loot.length], [
  dates.snapshotDate, dates.weaponAssets, dates.blueprintAssets, dates.lootAssets
], '请先同步武器、图纸与掉落物的快照记录');

const itemsByType = [['武器', equipment.weapons], ['图纸', equipment.blueprints], ['掉落物', equipment.loot]];
for (const [label, items] of itemsByType) {
  assert.ok(items.length, `缺少${label}数据`);
  assert.equal(new Set(items.map(item => item.detail.id)).size, items.length, `${label}资产 GUID 重复`);
  for (const item of items) {
    assert.match(item.detail.id, /^[a-f0-9]{32}$/, item.path);
    assert.ok(item.detail.name?.trim(), `缺少名称：${item.path}`);
  }
}
const weaponIds = new Set(equipment.weapons.map(item => item.detail.id));
const lootNames = Object.fromEntries(equipment.loot.map(item => [item.detail.id, item.detail.name]));
for (const {detail, path: assetPath} of equipment.blueprints) {
  if (detail.weapon) assert.ok(weaponIds.has(detail.weapon), `图纸的锻造产物未收录：${assetPath}`);
  for (const material of detail.materials) assert.ok(Object.hasOwn(lootNames, material.id), `素材未收录：${material.name} · ${assetPath}`);
}
const enemyIndex = enemyData.enemies.map(({guid, name, number, variant}) => ({guid, name, number, variant}));
const enemyIds = new Set(enemyIndex.map(enemy => enemy.guid));
const battle = equipment.battleLoot;
assert.deepEqual(new Set(battle.enemyIds), enemyIds, '正式敌人图鉴与团结导出的敌人资产 GUID 不一致');
const tablesById = new Map(battle.tables.map(table => [table.id, table]));
assert.equal(tablesById.size, battle.tables.length, '掉落表 GUID 重复');
for (const source of battle.sources) {
  for (const id of source.enemyIds) assert.ok(enemyIds.has(id), `敌群成员未收录：${source.path} · ${id}`);
  for (const id of source.tableIds) assert.ok(tablesById.has(id), `掉落表未收录：${source.path} · ${id}`);
}
for (const table of [...battle.tables, ...battle.sources.flatMap(source => source.eventPicks)]) {
  for (const drop of table.drops) assert.ok(Object.hasOwn(lootNames, drop.id), `掉落物未收录：${drop.id}`);
}
const enemiesWithDrops = new Set(battle.sources.filter(source =>
  source.eventPicks.length || source.tableIds.some(id => tablesById.get(id).drops.length)).flatMap(source => source.enemyIds));
assert.deepEqual([enemyIds.size, enemiesWithDrops.size], [
  dates.enemyAssetsCheckedForLoot, dates.enemiesWithMaterialDrops
], '请先同步敌人掉落覆盖数量');

async function encodeSprite(sprite) {
  // Unity records the imported Sprite rect with a bottom-left origin. Scale to the source PNG.
  const source = path.join(root, sprite.path);
  const meta = await sharp(source).metadata();
  const x = meta.width / sprite.textureWidth;
  const y = meta.height / sprite.textureHeight;
  const left = Math.round(sprite.x * x);
  const right = Math.round((sprite.x + sprite.width) * x);
  const top = Math.round((sprite.textureHeight - sprite.y - sprite.height) * y);
  const bottom = Math.round((sprite.textureHeight - sprite.y) * y);
  const fullImage = !left && !top && right === meta.width && bottom === meta.height;
  const input = fullImage ? source :
    await sharp(source).extract({left, top, width: right - left, height: bottom - top}).toBuffer();
  const bytes = await sharp(input).trim().resize(160, 160, {fit: 'inside', withoutEnlargement: true})
    .webp({quality: 88}).toBuffer();
  return `data:image/webp;base64,${bytes.toString('base64')}`;
}

const sprites = new Map(itemsByType.flatMap(([, items]) => items)
  .map(item => item.icon).filter(icon => icon?.key).map(icon => [icon.key, icon]));
const icons = {};
for (const [key, sprite] of sprites) icons[key] = await encodeSprite(sprite);

function replaceRequired(html, needle, replacement) {
  assert.ok(html.includes(needle), `上游图鉴模板已修改，请更新总图鉴的嵌入适配：${needle}`);
  return html.replaceAll(needle, () => replacement);
}

function embed(html, tab, directory, files) {
  const bootstrap = bridge.replace('/*__CATALOG_KEY__*/', JSON.stringify(tab));
  const lootPanel = tab === 'enemies'
    ? `<style>${enemyLootStyle}</style>\n<script>${battleLootScript}\n${enemyLootScript.replace('/*__BATTLE_LOOT__*/',
      () => jsonForHtml({battle, names: lootNames, date: equipment.date}))}</script>\n`
    : '';
  // Keep the original filters and drawers but give the nested document a valid address
  // and let it report selections to the address bar of the combined catalog.
  let page = replaceRequired(html, '</head>', `<style>.brand { display: none; }</style>\n<script>${bootstrap}</script>\n${lootPanel}</head>`);
  if (tab === 'relics') {
    page = replaceRequired(page, "history.replaceState(null,'',location.pathname+location.search+(hash?'#'+hash:''));", 'setEmbeddedCatalogHash(hash);');
  } else {
    page = replaceRequired(page, "if (url.href !== location.href) history.replaceState(null, '', url.href);", 'setEmbeddedCatalogHash(url.hash.slice(1));');
    page = replaceRequired(page, 'href="../relic-catalog/遗物完整图鉴.html"', 'href="#tab=relics" data-open-catalog="relics"');
    const encounters = '<button type="button" data-section="encounters">投放与来源</button>';
    page = replaceRequired(page, encounters, '<button type="button" data-section="battle-loot">掉落物</button>\n        ' + encounters);
    const detailRender = "$('detail-content').innerHTML = renderDetail(enemy);";
    page = replaceRequired(page, detailRender, detailRender + '\n  appendEnemyLoot(enemy);');
  }
  for (const file of files) {
    page = replaceRequired(page, `href="${file}"`, `href="../${directory}/${file}" target="_blank" rel="noopener"`);
  }
  return page;
}

const embedded = {
  relics: embed(relicPage, 'relics', 'relic-catalog', ['遗物体系完整表.md', 'DiceLand遗物体系表.xlsx']),
  enemies: embed(enemyPage, 'enemies', 'enemy-catalog', ['敌人与技能完整清单.txt', 'catalog.json'])
};
const html = template
  .replaceAll('/*__WEAPON_COUNT__*/', equipment.weapons.length)
  .replaceAll('/*__BLUEPRINT_COUNT__*/', equipment.blueprints.length)
  .replaceAll('/*__LOOT_COUNT__*/', equipment.loot.length)
  .replaceAll('/*__RELIC_COUNT__*/', relicData.relics.length)
  .replaceAll('/*__ENEMY_COUNT__*/', enemyData.enemies.length)
  .replace('/*__STYLE__*/', () => style.trim())
  .replace('/*__EQUIPMENT_DATA__*/', () => jsonForHtml({...equipment, icons, enemyIndex}))
  .replace('/*__EMBEDDED_CATALOGS__*/', () => jsonForHtml(embedded, 2))
  .replace('/*__SCRIPT__*/', () => battleLootScript.trim() + '\n' + script.trim());

const target = path.join(output, 'DiceLand完整图鉴.html');
const previous = await read(target).catch(error => {
  if (error.code !== 'ENOENT') throw error;
  return '';
});
if (previous !== html) {
  const pending = target + '.' + process.pid + '.tmp';
  try {
    await fs.writeFile(pending, html, {flag: 'wx'});
    await fs.rename(pending, target);
  } finally {
    await fs.rm(pending, {force: true});
  }
}
console.log(`已生成总图鉴：${equipment.weapons.length} 把武器、${equipment.blueprints.length} 张图纸、${equipment.loot.length} 种掉落物、${relicData.relics.length} 件遗物、${enemyData.enemies.length} 个敌人。`);
