'use strict';
(() => {
  const $ = id => document.getElementById(id);
  const names = ['普通', '罕见', '稀有', '史诗', '传说', '杂物'];
  const drafts = new Map(), details = new Set();
  let data = null, remote = null, category = 'loot', descending = false;
  let saving = false, polling = false, timer = null, connected = false, networkError = false;
  const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const rowKey = row => `${row.category}:${row.index}`;
  const number = value => value !== '' && /^\d+$/.test(String(value)) && Number.isSafeInteger(Number(value)) ? Number(value) : NaN;
  const time = () => new Date().toLocaleTimeString('zh-CN', {hour12:false});

  function status(label, detail, error = false) {
    $('save-label').textContent = label; $('save-detail').textContent = detail;
    document.querySelector('.sync-stat').classList.toggle('error', error);
    $('save-now').disabled = !connected || saving || !drafts.size || !!remote;
    $('open-add').disabled = !connected || saving || !!drafts.size || !!remote;
  }
  function connection(ok, message) {
    connected = ok; $('connection').className = `connection ${ok ? 'online' : 'error'}`;
    $('connection').querySelector('span').textContent = message;
    $('offline').hidden = ok;
  }
  async function request(method = 'GET', body) {
    const controller = new AbortController(), timeout = setTimeout(() => controller.abort(), 8000);
    try {
      const response = await fetch('/api/catalog', {method, cache:'no-store', signal:controller.signal,
        headers:body ? {'Content-Type':'application/json', 'X-DiceLand-Editor':'merchant'} : {},
        body:body ? JSON.stringify(body) : undefined});
      const result = await response.json();
      if (!response.ok) { const error = new Error(result.message || `请求失败 (${response.status})`); error.status = response.status; error.current = result.current; throw error; }
      return result;
    } finally { clearTimeout(timeout); }
  }
  function overlay(key, original) { return {...original, ...(drafts.get(key)?.fields || {})}; }
  function edit(key, field, value) {
    let draft = drafts.get(key);
    if (!draft) {
      if (key === 'settings') draft = {operation:'settings', fields:{}};
      else if (key.startsWith('category:')) draft = {operation:'category', category:key.slice(9), fields:{}};
      else {
        const row = data.rows.find(r => rowKey(r) === key);
        draft = {operation:'entry', category:row.category, index:row.index, assetGuid:row.asset.guid, fields:{}};
      }
    }
    const base = draft.operation === 'settings' ? data : draft.operation === 'category' ? data.categories.find(c => c.key === draft.category) : data.rows.find(r => rowKey(r) === key);
    if (String(base?.[field] ?? '') === String(value)) delete draft.fields[field];
    else draft.fields[field] = value;
    if (Object.keys(draft.fields).length) drafts.set(key, draft); else drafts.delete(key);
    validate(); clearTimeout(timer); if (!remote) timer = setTimeout(save, 600);
  }
  function changes() {
    const result = [];
    for (const [key, draft] of drafts) {
      if (draft.operation === 'settings') result.push({operation:'settings', ...overlay(key, {minTotalGoodsCount:data.minTotalGoodsCount, maxTotalGoodsCount:data.maxTotalGoodsCount})});
      else if (draft.operation === 'category') {
        const base = data.categories.find(c => c.key === draft.category);
        result.push({operation:'category', category:draft.category, ...overlay(key, {minimumCount:base.minimumCount, randomWeight:base.randomWeight})});
      } else {
        const base = data.rows.find(r => rowKey(r) === key);
        if (!base || base.asset.guid !== draft.assetGuid) throw new Error('商品行发生变化，请先使用最新配置。');
        result.push({operation:'entry', category:base.category, index:base.index, assetGuid:base.asset.guid,
          ...overlay(key, {goodsId:base.goodsId, minPrice:base.minPrice, maxPrice:base.maxPrice, weight:base.weight})});
      }
    }
    return result;
  }
  function validate() {
    let message = '';
    for (const input of document.querySelectorAll('[data-key][data-field]')) input.removeAttribute('aria-invalid');
    const invalid = (key, fields, text) => {
      message ||= text;
      for (const input of document.querySelectorAll('[data-key][data-field]'))
        if (input.dataset.key === key && fields.includes(input.dataset.field)) input.setAttribute('aria-invalid', 'true');
    };
    const integer = (value, min, max = 1000000) => Number.isSafeInteger(number(value)) && number(value) >= min && number(value) <= max;
    if (!data) return false;
    for (const [key, draft] of drafts) {
      if (draft.operation === 'settings') {
        const values = overlay(key, data);
        if (!integer(values.minTotalGoodsCount, 0, 10000) || !integer(values.maxTotalGoodsCount, 0, 10000) || number(values.minTotalGoodsCount) > number(values.maxTotalGoodsCount))
          invalid(key, ['minTotalGoodsCount','maxTotalGoodsCount'], '商品总数需要有效整数，且下限不能高于上限。');
      } else if (draft.operation === 'category') {
        const values = overlay(key, data.categories.find(c => c.key === draft.category));
        if (!integer(values.minimumCount, 0, 10000)) invalid(key, ['minimumCount'], '保底数量需要 0–10000 的整数。');
        if (!integer(values.randomWeight, 0)) invalid(key, ['randomWeight'], '类型权重需要 0–1000000 的整数。');
      } else {
        const base = data.rows.find(r => rowKey(r) === key);
        if (!base) { message ||= '商品行已失效。'; continue; }
        const values = overlay(key, base);
        if (!integer(values.minPrice, 1) || !integer(values.maxPrice, 1) || number(values.minPrice) > number(values.maxPrice))
          invalid(key, ['minPrice','maxPrice'], '价格需要正整数，且下限不能高于上限。');
        if (!integer(values.weight, 1)) invalid(key, ['weight'], '商品权重需要 1–1000000 的整数。');
        if (!String(values.goodsId).trim() || String(values.goodsId).length > 120) invalid(key, ['goodsId'], '商品标识需要 1–120 个字符。');
      }
    }
    if (remote) status('修改待确认', '外部配置已更新，输入已保留。', true);
    else if (message) status('待修正', message, true);
    else if (drafts.size) status(saving ? '正在保存' : '等待保存', '有效修改将在停止输入后自动写回。');
    else if (!saving && connected) status('已同步', `上次检查 ${time()}`);
    return !message;
  }
  function serializeChanges(list) {
    return list.map(change => Object.fromEntries(Object.entries(change).map(([key,value]) =>
      [key, ['minPrice','maxPrice','weight','minTotalGoodsCount','maxTotalGoodsCount','minimumCount','randomWeight','index'].includes(key) ? number(value) : value])));
  }
  function showConflict(latest) { remote = latest; $('conflict').hidden = false; status('修改待确认', '配置已更新，尚未保存的输入仍然保留。', true); }
  async function save(extra) {
    clearTimeout(timer);
    if (!data || saving || remote || !connected || (!drafts.size && !extra) || !validate()) return;
    const sent = new Map([...drafts].map(([key,draft]) => [key, {...draft, fields:{...draft.fields}}]));
    let payload;
    try { payload = {baseRevision:data.revision, changes:serializeChanges(extra || changes())}; }
    catch (e) { status('无法保存', e.message, true); return; }
    saving = true; status('正在保存', '正在写入 Unity 商店资产…');
    let succeeded = false;
    try {
      const next = await request('POST', payload);
      for (const [key,draft] of sent) {
        const remaining = drafts.get(key); if (!remaining) continue;
        for (const [field,value] of Object.entries(draft.fields)) if (remaining.fields[field] === value) delete remaining.fields[field];
        if (!Object.keys(remaining.fields).length) drafts.delete(key);
      }
      data = next; networkError = false; succeeded = true; connection(true, 'Unity 已连接 · 双向同步');
      render(); status(drafts.size ? '等待保存' : '已保存', `${time()} · 已写入商店配置`);
    } catch (e) {
      if (e.status === 409 && e.current) showConflict(e.current);
      else if (!e.status || e.status >= 500) { networkError = true; connection(false, '连接中断 · 等待重连'); status('尚未保存', '输入已保留；连接恢复后继续。', true); $('offline-detail').textContent = e.message; }
      else status('未保存', e.message, true);
      if (extra) $('add-error').textContent = e.message;
    } finally {
      saving = false;
      $('save-now').disabled = !connected || !drafts.size || !!remote;
      $('open-add').disabled = !connected || !!drafts.size || !!remote;
      if (succeeded && drafts.size && !remote) timer = setTimeout(save, 600);
    }
    return succeeded;
  }
  function preserveFocus(action) {
    const active = document.activeElement;
    const key = active?.dataset.key, field = active?.dataset.field;
    const selection = active?.type === 'text' ? [active.selectionStart,active.selectionEnd] : null;
    action();
    if (key && field) {
      const target = [...document.querySelectorAll('[data-key][data-field]')].find(e => e.dataset.key === key && e.dataset.field === field);
      target?.focus({preventScroll:true}); if (target && selection) target.setSelectionRange(...selection);
    }
  }
  function render() {
    if (!data) return;
    preserveFocus(() => {
      const loot = data.categories.find(c => c.key === 'loot');
      $('coverage').textContent = `${loot.coveredCount} / ${loot.availableCount}`;
      $('coverage-note').textContent = loot.coveredCount === loot.availableCount ? '已覆盖全部' : `尚缺 ${loot.availableCount - loot.coveredCount} 种`;
      $('entry-count').textContent = data.rows.length; $('stock-count').textContent = `${data.minTotalGoodsCount}–${data.maxTotalGoodsCount}`;
      $('source-path').textContent = data.assetPath; $('unity-dirty').hidden = !data.unsavedUnityChanges;
      const settings = overlay('settings', data);
      $('total-min').value = settings.minTotalGoodsCount; $('total-max').value = settings.maxTotalGoodsCount;
      $('category-settings').innerHTML = data.categories.map(c => {
        const v = overlay(`category:${c.key}`, c);
        return `<div class="category-setting"><span>${escape(c.title)}</span><input data-key="category:${c.key}" data-field="minimumCount" type="number" min="0" max="10000" step="1" value="${escape(v.minimumCount)}" aria-label="${escape(c.title)}保底数量"><input data-key="category:${c.key}" data-field="randomWeight" type="number" min="0" max="1000000" step="1" value="${escape(v.randomWeight)}" aria-label="${escape(c.title)}补位权重"></div>`;
      }).join('');
      $('tabs').innerHTML = data.categories.map(c => `<button class="tab ${category === c.key ? 'active' : ''}" data-category="${c.key}" aria-pressed="${category === c.key}">${escape(c.title)}<span>${c.entryCount}</span></button>`).join('');
      renderRows();
    });
    validate();
  }
  function renderRows() {
    const query = $('query').value.trim().toLocaleLowerCase(), rarity = $('rarity').value, sort = $('sort').value;
    const all = data.rows.filter(r => r.category === category);
    const sum = all.reduce((total,r) => total + Math.max(1,Number(overlay(rowKey(r),r).weight) || 1),0);
    let rows = all.filter(r => (!query || [r.asset.name,r.asset.id,r.asset.path,r.asset.guid,r.asset.description,r.goodsId].join(' ').toLocaleLowerCase().includes(query)) && (rarity === '' || String(r.asset.rarity) === rarity));
    rows.sort((a,b) => {
      const av = sort === 'id' || sort === 'name' || sort === 'rarity' ? a.asset[sort] : overlay(rowKey(a),a)[sort];
      const bv = sort === 'id' || sort === 'name' || sort === 'rarity' ? b.asset[sort] : overlay(rowKey(b),b)[sort];
      const compared = sort === 'name' ? String(av).localeCompare(String(bv),'zh-CN') : Number(av) - Number(bv);
      return (compared || a.index - b.index) * (descending ? -1 : 1);
    });
    $('pool-summary').textContent = `${all.length} 项 · 权重合计 ${sum}`;
    $('result-count').textContent = `显示 ${rows.length} / ${all.length} 项`;
    $('rows').innerHTML = rows.map(r => {
      const key = rowKey(r), v = overlay(key,r), asset = r.asset;
      const input = field => `<input data-key="${key}" data-field="${field}" type="number" min="1" max="1000000" step="1" value="${escape(v[field])}" aria-label="${escape(asset.name)}${{minPrice:'价格下限',maxPrice:'价格上限',weight:'权重'}[field]}">`;
      const share = sum ? ((Math.max(1,Number(v.weight) || 1) / sum) * 100).toFixed(1) : '0.0';
      return `<tr data-row="${key}"><td><div class="asset-cell"><span class="asset-id">${escape(asset.id ? String(asset.id).padStart(3,'0') : '—')}</span><div><strong class="asset-name ${asset.valid ? '' : 'invalid-name'}">${escape(asset.name)}</strong><span class="asset-subtitle">${escape(asset.description || (r.legacyRelic ? '旧版直接遗物引用' : asset.path.split('/').pop()))}</span></div></div></td><td><span class="rarity r${asset.rarity}">${names[asset.rarity] || '遗物表'}</span></td><td>${input('minPrice')}</td><td>${input('maxPrice')}</td><td class="weight-cell">${input('weight')}<span class="weight-note">池内 ${share}%</span></td><td><div class="row-actions"><button class="text-button" data-detail="${key}">${details.has(key) ? '收起' : '详情'}</button><button class="text-button remove" data-remove="${key}" ${saving || drafts.size || remote ? 'disabled' : ''}>移出</button></div></td></tr>` +
        (details.has(key) ? `<tr class="detail-row"><td colspan="6"><div class="detail-content"><p>${escape(asset.description || '暂无描述')}</p><code>资产 ID：${escape(asset.id)} · GUID：${escape(asset.guid)}</code><code>${escape(asset.path)}</code><label>商品标识<input data-key="${key}" data-field="goodsId" type="text" maxlength="120" value="${escape(v.goodsId)}" aria-label="${escape(asset.name)}商品标识"></label>${r.legacyRelic ? '<span>此条目保留旧版直接遗物引用；网页不会迁移它。</span>' : ''}${category === 'relic' ? '<span>此处编辑遗物表商品条目；实际遗物定价仍由难度和遗物价格规则计算。</span>' : ''}</div></td></tr>` : '');
    }).join('') || '<tr><td colspan="6" class="empty">没有匹配的商品。可调整筛选，或将新资产加入商品池。</td></tr>';
  }
  async function poll() {
    if (location.protocol === 'file:') { connection(false, '请连接本地编辑器'); status('等待编辑器', '从 Unity 菜单打开实时页面。'); return; }
    if (polling || saving) { setTimeout(poll,1000); return; }
    polling = true;
    try {
      const startedRevision = data?.revision;
      const next = await request();
      if (saving || (startedRevision && data?.revision !== startedRevision)) return;
      const reconnected = !connected;
      connection(true,'Unity 已连接 · 双向同步');
      if (!data) { data = next; render(); }
      else if (next.revision !== data.revision) {
        if (drafts.size) showConflict(next); else { remote = null; $('conflict').hidden = true; data = next; render(); }
      }
      if (!drafts.size && !remote) status('已同步', `${time()} · 外部更改自动更新`);
      if (reconnected && networkError && drafts.size && !remote) timer = setTimeout(save,200);
      networkError = false;
      $('open-add').disabled = saving || !!drafts.size || !!remote;
    } catch (e) {
      connection(false,'连接中断 · 等待重连'); networkError = true;
      status(drafts.size ? '尚未保存' : '等待连接', drafts.size ? '输入已保留，连接恢复后继续。' : '请保持 Unity 编辑器运行。',true);
      $('offline-detail').textContent = e.message;
    } finally { polling = false; setTimeout(poll,1000); }
  }
  function defaultValues(asset) {
    const same = data.rows.filter(r => r.category === category && r.asset.rarity === asset.rarity && r.asset.valid);
    if (same.length) {
      const mode = field => { const counts = new Map(); for (const r of same) counts.set(r[field],(counts.get(r[field]) || 0)+1); return [...counts].sort((a,b) => b[1]-a[1] || b[0]-a[0])[0][0]; };
      return {minPrice:mode('minPrice'),maxPrice:mode('maxPrice'),weight:mode('weight')};
    }
    const fallback = [[8,12,10],[18,26,4],[30,40,2],[45,60,1],[60,80,1],[1,3,1]][asset.rarity] || [1,1,1];
    return {minPrice:fallback[0],maxPrice:fallback[1],weight:fallback[2]};
  }
  function selectedAsset() { return data.available.find(c => c.category === category && c.asset.guid === $('add-asset').value)?.asset; }
  function updateAdd() {
    const asset = selectedAsset(); if (!asset) return;
    const defaults = defaultValues(asset);
    $('add-min').value = defaults.minPrice; $('add-max').value = defaults.maxPrice; $('add-weight').value = defaults.weight;
    $('add-description').textContent = `${names[asset.rarity] || '遗物表'} · ${asset.description || asset.path}`;
  }
  $('open-add').addEventListener('click', () => {
    const included = new Set(data.rows.filter(r => r.category === category).map(r => r.asset.guid));
    const available = data.available.filter(c => c.category === category && c.asset.valid && !included.has(c.asset.guid));
    $('add-asset').replaceChildren(...available.map(c => new Option(`${String(c.asset.id).padStart(3,'0')} · ${c.asset.name}`,c.asset.guid)));
    $('add-error').textContent = available.length ? '' : '当前类型的全部可用资产都已加入商品池。';
    $('confirm-add').disabled = !available.length; updateAdd(); $('add-dialog').showModal();
  });
  $('close-add').addEventListener('click', () => $('add-dialog').close());
  $('add-asset').addEventListener('change',updateAdd);
  $('add-form').addEventListener('submit',async e => {
    e.preventDefault(); const asset = selectedAsset(); if (!asset) return;
    const minPrice = number($('add-min').value), maxPrice = number($('add-max').value), weight = number($('add-weight').value);
    if (!(minPrice >= 1 && maxPrice >= minPrice && weight >= 1)) { $('add-error').textContent = '请填写有效价格范围和正整数权重。'; return; }
    $('confirm-add').disabled = true;
    const ok = await save([{operation:'add', category, assetGuid:asset.guid, minPrice,maxPrice,weight}]);
    $('confirm-add').disabled = false; if (ok) $('add-dialog').close();
  });
  document.addEventListener('input', e => { const {key,field} = e.target.dataset; if (key && field && data) edit(key,field,e.target.value); });
  document.addEventListener('keydown', e => { if (e.key === 'Enter' && e.target.matches('[data-key][data-field]')) { e.preventDefault(); e.target.blur(); save(); } });
  $('tabs').addEventListener('click', e => { const tab = e.target.closest('[data-category]'); if (tab) { category = tab.dataset.category; render(); } });
  for (const id of ['query','rarity','sort']) $(id).addEventListener(id === 'query' ? 'input' : 'change',() => { if (data) renderRows(); });
  $('direction').addEventListener('click',() => { descending = !descending; $('direction').textContent = descending ? '↓' : '↑'; if (data) renderRows(); });
  $('rows').addEventListener('click',async e => {
    const detail = e.target.closest('[data-detail]'), remove = e.target.closest('[data-remove]');
    if (detail) { const key = detail.dataset.detail; if (details.has(key)) details.delete(key); else details.add(key); renderRows(); }
    if (remove && !saving && !drafts.size && !remote) {
      const row = data.rows.find(r => rowKey(r) === remove.dataset.remove);
      await save([{operation:'remove',category:row.category,index:row.index,assetGuid:row.asset.guid}]);
    }
  });
  $('save-now').addEventListener('click',() => save());
  $('use-latest').addEventListener('click',() => { drafts.clear(); data = remote; remote = null; $('conflict').hidden = true; render(); });
  $('retry-changes').addEventListener('click',async () => {
    if (!remote) return;
    const rebased = new Map();
    for (const [key,draft] of drafts) {
      if (draft.operation !== 'entry') { rebased.set(key,draft); continue; }
      const candidates = remote.rows.filter(r => r.category === draft.category && r.asset.guid === draft.assetGuid);
      const exact = candidates.find(r => r.index === draft.index), row = exact || (candidates.length === 1 ? candidates[0] : null);
      if (!row) { status('无法重新应用', '对应商品已移出或出现重复引用，请使用最新配置后重新编辑。',true); return; }
      rebased.set(rowKey(row),{...draft,index:row.index});
    }
    drafts.clear(); for (const entry of rebased) drafts.set(...entry);
    data = remote; remote = null; $('conflict').hidden = true; render(); await save();
  });
  window.addEventListener('beforeunload',e => { if (drafts.size || saving) { e.preventDefault(); e.returnValue = ''; } });
  poll();
})();
