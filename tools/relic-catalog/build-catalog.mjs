import fs from 'node:fs/promises';
import path from 'node:path';
import sharp from 'sharp';
const dir='outputs/relic-catalog';
const read=async p=>JSON.parse((await fs.readFile(p,'utf8')).replace(/^\uFEFF/,''));
const [raw,old,curation]=await Promise.all([read(`${dir}/relics-raw.json`),read(`${dir}/catalog.json`),read('tools/relic-catalog/curation.json')]);
const triggers=['移动消耗前','状态施加时','玩家骰子准备时','即将受到单方面攻击伤害','商店价格计算时','玩家回合开始','战斗回合开始','玩家死亡时','战斗回合结束','即将受到攻击伤害','遗物重铸时','掉落物重铸时','休息格重铸上限计算时','即将受到伤害','即将造成伤害','敌人死亡时','即将受到致命伤害','商店购买完成时','收集钥匙时','触发灾厄时','受到单方面攻击伤害后','命中敌方时','防御骰拼点胜利','玩家击杀敌人时','战斗掉落奖励发放时','战斗建立时','获得遗物时','消耗行动点时','商店库存数量计算时','商店支付能力判定时','遗物选择奖励生成时','玩家拼点失败','休息格恢复行动点时','休息格重铸完成时','休息格治疗完成时','玩家使用技能时','战斗完成时','战斗失败时','玩家骰子掷出时','友方护盾层数减少时'];
const rarityNames={Common:'普通',Uncommon:'罕见',Rare:'稀有',Epic:'史诗',Legendary:'传说',Trash:'垃圾'};
const byPath=new Map(old.relics.map(r=>[r.path,r]));
const relics=[];
for(const r of raw.rows){
  const prior=byPath.get(r.path)||{};
  const id=prior.id??(r.relicId==='yin_yang_fish'?159:Number(r.relicId));
  if(!Number.isInteger(id)||(!prior.id&&!curation[id]))throw Error(`Unclassified relic ${r.path}`);
  const effects=r.effects.map(e=>({trigger:e.trigger,effect:e.effect,data:Object.fromEntries(Object.entries(e.data).map(([k,v])=>[k,k in e.references?e.references[k]:v])),references:e.references}));
  const refs=(e)=>Object.fromEntries(Object.entries(e.data).map(([k,v])=>[k,k in e.references?(e.references[k].valid?{fileID:11400000,guid:e.references[k].guid,type:2}:{fileID:0}):v]));
  const item={...prior,...curation[id],id,relicId:r.relicId==='yin_yang_fish'?r.relicId:Number(r.relicId),relicName:r.relicName,path:r.path,guid:r.guid,description:r.description,additionalDescription:r.additionalDescription,rarity:r.rarity,rarityName:rarityNames[r.rarityName]||prior.rarityName||r.rarityName,icon:{fileID:r.iconGuid?21300000:0,...(r.iconGuid?{guid:r.iconGuid,type:3}:{})},iconPath:r.iconPath,iconGuid:r.iconGuid,iconRect:r.iconRect,iconTextureSize:r.iconTextureSize,builtInEffects:r.effects.map(refs),customEffectHandler:r.customEffectHandler?{path:r.customEffectHandler}:{fileID:0},resolvedEffects:effects,referencedByRelicTables:r.referencedByRelicTables,source:path.basename(r.path),triggerNames:[...new Set(r.effects.map(e=>triggers[e.data.trigger]||e.trigger))].join('、')||'无内置触发',effectNames:r.effects.map(e=>e.effect),isNew:id>=143,configVerifiedAt:'2026-10-29',codeReviewedAt:curation[id]?.codeReviewedAt||(id>=143?'2026-10-29':'2026-10-27')};
  relics.push(item);
}
relics.sort((a,b)=>a.id-b.id);
if(relics.length!==raw.rows.length||new Set(relics.map(r=>r.id)).size!==relics.length||new Set(relics.map(r=>r.guid)).size!==relics.length)throw Error('Count or identity mismatch');
const groups=old.groups.map(g=>({...g,count:relics.filter(r=>r.category===g.name).length}));
if(groups.reduce((sum,g)=>sum+g.count,0)!==relics.length)throw Error('Unregistered category');
Object.assign(groups.find(g=>g.name==='水蚀'),{summary:'水蚀条件增益与即时易伤转换。',examples:'钟乳石、浪鸣、碎潮盐瓶、涌潮加速器',play:'保留水蚀可驱动条件增伤、减伤与急速；碎潮盐瓶将每次新施加的水蚀改为基础 15 层即时易伤，放弃该次水蚀层数。浪鸣现为每层水蚀额外 5 层易伤的伤害修正。'});
Object.assign(groups.find(g=>g.name==='锻造与重铸'),{summary:'保留材料、增加产物、提高次数，以及阴鱼与阳鱼的固定配对。',examples:'锻灵石、繁生核、矮人腕环、阴鱼、阳鱼',play:'保留既有重铸收益；阴鱼与阳鱼固定合成阴阳鱼。产物当前无战斗效果。'});
Object.assign(groups.find(g=>g.name==='防御与反击'),{summary:'单方面攻击减伤、反击、防御骰子与护盾的产生和消耗。',play:'盾灵逐回合供盾，护甲保养剂开局供盾，安全感强化有盾防御骰，盾反利用失盾反伤。反击刺刃按每角色每回合一次提供下回合临时强壮。'});
const catalog={date:'2026-10-29',count:relics.length,coverage:'160 件当前资产配置由团结编辑器导出；新增目录序号 143–160，以及历史序号 12、19、30、59、79、112、134、138 的主要机制已核对。已按设计反馈修订凰的反复保命、反击刺刃限次、福灵剂逐角色概率与当头棒文案，并修复阵亡队员在后续战斗结算后丢失的问题。本次屏蔽笼、寒霜护符、疾病减伤为 10%，幽影斗篷、驰隙时序环为 20%，并修正屏蔽笼触发时机。其余历史 134 件保留原机制备注，未重新逐项审计全部历史实现；新增稀有遗物 160 碎潮盐瓶（每次敌方水蚀施加改为基础 15 层易伤），浪鸣调整为每层水蚀视为额外 5 层易伤；验证范围见维护说明，未做完整试玩。',exportedAt:raw.exportedAt,newIds:relics.filter(r=>r.isNew).map(r=>r.id),groups,relics};
await fs.writeFile(`${dir}/catalog.json`,JSON.stringify(catalog,null,2)+'\n');
const cell=v=>String(v??'').replaceAll('|','\\|').replaceAll('\n','<br>');
const lines=['# DiceLand 现有遗物体系表','',`更新日期：${catalog.date}。共 ${catalog.count} 件遗物，${groups.length} 个主体系。每件只计入一个主体系，跨体系关系保留在联动标签中。`,'',catalog.coverage,'','来源：当前项目 Relic_SO 配置、LootRarity 枚举及 RelicSystem、地图与重铸实现。离线交互入口：[遗物完整图鉴](遗物完整图鉴.html)。','', '2026-10-29 新入库共 18 件：143–158、阴阳鱼（目录序号 159，实际 relicId 为 `yin_yang_fish`）以及稀有遗物 160 碎潮盐瓶。浪鸣的每层水蚀额外易伤已由 10 调整为 5。本次修订凰的反复保命、反击刺刃每角色每回合限次、福灵剂逐角色独立概率与当头棒“伤害 +100%”文案，并保留阵亡队员的队伍位置。阴阳鱼当前没有配置战斗效果。','','| 主体系 | 数量 | 主要玩法 |','|---|---:|---|',...groups.map(g=>`|${g.name}|${g.count}|${g.summary}|`)];
for(const g of groups){lines.push('',`## ${g.name}（${g.count}件）`,'',g.play,'','| ID | 遗物 | 稀有度 | 定位 | 完整效果 | 联动与备注 |','|---:|---|---|---|---|---|');for(const r of relics.filter(r=>r.category===g.name))lines.push(`|${r.id}|${cell(r.relicName)}|${r.rarityName}|${cell(r.role)}|${cell(r.description)}|${cell([r.tags,r.note].filter(Boolean).join('；'))}|`);}
lines.push('','## 本次新增及修订的机制与来源核对','');
for(const r of relics.filter(r=>r.mechanics)){lines.push(`### ${r.id} · ${r.relicName}`,'',`- 实际 relicId：\`${r.relicId}\`；GUID：\`${r.guid}\`。`,`- 资产：\`${r.path}\`。`,`- 触发：${r.triggerNames}。`,`- 条件与目标：${r.mechanics.condition}；${r.mechanics.target}。`,`- 次数与叠加：${r.mechanics.limit}；${r.mechanics.stacking}。`,`- 机制备注：${r.note}`,`- 遗物表引用：${r.referencedByRelicTables.length?r.referencedByRelicTables.map(p=>'`'+p+'`').join('、'):'无 RelicTable_SO 直接引用；固定重铸产物。'}`,'');}
await fs.writeFile(`${dir}/遗物体系完整表.md`,lines.join('\n').trimEnd()+'\n');
const icons={};
for(const r of relics){if(!r.iconPath||icons[r.iconGuid])continue;const meta=await sharp(r.iconPath).metadata();let img=sharp(r.iconPath);const [x,y,w,h]=r.iconRect||[0,0,meta.width,meta.height];const [tw,th]=r.iconTextureSize?.length===2?r.iconTextureSize:[meta.width,meta.height];const sx=meta.width/tw,sy=meta.height/th;if(w&&h)img=img.extract({left:Math.round(x*sx),top:Math.round((th-y-h)*sy),width:Math.min(meta.width,Math.round(w*sx)),height:Math.min(meta.height,Math.round(h*sy))});icons[r.iconGuid]='data:image/webp;base64,'+(await img.resize(96,96,{fit:'contain',background:{r:0,g:0,b:0,alpha:0}}).webp({quality:85}).toBuffer()).toString('base64');}
const template=await fs.readFile('tools/relic-catalog/catalog.template.html','utf8');
await fs.writeFile(`${dir}/遗物完整图鉴.html`,template.replace('/*__CATALOG_DATA__*/',JSON.stringify({...catalog,icons}).replaceAll('<','\\u003c')));
console.log(JSON.stringify({count:catalog.count,categories:groups.length,newCount:catalog.newIds.length,icons:Object.keys(icons).length,html:`${dir}/遗物完整图鉴.html`}));
