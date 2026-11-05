import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import {createRequire} from 'node:module';
import path from 'node:path';
import {fileURLToPath} from 'node:url';

const tools = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(tools, '../..');
const output = path.join(root, 'outputs/enemy-catalog');
const read = file => fs.readFile(file, 'utf8');
const readJson = async file => JSON.parse((await read(file)).replace(/^\uFEFF/, ''));

const require = createRequire(import.meta.url);
let sharp;
try {
  sharp = require('sharp');
} catch (error) {
  if (error.code !== 'MODULE_NOT_FOUND') throw error;
  sharp = createRequire(path.join(tools, '../relic-catalog/build-catalog.mjs'))('sharp');
}

const [catalog, snapshots, sources, template, css, script] = await Promise.all([
  readJson(path.join(output, 'catalog.json')),
  readJson(path.join(output, '../catalog-snapshots.json')),
  readJson(path.join(tools, 'portraits.json')),
  read(path.join(tools, 'catalog.template.html')),
  read(path.join(tools, 'catalog.css')),
  read(path.join(tools, 'catalog.js'))
]);

const enemies = catalog.enemies.map(({raw, skills, ...enemy}) => ({
  ...enemy,
  portrait: raw.enemySprite?.guid || '',
  skills: skills.map(({raw, ...skill}) => skill)
}));
const uniqueSkills = new Set(enemies.flatMap(enemy => enemy.skills.map(skill => skill.guid)));
assert.equal(enemies.length, catalog.summary.enemy_assets);
assert.equal(new Set(enemies.map(enemy => enemy.guid)).size, enemies.length);
assert.equal(uniqueSkills.size, catalog.summary.related_unique_skills);
assert.equal(snapshots.enemyCatalog.enemyAssets, enemies.length);
assert.equal(snapshots.enemyCatalog.relatedUniqueSkills, uniqueSkills.size);
assert.equal(snapshots.enemyCatalog.snapshotDate, catalog.summary.timestamp.slice(0, 10));

const portraits = {};
for (const guid of new Set(enemies.map(enemy => enemy.portrait).filter(Boolean))) {
  const sourcePath = sources[guid];
  assert.ok(sourcePath, `缺少 Sprite 源路径 ${guid}`);
  const bytes = await sharp(path.join(root, sourcePath)).trim()
    .resize(220, 220, {fit: 'inside', withoutEnlargement: true})
    .webp({quality: 86}).toBuffer();
  portraits[guid] = `data:image/webp;base64,${bytes.toString('base64')}`;
}

const data = {
  date: snapshots.enemyCatalog.snapshotDate,
  summary: catalog.summary,
  enemies,
  portraits
};
const html = template
  .replace('/*__STYLE__*/', () => css.trim())
  .replace('/*__CATALOG_DATA__*/', () => JSON.stringify(data).replaceAll('<', '\\u003c'))
  .replace('/*__SCRIPT__*/', () => script.trim());
await fs.writeFile(path.join(output, '敌人与技能完整图鉴.html'), html);
console.log(`${enemies.length} 个敌人资产，${uniqueSkills.size} 个技能，${Object.keys(portraits).length} 张内嵌立绘。`);
