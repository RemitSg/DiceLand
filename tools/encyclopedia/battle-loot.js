function resolveBattleLoot(battle) {
  const byTable = new Map(battle.tables.map(table => [table.id, table]));
  return battle.sources.map(source => ({
    ...source,
    tables: [
      ...source.tableIds.map(id => byTable.get(id)),
      ...source.eventPicks.map(pick => ({name: '事件素材奖励', pick, drops: pick.drops}))
    ]
  }));
}

function lootSourceName(source) {
  if (source.kind === 'retreat') return `${source.name} · 第 ${source.round} 回合撤退`;
  if (source.kind === 'event') return `${source.name} · ${source.label} · 胜利奖励`;
  return `${source.name} · 敌群`;
}

function lootAmount({min, max}) {
  return min === max ? String(min) : `${min}–${max}`;
}

function lootTableName(table) {
  return table.pick ? `${table.name} · 抽 ${lootAmount(table.pick)} / ${table.drops.length} 项` : table.name;
}

function lootDropText(drop, table) {
  const amount = lootAmount(drop) + ' 件';
  if (table.pick) return amount;
  const chance = new Intl.NumberFormat('zh-CN', {style: 'percent', maximumFractionDigits: 2}).format(drop.chance);
  return drop.chance > 0 ? `${chance} · ${amount}` : `${chance} · 保底时可抽 · ${amount}`;
}
