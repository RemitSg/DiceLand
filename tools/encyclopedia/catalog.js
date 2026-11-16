const $ = id => document.getElementById(id);
const data = JSON.parse($('equipment-data').textContent);
const tabs = [...document.querySelectorAll('[role="tab"]')];
const labels = {
  weapons: {title: '武器图鉴', subtitle: '查技能、骰面与对应图纸。', unit: '把武器', filter: '武器类型', search: '搜索名称、技能、效果或 ID…'},
  blueprints: {title: '图纸图鉴', subtitle: '查卷轴配方、锻造产物与投放。', unit: '张卷轴', filter: '产物类型', search: '搜索卷轴、产物、材料或 ID…'},
  loot: {title: '掉落物图鉴', subtitle: '查敌人掉落与卷轴素材用途。', unit: '种掉落物', filter: '属性', search: '搜索素材、敌人、卷轴或 ID…'},
  relics: {title: '遗物图鉴'},
  enemies: {title: '敌人与技能图鉴'}
};
const rarities = {Common: '普通', Uncommon: '优秀', Rare: '稀有', Epic: '史诗', Legendary: '传说', Trash: '垃圾'};
const types = {Sword: '剑', Axe: '斧', Bow: '弓', Wand: '法杖', Dagger: '匕首'};
const pools = {mainPool: '主图纸池', treasurePool: '普通宝藏', shop: '普通商店'};
const craftLabels = {weapons: '对应卷轴', blueprints: '锻造产物'};
const emptyCraft = {weapons: '无对应卷轴', blueprints: '未配置目标武器'};
const diceNames = {Attack: '攻击', Defense: '防御'};
const collator = new Intl.Collator('zh-CN', {numeric: true});
const rank = Object.keys(rarities);
const escape = value => String(value ?? '').replace(/[&<>"']/g, char => ({'&':'&amp;', '<':'&lt;', '>':'&gt;', '"':'&quot;', "'":'&#39;'}[char]));
const normalize = value => String(value ?? '').normalize('NFKC').toLowerCase();
const number = value => String(value).padStart(3, '0');
const skills = item => item.detail.skills.filter(skill => skill?.id);
const sources = item => Object.keys(pools).filter(key => key === 'shop' ? item.shop.length : item[key]);

const defaults = {query: '', rarity: '', filter: '', sort: 'id', reverse: false, view: 'grid'};
const states = Object.fromEntries(['weapons', 'blueprints', 'loot'].map(tab => [tab, {...defaults, scroll: 0}]));
const byTab = Object.fromEntries(Object.keys(states).map(tab => [tab, new Map(data[tab].map(item => [item.detail.id, item]))]));
const itemType = item => item.detail.weaponType || byTab.weapons.get(item.detail.weapon)?.detail.weaponType;
const filterValue = (item, tab) => tab === 'loot' ? item.detail.attribute : itemType(item);
const byEnemy = new Map(data.enemyIndex.map(enemy => [enemy.guid, enemy]));
const recipes = new Map();
const materialUse = new Map();
for (const item of data.blueprints) {
  const id = item.detail.weapon;
  if (!recipes.has(id)) recipes.set(id, []);
  recipes.get(id).push(item);
  for (const material of item.detail.materials) {
    if (!materialUse.has(material.id)) materialUse.set(material.id, []);
    materialUse.get(material.id).push({item, amount: material.amount});
  }
}
const battleSources = resolveBattleLoot(data.battleLoot);
const dropSources = new Map(data.loot.map(({detail: {id}}) => [id, battleSources.filter(source =>
  source.tables.some(table => table.drops.some(drop => drop.id === id)))]));
const indexes = new Map([
  ...data.weapons.map(item => [item, [
    types[item.detail.weaponType], item.saveId, ...skills(item).flatMap(skill => [skill.name, skill.description]),
    ...(recipes.get(item.detail.id) || []).map(recipe => recipe.detail.name)
  ]]),
  ...data.blueprints.map(item => [item, [
    item.weaponName, types[itemType(item)], ...item.detail.materials.map(material => material.name),
    ...sources(item).map(key => pools[key])
  ]]),
  ...data.loot.map(item => [item, [
    item.detail.attribute, item.saveId,
    ...(materialUse.get(item.detail.id) || []).map(use => use.item.detail.name),
    ...dropSources.get(item.detail.id).flatMap(({name, enemyIds}) => [name, ...enemyIds.map(id => byEnemy.get(id).name)])
  ]])
].map(([item, extra]) => [item, normalize([
  item.detail.name, item.detail.description, rarities[item.detail.rarity], item.assetId,
  number(item.assetId), item.detail.id, item.path, ...extra
].join(' '))]));

const frames = {};
const embeddedViews = {};
let embedded;
let active;
let selected;
let filtered = [];

function writeAddress() {
  const address = new URL(location.href);
  const params = new URLSearchParams({tab: active});
  if (selected) params.set('item', selected);
  if (embeddedViews[active]) params.set('view', embeddedViews[active]);
  address.hash = params.toString();
  if (address.href !== location.href) history.replaceState(null, '', address.href);
}

function mount(tab) {
  if (frames[tab]) return;
  embedded ??= JSON.parse($('embedded-catalogs').textContent);
  const frame = document.createElement('iframe');
  frame.title = labels[tab].title;
  frame.addEventListener('load', () => { frame.dataset.ready = 'true'; }, {once: true});
  frames[tab] = frame;
  // The document and its initial hash are embedded so a copied HTML file still works.
  frame.srcdoc = embedded[tab].replace('/*__INITIAL_CATALOG_HASH__*/', () =>
    JSON.stringify(embeddedViews[tab] || '').replaceAll('<', '\\u003c'));
  $(tab).append(frame);
}

function activate(tab, id) {
  if (active === tab) {
    if (id) openDetail(id);
    return;
  }
  if (states[active]) states[active].scroll = $(active).scrollTop;
  active = tab;
  selected = undefined;
  for (const button of tabs) {
    const panel = button.getAttribute('aria-controls');
    button.setAttribute('aria-selected', String(panel === tab));
    button.tabIndex = panel === tab ? 0 : -1;
    $(panel).hidden = panel !== tab;
  }
  if (states[tab]) {
    $(tab).append($('equipment'));
    setupEquipment(tab);
    $(tab).scrollTop = states[tab].scroll;
    if (id) openDetail(id);
  } else {
    mount(tab);
  }
  writeAddress();
}

function openEnemyDropSection(id) {
  if (!byEnemy.has(id)) return;
  if ($('detail').open) $('detail').close();
  const frame = frames.enemies;
  if (!frame) {
    embeddedViews.enemies = new URLSearchParams({enemy: id, loot: '1'}).toString();
    activate('enemies');
    return;
  }
  activate('enemies');
  const open = () => frame.contentWindow.postMessage({openEnemyLoot: id}, '*');
  if (frame.dataset.ready) open();
  else frame.addEventListener('load', open, {once: true});
}

window.addEventListener('message', event => {
  const message = event.data;
  if (!message || !Object.hasOwn(frames, message.catalog) || frames[message.catalog].contentWindow !== event.source) return;
  if (typeof message.hash === 'string') {
    embeddedViews[message.catalog] = message.hash;
    if (message.catalog === active) writeAddress();
  }
  if (Object.hasOwn(labels, message.openTab)) {
    activate(message.openTab);
    $('tab-' + message.openTab).focus();
  }
  if (message.catalog === 'enemies' && byTab.loot.has(message.openLoot)) activate('loot', message.openLoot);
});

function setupEquipment(tab) {
  const {title, subtitle, unit, filter, search} = labels[tab];
  $('title').textContent = title;
  $('subtitle').textContent = subtitle;
  $('total').textContent = data[tab].length;
  $('unit').textContent = unit;
  $('filter-label').textContent = filter;
  $('query').placeholder = search;
  $('coverage').textContent = `武器、图纸和掉落物于 ${data.date} 从团结资产导出。技能、配方和掉落表使用游戏实际引用。`;
  $('coverage-note').textContent = tab === 'loot'
    ? '掉落来源记录敌群材料表和特殊战斗结算的素材配置。实际结果受保底和额外奖励影响。'
    : '图纸投放标记对应主图纸池、普通宝藏池和普通商店池。实际抽取受游戏规则影响。';
  const available = new Set(data[tab].map(item => filterValue(item, tab)).filter(Boolean));
  const options = tab === 'loot' ? [...available].sort(collator.compare).map(name => [name, name]) :
    Object.entries(types).filter(([key]) => available.has(key));
  $('filter').replaceChildren(new Option(tab === 'loot' ? '全部属性' : '全部类型', ''),
    ...options.map(([key, name]) => new Option(name, key)));

  const counts = new Map(rank.map(rarity => [rarity, 0]));
  for (const item of data[tab]) counts.set(item.detail.rarity, counts.get(item.detail.rarity) + 1);
  $('rarity-options').innerHTML = [['', '全部稀有度', data[tab].length],
    ...rank.filter(rarity => counts.get(rarity)).map(rarity => [rarity, rarities[rarity], counts.get(rarity)])
  ].map(([key, label, count]) => `<button type="button" data-rarity="${key}" aria-pressed="false"><span>${label}</span><span>${count}</span></button>`).join('');
  syncControls();
  render();
}

function syncControls() {
  const state = states[active];
  $('query').value = state.query;
  $('filter').value = state.filter;
  $('sort').value = state.sort;
}

function searchMatches(item, query) {
  const range = /^(\d+)\s*[-–—~至]\s*(\d+)$/.exec(query);
  if (range) {
    const [a, b] = range.slice(1).map(Number);
    return item.assetId >= Math.min(a, b) && item.assetId <= Math.max(a, b);
  }
  if (/^(?:id[:：]\s*)?\d+$/.test(query)) return item.assetId === Number(query.replace(/^id[:：]\s*/, ''));
  return query.split(/\s+/).every(word => indexes.get(item).includes(word));
}

function compare(a, b, sort) {
  if (sort === 'rarity') return rank.indexOf(a.detail.rarity) - rank.indexOf(b.detail.rarity) || a.assetId - b.assetId;
  if (sort === 'name') return collator.compare(a.detail.name, b.detail.name) || a.assetId - b.assetId;
  return a.assetId - b.assetId || collator.compare(a.detail.name, b.detail.name);
}

function render() {
  const state = states[active];
  const query = normalize(state.query).trim();
  filtered = data[active].filter(item =>
    (!state.rarity || item.detail.rarity === state.rarity) &&
    (!state.filter || filterValue(item, active) === state.filter) && searchMatches(item, query));
  filtered.sort((a, b) => compare(a, b, state.sort) * (state.reverse ? -1 : 1));
  for (const button of $('rarity-options').querySelectorAll('button')) button.setAttribute('aria-pressed', String(button.dataset.rarity === state.rarity));
  for (const button of document.querySelectorAll('[data-view]')) button.setAttribute('aria-pressed', String(button.dataset.view === state.view));
  $('direction').textContent = state.reverse ? '↓ 降序' : '↑ 升序';
  $('status').textContent = `显示 ${filtered.length} / ${data[active].length}`;
  $('results').classList.toggle('list', state.view === 'list');
  $('results').innerHTML = filtered.length ? filtered.map(item => card(item, active)).join('') : '<p class="empty">没有匹配的条目。</p>';
}

function icon(item, tab) {
  const image = data.icons[item.icon?.key];
  return `<span class="item-icon" aria-hidden="true">${image ? `<img src="${image}" alt="" loading="lazy">` : {weapons: '武', blueprints: '卷', loot: '材'}[tab]}</span>`;
}

function rarity(item) {
  return `<span class="tag" data-rarity="${escape(item.detail.rarity)}">${escape(rarities[item.detail.rarity])}</span>`;
}

function cardSummary(item, tab) {
  const {detail} = item;
  if (tab === 'loot') return {
    line: detail.description || '查看掉落来源与用途。',
    meta: [detail.attribute, `${materialUse.get(detail.id)?.length || 0} 张卷轴使用`].filter(Boolean).join(' · ')
  };
  if (tab === 'weapons') {
    const names = skills(item).map(skill => skill.name).join(' · ');
    return {
      line: names ? `技能　${names}` : `基础骰 ${detail.fallbackMinPower}–${detail.fallbackMaxPower}`,
      meta: `${types[detail.weaponType]} · ${detail.speedButtonCount} 速度槽`
    };
  }
  const materials = detail.materials.map(material => `${material.name} ×${material.amount}`).join(' · ');
  return {line: materials ? `材料　${materials}` : '未配置有效材料', meta: types[itemType(item)] || ''};
}

function card(item, tab) {
  const {detail} = item;
  const {line, meta} = cardSummary(item, tab);
  const label = craftLabels[tab];
  const related = label ? craftLinks(item, tab) : '';
  return `<article class="entry"><button class="card" type="button" data-open="${detail.id}">
    <span class="identity">${icon(item, tab)}<span>
      <span class="item-name">${escape(detail.name)}</span><span class="item-number">ID ${number(item.assetId)}</span>
    </span></span>
    <span class="card-line">${escape(line)}</span>
    <span class="card-bottom">${rarity(item)}${meta ? `<span>${escape(meta)}</span>` : ''}</span>
  </button>
  ${label ? `<div class="card-craft" role="group" aria-label="${label}">
    ${related ? `<span aria-hidden="true">${label}</span>${related}` : `<span>${emptyCraft[tab]}</span>`}
  </div>` : ''}
  </article>`;
}

function section(title, content) {
  return `<section class="detail-section"><h3>${title}</h3>${content}</section>`;
}

function relation(tab, id, name, meta = '') {
  return `<button class="relation" type="button" data-jump="${tab}" data-id="${id}">
    <span class="relation-name">${escape(name)}</span><span class="relation-action">
      ${meta ? `<span class="relation-count">${escape(meta)}</span>` : ''}
      <span aria-hidden="true">↗</span></span></button>`;
}

function craftLinks(item, tab) {
  if (tab === 'weapons') return (recipes.get(item.detail.id) || []).map(({detail}) =>
    relation('blueprints', detail.id, detail.name)).join('');
  const weapon = byTab.weapons.get(item.detail.weapon)?.detail;
  return weapon ? relation('weapons', weapon.id, weapon.name) : '';
}

function rulesWithoutRepeatedStats(skill, cost) {
  const [header, ...lines] = skill.description.split('\n');
  if (header !== skill.name) return skill.description;
  const split = lines.indexOf('');
  if (split < 0) return skill.description;
  const repeated = new Set([
    `技能：${skill.name}`, `能耗：${cost}`, `骰子数：${skill.dice.length}`,
    ...skill.dice.map((die, index) => `第 ${index + 1} ${diceNames[die.category]}骰：${die.min} ~ ${die.max}`)
  ]);
  return [...lines.slice(0, split).filter(line => !repeated.has(line)), ...lines.slice(split)].join('\n').trim();
}

function skillDetail(skill, isDefault) {
  const cost = Math.max(0, skill.energyCost);
  const rules = rulesWithoutRepeatedStats(skill, cost);
  return `<details class="skill" open><summary>${escape(skill.name)}
    <span class="skill-badges"><span class="tag">能耗 ${cost}</span>${isDefault ? '<span class="tag">默认技能</span>' : ''}</span></summary>
    <ol class="dice" aria-label="骰面">${skill.dice.map((die, index) =>
      `<li data-kind="${escape(die.category)}">${index + 1} · ${escape(diceNames[die.category] || die.category)} ${die.min}–${die.max}</li>`).join('')}</ol>
    ${rules ? `<p class="skill-rule">${escape(rules)}</p>` : ''}
  </details>`;
}

function weaponDetail(item) {
  const {detail} = item;
  const skillRows = skills(item).map(skill => skillDetail(skill, item.hasDefaultSkill && skill.index === 0)).join('');
  return `<dl class="stats">
    <div><dt>速度槽</dt><dd>${detail.speedButtonCount}</dd></div>
    <div><dt>融合上限</dt><dd>${item.fusionLimit}</dd></div>
    <div><dt>技能数</dt><dd>${skills(item).length}</dd></div>
  </dl>
  ${section('武器技能', skillRows || `<p>基础攻击为单骰 ${detail.fallbackMinPower}–${detail.fallbackMaxPower}。</p>`)}`;
}

function blueprintDetail(item) {
  const ingredients = item.detail.materials.map(material =>
    `<li>${relation('loot', material.id, material.name, '×' + material.amount)}</li>`).join('');
  return section('所需材料', ingredients ? `<ul class="materials">${ingredients}</ul>` : '<p>未配置有效材料，当前无法锻造。</p>');
}

function lootDetail(item) {
  const {id} = item.detail;
  const dropRows = dropSources.get(id).map(source => {
    const rows = source.tables.flatMap(table => table.drops.filter(drop => drop.id === id).map(drop =>
      `<li><span>${escape(lootTableName(table))}</span><strong>${escape(lootDropText(drop, table))}</strong></li>`)).join('');
    const enemies = [...new Set(source.enemyIds)].map(guid => {
      const enemy = byEnemy.get(guid);
      const label = enemy.name + (enemy.variant ? `（${enemy.variant}）` : '');
      return relation('enemies', guid, label, number(enemy.number));
    }).join('');
    return `<div class="loot-source"><h4>${escape(lootSourceName(source))}</h4>
      <ul class="drop-rows">${rows}</ul><div class="enemy-links">${enemies}</div></div>`;
  }).join('');
  const formulas = (materialUse.get(id) || []).map(({item: {detail}, amount}) =>
    relation('blueprints', detail.id, detail.name, '×' + amount)).join('');
  return section('战斗掉落', dropRows || '<p>当前没有关联的战斗素材掉落。</p>') +
    section('用于锻造', formulas || '<p>现有图纸未使用此素材。</p>') +
    (item.inShop ? section('商店', '<span class="tag">普通商店商品池</span>') : '');
}

function placements(item, weapon) {
  const tags = sources(item).map(key => `<span class="tag">${pools[key]}</span>`).join('');
  const prices = item.shop.map(good => {
    const price = good.minPrice === good.maxPrice ? good.minPrice : `${good.minPrice}–${good.maxPrice}`;
    return `<li><span>普通商店 · ${price} 金币</span><span>权重 ${good.weight}</span></li>`;
  }).join('');
  if (weapon && !prices) return '';
  return section(weapon ? '普通商店配置' : '投放池', `${tags ? `<div class="sources">${tags}</div>` : '<p>未出现在这三个投放池中。</p>'}
    ${prices ? `<ul class="prices">${prices}</ul>` : ''}`);
}

function openDetail(id) {
  const item = byTab[active]?.get(id);
  if (!item) return;
  selected = id;
  const {detail} = item;
  const index = filtered.indexOf(item);
  const titles = {weapons: '武器', blueprints: '图纸', loot: '掉落物'};
  const kinds = {weapons: types[detail.weaponType], blueprints: '卷轴', loot: detail.attribute};
  const fields = {weapons: 'id', blueprints: 'blueprintID', loot: 'lootID'};
  const details = {weapons: weaponDetail, blueprints: blueprintDetail, loot: lootDetail};
  const label = craftLabels[active];
  $('detail-label').textContent = titles[active] + '详情';
  $('position').textContent = index < 0 ? '' : `${index + 1} / ${filtered.length}`;
  $('previous').disabled = index < 1;
  $('next').disabled = index < 0 || index === filtered.length - 1;
  $('detail-content').innerHTML = `
    <div class="identity detail-heading">${icon(item, active)}<div>
      <span class="item-number">ID ${number(item.assetId)}</span>
      <h2 id="detail-name">${escape(detail.name)}</h2>
      ${rarity(item)} ${escape(kinds[active])}
    </div></div>
    ${label ? section(label, craftLinks(item, active) || `<p>${emptyCraft[active]}</p>`) : ''}
    ${detail.description ? `<p class="description">${escape(detail.description)}</p>` : ''}
    ${details[active](item)}
    ${active === 'loot' ? '' : placements(item, active === 'weapons')}
    <details class="asset"><summary>资产信息</summary><dl>
      <dt>${fields[active]}</dt><dd>${item.assetId}</dd>
      ${item.saveId ? `<dt>saveId</dt><dd>${escape(item.saveId)}</dd>` : ''}
      <dt>GUID</dt><dd>${detail.id}</dd><dt>路径</dt><dd>${escape(item.path)}</dd>
    </dl></details>`;
  if (!$('detail').open) $('detail').showModal();
  else $('close').focus();
  $('detail-content').scrollTop = 0;
  writeAddress();
}

function step(offset) {
  const index = filtered.findIndex(item => item.detail.id === selected);
  if (index >= 0 && filtered[index + offset]) openDetail(filtered[index + offset].detail.id);
}

for (const tab of tabs) tab.addEventListener('click', () => activate(tab.getAttribute('aria-controls')));
tabs[0].parentElement.addEventListener('keydown', event => {
  const index = tabs.indexOf(event.target);
  if (index < 0) return;
  const next = {ArrowLeft: (index + tabs.length - 1) % tabs.length, ArrowRight: (index + 1) % tabs.length, Home: 0, End: tabs.length - 1}[event.key];
  if (next === undefined) return;
  event.preventDefault();
  tabs[next].focus();
  tabs[next].click();
});

$('query').addEventListener('input', event => { states[active].query = event.target.value; render(); });
for (const key of ['sort', 'filter']) {
  $(key).addEventListener('change', event => { states[active][key] = event.target.value; render(); });
}
$('direction').addEventListener('click', () => { states[active].reverse = !states[active].reverse; render(); });
$('reset').addEventListener('click', () => { Object.assign(states[active], defaults); syncControls(); render(); });
$('rarity-options').addEventListener('click', event => {
  const button = event.target.closest('button[data-rarity]');
  if (!button) return;
  states[active].rarity = button.dataset.rarity;
  render();
});
for (const button of document.querySelectorAll('[data-view]')) {
  button.addEventListener('click', () => { states[active].view = button.dataset.view; render(); });
}
for (const id of ['results', 'detail-content']) {
  $(id).addEventListener('click', event => {
    const button = event.target.closest('[data-open], [data-jump]');
    if (!button) return;
    if (button.dataset.open) openDetail(button.dataset.open);
    else if (button.dataset.jump === 'enemies') openEnemyDropSection(button.dataset.id);
    else activate(button.dataset.jump, button.dataset.id);
  });
}
$('previous').addEventListener('click', () => step(-1));
$('next').addEventListener('click', () => step(1));
$('close').addEventListener('click', () => $('detail').close());
$('detail').addEventListener('close', () => {
  if ($('detail').open) return;
  if (selected && states[active]) {
    const card = $('results').querySelector(`[data-open="${selected}"]`);
    (card || $('query')).focus({preventScroll: true});
  }
  selected = undefined;
  writeAddress();
});
$('detail').addEventListener('click', event => {
  if (event.target === $('detail') && event.clientX < $('detail').getBoundingClientRect().left) $('detail').close();
});
document.addEventListener('keydown', event => {
  if (event.ctrlKey || event.metaKey || event.altKey) return;
  if ($('detail').open) {
    if (event.key === 'ArrowLeft') step(-1);
    if (event.key === 'ArrowRight') step(1);
  } else if (event.key === '/' && states[active] && !event.target.matches('input, select, textarea')) {
    event.preventDefault();
    $('query').focus();
  }
});

const address = new URLSearchParams(location.hash.slice(1));
const requestedTab = address.get('tab');
const initial = Object.hasOwn(labels, requestedTab) ? requestedTab : 'weapons';
if (!states[initial]) embeddedViews[initial] = address.get('view') || '';
activate(initial, address.get('item'));
