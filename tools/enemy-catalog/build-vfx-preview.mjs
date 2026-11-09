import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';

const tools=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(tools,'../..');
const catalog=JSON.parse((await fs.readFile(path.join(root,'outputs/enemy-catalog/catalog.json'),'utf8')).replace(/^\uFEFF/,''));
const enemy=catalog.enemies.find(enemy=>enemy.enemy_id===44);
const skill=enemy.skills.find(skill=>skill.name==='分解');
const vfx=skill.visual_effects;
if(!vfx)throw new Error('分解缺少已核实的特效记录');
const catalogHtml=await fs.readFile(path.join(root,'outputs/enemy-catalog/敌人与技能完整图鉴.html'),'utf8');
const embedded=JSON.parse(catalogHtml.match(/<script id="catalog-data" type="application\/json">(.*?)<\/script>/s)[1]);
const atlas=await fs.readFile(path.join(root,vfx.atlas_path));
const data={atlas:`data:image/png;base64,${atlas.toString('base64')}`,idle:embedded.portraits[enemy.raw.enemySprite.guid],pose:embedded.skillArt[skill.presentations[0].sprite_guid],frames:vfx.frames,fps:vfx.fps,duration:vfx.duration,atlasName:path.basename(vfx.atlas_path),sourceLabel:vfx.source_label||'内置 ImageGen 生成透明原图'};
const template=await fs.readFile(path.join(tools,'vfx-preview.template.html'),'utf8');
const output=path.join(root,'outputs/enemy-vfx/044-分解-枯根撕裂.html');
await fs.mkdir(path.dirname(output),{recursive:true});
await fs.writeFile(output,template.replace('/*__VFX_DATA__*/',()=>JSON.stringify(data).replaceAll('<','\\u003c')));
console.log('Created offline VFX preview:',output);
