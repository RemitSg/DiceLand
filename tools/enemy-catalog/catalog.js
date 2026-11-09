'use strict';

const data = JSON.parse(document.getElementById('catalog-data').textContent);
const $ = id => document.getElementById(id);
const escape = value => String(value ?? '').replace(/[&<>"']/g, char => ({
  '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
}[char]));
const normalize = value => String(value ?? '').normalize('NFKC').toLowerCase();
const numberLabel = value => String(value).padStart(3, '0');
const rangeLabel = (low, high) => low === high ? String(low) : `${low}–${high}`;
const collator = new Intl.Collator('zh-CN', {numeric: true});
const star = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="m12 3 2.7 5.5 6.1.9-4.4 4.3 1 6.1-5.4-2.9-5.4 2.9 1-6.1-4.4-4.3 6.1-.9Z"/></svg>';
const shield = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="m12 3 7 3v6c0 4-3 7-7 9-4-2-7-5-7-9V6l7-3Z"/><path d="M9 12h6m-3-3v6"/></svg>';

function splitSkill(skill) {
  const dice = [], rules = [];
  let energy = '';
  for (const [index, original] of skill.detail.split('\n').entries()) {
    const line = original.trim();
    if (!line || (index === 0 && line === skill.name) || /^技能[：:]|^骰子数[：:]/.test(line)) continue;
    const die = line.match(/^第\s*(\d+)\s*(攻击骰|防御骰)[：:]\s*(-?\d+)\s*[~～–-]\s*(-?\d+)$/);
    if (die) dice.push({number: die[1], type: die[2], low: die[3], high: die[4]});
    else if (/^能耗[：:]/.test(line)) energy = line.replace(/^能耗[：:]\s*/, '');
    else rules.push(line);
  }
  return {...skill, dice, rules, energy, search: normalize([skill.name, skill.detail, ...skill.roles].join(' '))};
}

const placementLabels = ['外圈普通', '外圈精英', '中圈普通', '中圈精英', 'Boss池'];
const dungeonReference = text => /特殊副本/.test(text);
const categories = [
  {key: 'all', label: '全部敌人'},
  ...placementLabels.map(label => ({key: label, label: label === 'Boss池' ? '首领' : label})),
  {key: 'event', label: '事件引用'},
  {key: 'dungeon', label: '特殊副本'},
  {key: 'other', label: '未标注投放'}
];
const categoryLabel = new Map(categories.map(category => [category.key, category.label]));
const countsByNumber = new Map();
for (const enemy of data.enemies) countsByNumber.set(enemy.number, (countsByNumber.get(enemy.number) || 0) + 1);

const enemies = data.enemies.map(enemy => {
  const speed = enemy.stages.flatMap(stage => stage.speed.match(/\d+/g).map(Number));
  const slots = enemy.stages.map(stage => stage.slots);
  const categoryKeys = enemy.placements.slice();
  if (enemy.groups.some(group => group.path.includes('/Events/')) ||
      enemy.special_references.some(ref => !dungeonReference(ref))) categoryKeys.push('event');
  if (enemy.special_references.some(dungeonReference)) categoryKeys.push('dungeon');
  if (!categoryKeys.length) categoryKeys.push('other');
  const labels = categoryKeys.map(key => categoryLabel.get(key) || key);
  const skills = enemy.skills.map(splitSkill);
  return {
    ...enemy, skills, categoryKeys, labels,
    variant: countsByNumber.get(enemy.number) > 1 ? enemy.path.split('/').slice(-2).join('/') : '',
    speedMax: Math.max(...speed),
    slotsMax: Math.max(...slots),
    speedLabel: rangeLabel(Math.min(...speed), Math.max(...speed)),
    slotsLabel: rangeLabel(Math.min(...slots), Math.max(...slots)),
    search: normalize([
      enemy.name, enemy.path, enemy.guid, enemy.description, ...labels, ...enemy.attributes,
      ...enemy.passives, ...enemy.configured_mechanics, ...enemy.special_references,
      ...enemy.groups.flatMap(group => [group.name, group.path]),
      ...enemy.stages.map(stage => stage.name),
      ...skills.map(skill => skill.search)
    ].join(' '))
  };
});
const byGuid = new Map(enemies.map(enemy => [enemy.guid, enemy]));
const attributes = [...new Set(enemies.flatMap(enemy => enemy.attributes))].sort(collator.compare);
const defaults = {category: 'all', query: '', attribute: '', sort: 'number', order: 'asc', view: 'grid', favoritesOnly: false};
const sorters = {
  number: (a, b) => a.number - b.number,
  name: (a, b) => collator.compare(a.name, b.name),
  hp: (a, b) => a.hp - b.hp,
  speed: (a, b) => a.speedMax - b.speedMax,
  slots: (a, b) => a.slotsMax - b.slotsMax,
  skills: (a, b) => a.skills.length - b.skills.length
};
const storageKey = 'diceland.enemy-catalog.favorites';
let favorites = new Set();
try {
  const stored = JSON.parse(localStorage.getItem(storageKey) || '[]');
  if (Array.isArray(stored)) favorites = new Set(stored.filter(guid => byGuid.has(guid)));
} catch { /* File URLs may have storage disabled. */ }

let state = {...defaults};
let filtered = [];
let terms = [];
let highlighted = null;
let current = null;

function readHash() {
  const legacy = location.hash.match(/^#enemy-(\d+)$/);
  if (legacy) return {state: {...defaults}, enemy: enemies[Number(legacy[1])]?.guid};
  const params = new URLSearchParams(location.hash.slice(1));
  const category = params.get('category');
  const attribute = params.get('attribute');
  const sort = params.get('sort');
  return {
    state: {
      category: categoryLabel.has(category) ? category : defaults.category,
      query: params.get('q') || '',
      attribute: attributes.includes(attribute) ? attribute : '',
      sort: Object.hasOwn(sorters, sort) ? sort : defaults.sort,
      order: params.get('order') === 'desc' ? 'desc' : 'asc',
      view: params.get('view') === 'list' ? 'list' : 'grid',
      favoritesOnly: params.get('favorites') === '1'
    },
    enemy: params.get('enemy')
  };
}

function writeHash() {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(state)) {
    if (value === defaults[key]) continue;
    const name = key === 'query' ? 'q' : key === 'favoritesOnly' ? 'favorites' : key;
    params.set(name, value === true ? '1' : value);
  }
  if (current) params.set('enemy', current.guid);
  const url = new URL(location.href);
  url.hash = params.toString();
  if (url.href !== location.href) history.replaceState(null, '', url.href);
}

function parseQuery(query) {
  return normalize(query).trim()
    .replace(/(\d+)\s*[-–—~至]\s*(\d+)/g, '$1–$2')
    .split(/\s+/).filter(Boolean).map(word => {
      const range = word.match(/^#?(\d+)–#?(\d+)$/);
      if (range) {
        const endpoints = range.slice(1).map(Number);
        return {from: Math.min(...endpoints), to: Math.max(...endpoints)};
      }
      const id = word.match(/^(?:id|enemyid)[:=](\d+)$/);
      if (id) return {id: Number(id[1])};
      const number = word.match(/^#?(\d+)$/);
      if (number) return {number: Number(number[1])};
      return {text: word};
    });
}

function matches(enemy, term) {
  if (term.from !== undefined) return enemy.number >= term.from && enemy.number <= term.to;
  if (term.id !== undefined) return enemy.enemy_id === term.id;
  if (term.number !== undefined) return enemy.number === term.number;
  return enemy.search.includes(term.text);
}

function skillHit(skill) {
  return terms.some(term => term.text && skill.search.includes(term.text));
}

function highlight(value) {
  if (!highlighted) return escape(value);
  return String(value ?? '').split(highlighted).map((part, index) =>
    index % 2 ? `<mark>${escape(part)}</mark>` : escape(part)).join('');
}

function portrait(enemy) {
  const image = data.portraits[enemy.portrait];
  return `<span class="portrait">${image ? `<img src="${escape(image)}" alt="" loading="lazy">` : shield}</span>`;
}

function badges(enemy) {
  const encounters = enemy.labels.map(label => {
    const tone = label === '首领' ? 'boss' : label.includes('精英') ? 'elite' : '';
    return `<span class="badge ${tone}">${escape(label)}</span>`;
  });
  if (enemy.attributes.length) encounters.push(`<span class="badge attr" title="属性取自所属敌群">${escape(enemy.attributes.join(' · '))}</span>`);
  return encounters.join('');
}

function stats(enemy, className) {
  return `<span class="${className}" title="速度与速度槽为快照中所有形态的范围">
    ${[['生命', enemy.hp], ['速度', enemy.speedLabel], ['速度槽', enemy.slotsLabel]].map(([label, value]) =>
      `<span class="stat"><b>${escape(value)}</b><small>${label}</small></span>`).join('')}
  </span>`;
}

function identity(enemy) {
  const id = enemy.number !== enemy.enemy_id ? ` · ID ${enemy.enemy_id}` : '';
  return `<span class="card-id">NO. ${numberLabel(enemy.number)}${id}</span>
    <span class="card-name">${highlight(enemy.name)}</span>
    ${enemy.variant ? `<span class="variant">${escape(enemy.variant)}</span>` : ''}`;
}

function renderCard(enemy) {
  const hit = enemy.skills.filter(skillHit);
  const excerpt = hit.length ? `匹配技能 · ${hit.slice(0, 3).map(skill => skill.name).join('、')}${hit.length > 3 ? ' 等' : ''}` :
    enemy.passives[0] || enemy.description || enemy.configured_mechanics[0] || enemy.skills.map(skill => skill.name).join('、');
  const saved = favorites.has(enemy.guid);
  return `<article class="enemy" data-enemy="${enemy.guid}">
    <button class="favorite" type="button" data-favorite="${enemy.guid}" aria-pressed="${saved}" aria-label="收藏 ${escape(enemy.name)}" title="${saved ? '取消收藏' : '收藏'}">${star}</button>
    <button class="card-open" type="button" data-open="${enemy.guid}" aria-label="查看 ${numberLabel(enemy.number)} ${escape(enemy.name)}${enemy.variant ? ' ' + escape(enemy.variant) : ''}">
      <span class="card-top">${portrait(enemy)}<span class="identity">${identity(enemy)}</span></span>
      ${stats(enemy, 'card-stats')}
      <span class="card-meta">${badges(enemy)}</span>
      <span class="card-excerpt${hit.length ? ' hit' : ''}">${highlight(excerpt)}</span>
      <span class="card-bottom"><span>${enemy.skills.length} 个技能${enemy.stages.length > 1 ? ` · ${enemy.stages.length} 个形态` : ''}</span><span class="read-more">查看档案 ↗</span></span>
    </button>
  </article>`;
}

function render() {
  terms = parseQuery(state.query);
  const words = [...new Set(terms.flatMap(term => term.text ? [term.text] : []))].sort((a, b) => b.length - a.length);
  highlighted = words.length ? new RegExp('(' + words.map(word => word.replace(/[.*+?^$(){}|[\]\\]/g, '\\$&')).join('|') + ')', 'gi') : null;
  filtered = enemies.filter(enemy =>
    (state.category === 'all' || enemy.categoryKeys.includes(state.category)) &&
    (!state.attribute || enemy.attributes.includes(state.attribute)) &&
    (!state.favoritesOnly || favorites.has(enemy.guid)) &&
    terms.every(term => matches(enemy, term)));
  filtered.sort((a, b) => {
    const difference = sorters[state.sort](a, b);
    return difference * (state.order === 'desc' ? -1 : 1) ||
      a.number - b.number || collator.compare(a.path, b.path);
  });

  $('query').value = state.query;
  $('attribute').value = state.attribute;
  $('sort').value = state.sort;
  $('direction').textContent = state.order === 'asc' ? '↑ 升序' : '↓ 降序';
  $('direction').setAttribute('aria-label', state.order === 'asc' ? '当前升序，切换为降序' : '当前降序，切换为升序');
  $('view-grid').setAttribute('aria-pressed', state.view === 'grid');
  $('view-list').setAttribute('aria-pressed', state.view === 'list');
  $('favorites-only').setAttribute('aria-pressed', state.favoritesOnly);
  $('favorites-only').textContent = state.favoritesOnly ? `★ 收藏 ${favorites.size}` : `☆ 收藏 ${favorites.size}`;
  for (const button of $('categories').querySelectorAll('button')) {
    button.setAttribute('aria-pressed', state.category === button.dataset.category);
  }
  $('heading').textContent = categoryLabel.get(state.category) + (state.favoritesOnly ? ' · 已收藏' : '');
  $('result-count').textContent = `显示 ${filtered.length} / ${enemies.length} 个敌人 · ${$('sort').selectedOptions[0].textContent}${state.order === 'asc' ? '升序' : '降序'}`;
  $('results').className = state.view === 'list' ? 'grid list' : 'grid';
  $('results').innerHTML = filtered.length ? filtered.map(renderCard).join('') :
    '<div class="empty"><h3>没有符合条件的敌人</h3><p>试试其他关键词，或重置筛选条件。</p><button type="button" data-reset>重置筛选</button></div>';
  $('print').disabled = filtered.length === 0;
  writeHash();
}

function renderSkill(skill) {
  const hit = skillHit(skill);
  const condition = skill.roles.some(role => role.startsWith('条件技能'));
  return `<details class="skill-card${hit ? ' matched' : ''}" data-skill="${skill.guid}"${hit ? ' open' : ''}>
    <summary><span>
      <span class="skill-title">${highlight(skill.name)}</span>
      <span class="skill-meta">
        <span class="badge">阶段 ${escape(skill.stage_restriction.join(' / '))}</span>
        ${skill.dice.length ? `<span class="badge attr">${skill.dice.length} 骰</span>` : ''}
        ${condition ? '<span class="badge elite">条件技能</span>' : ''}
        ${skill.ally_grant ? '<span class="badge grant">授予玩家</span>' : ''}
      </span>
    </span></summary>
    <div class="skill-body">
      ${skill.dice.length ? `<div class="dice">${skill.dice.map(die => `<span class="die ${die.type === '防御骰' ? 'defense' : 'attack'}"><span>${die.number} · ${die.type}</span><b>${rangeLabel(die.low, die.high)}</b></span>`).join('')}</div>` : ''}
      <div class="skill-roles">
        ${skill.roles.map(role => `<div>${highlight(role)}</div>`).join('')}
        ${skill.ally_grant && skill.energy ? `<div>玩家能耗 ${escape(skill.energy)}</div>` : ''}
        ${skill.transform_stage > 0 ? `<div>使用后切换到阶段 ${escape(skill.transform_stage)}</div>` : ''}
      </div>
      ${skill.rules.map(line => /^(描述|词条)[：:]$/.test(line)
        ? `<p class="skill-rule rule-heading">${escape(line.slice(0, -1))}</p>`
        : `<p class="skill-rule">${highlight(line)}</p>`).join('')}
      <details class="technical">
        <summary>技能资产与原始文本</summary>
        <p><code>${escape(skill.path)}</code><br>GUID <code>${skill.guid}</code></p>
        <pre>${escape(skill.detail)}</pre>
      </details>
    </div>
  </details>`;
}

function ruleBlock(title, lines, empty) {
  return `<div class="rule-block"><h4>${title}</h4>${lines.length ?
    `<ul>${lines.map(line => `<li>${highlight(line)}</li>`).join('')}</ul>` :
    `<p class="no-record">${empty}</p>`}</div>`;
}

function renderDetail(enemy) {
  const groups = [...new Map(enemy.groups.map(group => [group.path, group])).values()];
  return `<section id="overview" class="drawer-section">
    <div class="detail-hero">${portrait(enemy)}<div>
      <div class="card-id">NO. ${numberLabel(enemy.number)} · enemyID ${enemy.enemy_id}</div>
      <h2 id="detail-name">${highlight(enemy.name)}</h2>
      <div class="detail-tags">${badges(enemy)}</div>
    </div></div>
    ${stats(enemy, 'detail-stats')}
    ${enemy.description ? `<p class="description">${highlight(enemy.description)}</p>` : ''}
    <div class="phase-list" aria-label="各形态速度与速度槽">
      ${enemy.stages.map(stage => `<div class="phase"><strong>${escape(stage.name)}</strong><span>速度 ${escape(stage.speed)}</span><span>${stage.slots} 个速度槽</span></div>`).join('')}
    </div>
  </section>
  <section id="skills" class="drawer-section">
    <div class="section-heading"><h3>技能</h3><p>${enemy.skills.length} 个关联技能</p></div>
    ${enemy.skills.map(renderSkill).join('') || '<p class="no-record">快照未记录关联技能。</p>'}
  </section>
  <section id="passives" class="drawer-section">
    <div class="section-heading"><h3>被动与机制</h3></div>
    ${ruleBlock('被动文案', enemy.passives, '快照未记录被动文案。')}
    ${ruleBlock('实际机制配置', enemy.configured_mechanics, '快照未记录额外的通用机制配置。')}
  </section>
  <section id="encounters" class="drawer-section">
    <div class="section-heading"><h3>投放与来源</h3></div>
    ${ruleBlock('地图投放', enemy.placements, '快照未标记固定地图投放。')}
    <div class="rule-block"><h4>所属敌群</h4>${groups.length ? `<ul>${groups.map(group =>
      `<li>${highlight(group.name)}${group.placement.length ? ' · ' + escape(group.placement.join('、')) : ''}<code class="source-path">${escape(group.path)}</code></li>`).join('')}</ul>` :
      '<p class="no-record">快照未记录所属敌群。</p>'}</div>
    ${ruleBlock('事件与副本引用', enemy.special_references, '快照未记录事件或副本引用。')}
    <details class="technical"><summary>敌人资产身份</summary>
      <p>目录编号 ${numberLabel(enemy.number)} · 实际 enemyID ${enemy.enemy_id}<br>
        GUID <code>${enemy.guid}</code><br><code>${escape(enemy.path)}</code></p>
      <p>属性取自所属敌群 · ${escape(enemy.attributes.join('、') || '快照未记录')}</p>
    </details>
  </section>`;
}

function openDetail(guid, jumpToHit = false) {
  const enemy = byGuid.get(guid);
  if (!enemy) return;
  current = enemy;
  const index = filtered.indexOf(enemy);
  $('prev').disabled = index <= 0;
  $('next').disabled = index < 0 || index >= filtered.length - 1;
  $('detail-position').textContent = index < 0 ? '独立档案' : `${index + 1} / ${filtered.length}`;
  $('detail-skill-count').textContent = enemy.skills.length;
  $('detail-content').innerHTML = renderDetail(enemy);
  const dialog = $('detail');
  if (!dialog.open) dialog.showModal();
  dialog.scrollTop = 0;
  const hit = dialog.querySelector('.skill-card.matched');
  if (jumpToHit && hit) {
    hit.scrollIntoView({block: 'start'});
    hit.querySelector('summary').focus({preventScroll: true});
  }
  writeHash();
}

function adjacent(offset) {
  const index = filtered.indexOf(current);
  const enemy = index < 0 ? null : filtered[index + offset];
  if (enemy) openDetail(enemy.guid);
}

function reset() {
  state = {...defaults, view: state.view};
  render();
  $('query').focus();
}

function renderPrint() {
  $('print-view').innerHTML = '<h1>DiceLand · 敌人与技能图鉴</h1>' + filtered.map(enemy =>
    `<article class="print-enemy"><h2>${numberLabel(enemy.number)} · ${escape(enemy.name)} · enemyID ${enemy.enemy_id}</h2>
      <p>生命 ${enemy.hp} · 属性 ${escape(enemy.attributes.join('、') || '快照未记录')}</p>
      <p>${enemy.stages.map(stage => `${escape(stage.name)} · 速度 ${escape(stage.speed)} · ${stage.slots} 槽`).join(' / ')}</p>
      ${enemy.description ? `<p>${escape(enemy.description)}</p>` : ''}
      ${ruleBlock('地图投放', enemy.placements, '快照未标记固定地图投放。')}
      ${ruleBlock('所属敌群', enemy.groups.map(group => `${group.name}（${group.path}）`), '快照未记录所属敌群。')}
      ${ruleBlock('事件与副本引用', enemy.special_references, '快照未记录事件或副本引用。')}
      ${ruleBlock('被动文案', enemy.passives, '快照未记录被动文案。')}
      ${ruleBlock('实际机制配置', enemy.configured_mechanics, '快照未记录额外的通用机制配置。')}
      ${enemy.skills.map(skill => `<section class="print-skill"><h3>${escape(skill.name)}</h3>
        <p>适用阶段 ${escape(skill.stage_restriction.join(' / '))}<br>${skill.roles.map(escape).join('<br>')}
        ${skill.transform_stage > 0 ? `<br>使用后切换到阶段 ${escape(skill.transform_stage)}` : ''}</p>
        <pre>${escape(skill.detail)}</pre><p>${escape(skill.path)} · GUID ${skill.guid}</p>
      </section>`).join('')}
      <p>${escape(enemy.path)} · GUID ${enemy.guid}</p>
    </article>`).join('');
}

$('enemy-total').textContent = enemies.length;
$('skill-total').textContent = data.summary.related_unique_skills;
const contentRevision = data.summary.latest_content_revision;
$('snapshot-date').textContent = contentRevision ? `${contentRevision.date} 增量更新` : `${data.date} 数据快照`;
$('coverage').textContent = data.summary.coverage || `收录 ${data.date} 快照中的 ${enemies.length} 个敌人资产与 ${data.summary.related_unique_skills} 个关联技能。${data.date === '2026-10-26' ? '血孢子沿用此快照中的数值。' : ''}`;
const statusRevision = data.summary.latest_status_mechanics_revision;
if (statusRevision) $('coverage').textContent += ` ${statusRevision.date} 增量复核：${statusRevision.enemyNumbers.map(number => String(number).padStart(3, '0')).join('、')} 的眩晕机制。${statusRevision.scope}`;
for (const attribute of attributes) $('attribute').add(new Option(attribute, attribute));
$('categories').innerHTML = categories.map(category => {
  const count = category.key === 'all' ? enemies.length : enemies.filter(enemy => enemy.categoryKeys.includes(category.key)).length;
  return `<button type="button" class="cat" data-category="${escape(category.key)}"><span>${escape(category.label)}</span><span class="cat-count">${count}</span></button>`;
}).join('');

$('categories').addEventListener('click', event => {
  const button = event.target.closest('[data-category]');
  if (!button) return;
  state.category = button.dataset.category;
  render();
});
$('query').addEventListener('input', event => { state.query = event.target.value; render(); });
$('attribute').addEventListener('change', event => { state.attribute = event.target.value; render(); });
$('sort').addEventListener('change', event => { state.sort = event.target.value; render(); });
$('direction').addEventListener('click', () => { state.order = state.order === 'asc' ? 'desc' : 'asc'; render(); });
$('favorites-only').addEventListener('click', () => { state.favoritesOnly = !state.favoritesOnly; render(); });
for (const view of ['grid', 'list']) {
  $('view-' + view).addEventListener('click', () => { state.view = view; render(); });
}
$('reset').addEventListener('click', reset);
document.querySelector('.skip-link').addEventListener('click', event => {
  event.preventDefault();
  $('main').focus();
});
$('results').addEventListener('click', event => {
  const favorite = event.target.closest('[data-favorite]');
  if (favorite) {
    const guid = favorite.dataset.favorite;
    if (favorites.has(guid)) favorites.delete(guid);
    else favorites.add(guid);
    try { localStorage.setItem(storageKey, JSON.stringify([...favorites])); } catch { /* Keep the in-memory collection. */ }
    render();
    (document.querySelector(`[data-favorite="${guid}"]`) || $('favorites-only')).focus({preventScroll: true});
    return;
  }
  const card = event.target.closest('[data-open]');
  if (card) openDetail(card.dataset.open, true);
  if (event.target.closest('[data-reset]')) reset();
});
$('prev').addEventListener('click', () => adjacent(-1));
$('next').addEventListener('click', () => adjacent(1));
$('close').addEventListener('click', () => $('detail').close());
$('detail').addEventListener('click', event => {
  const section = event.target.closest('[data-section]');
  if (section) $(section.dataset.section).scrollIntoView({block: 'start'});
  if (event.target === $('detail')) $('detail').close();
});
$('detail').addEventListener('close', () => {
  if ($('detail').open) return;
  const guid = current?.guid;
  current = null;
  writeHash();
  if (guid) document.querySelector(`[data-open="${guid}"]`)?.focus({preventScroll: true});
});
document.addEventListener('keydown', event => {
  if (event.target.closest('input, select, textarea, [contenteditable]')) return;
  if ($('detail').open) {
    if (event.key === 'ArrowLeft') adjacent(-1);
    if (event.key === 'ArrowRight') adjacent(1);
  } else if (event.key === '/') {
    event.preventDefault();
    $('query').focus();
  }
});
$('print').addEventListener('click', () => { renderPrint(); window.print(); });
window.addEventListener('beforeprint', renderPrint);
window.addEventListener('afterprint', () => { $('print-view').innerHTML = ''; });

function applyHash() {
  const {state: next, enemy} = readHash();
  state = next;
  current = byGuid.get(enemy) || null;
  if (!current && $('detail').open) $('detail').close();
  render();
  if (current) openDetail(current.guid);
}

window.addEventListener('hashchange', applyHash);
applyHash();
