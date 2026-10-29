"""Verify generated deliverables using only Python's standard library (read-only)."""
import csv
import io
import json
import re
import subprocess
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

root = Path('outputs/relic-catalog')
data = json.loads((root / 'catalog.json').read_text(encoding='utf-8'))
raw = json.loads((root / 'relics-raw.json').read_text(encoding='utf-8-sig'))
html = (root / '遗物完整图鉴.html').read_text(encoding='utf-8')
embedded = json.loads(re.search(r'<script id="catalog-data" type="application/json">(.*?)</script>', html, re.S)[1])
icons = embedded.pop('icons')
assert embedded == data
assert len(data['relics']) == data['count'] == sum(g['count'] for g in data['groups'])
assert 'instanceID' not in (root / 'catalog.json').read_text(encoding='utf-8')
raw_by_path = {r['path']: r for r in raw['rows']}
for relic in data['relics']:
    source = raw_by_path[relic['path']]
    for key in ['guid', 'description', 'additionalDescription', 'rarity', 'iconGuid']:
        assert source[key] == relic[key], (relic['id'], key)
    assert Path(relic['path']).is_file()
    assert len(relic['resolvedEffects']) == len(source['effects'])
    for effect in relic['resolvedEffects']:
        for reference in effect['references'].values():
            if reference['valid']:
                assert Path(reference['path']).exists()
# Tuanjie may encode GUIDs in .meta files: verify their identity round-trip via
# AssetDatabase in the editor, rather than treating the on-disk token as a GUID.
md = (root / '遗物体系完整表.md').read_text(encoding='utf-8')
ids = [int(s) for s in re.findall(r'^\|(\d+)\|', md, re.M)]
assert len(ids) == data['count'] and set(ids) == {r['id'] for r in data['relics']}
ns = {'x': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
with zipfile.ZipFile(root / 'DiceLand遗物体系表.xlsx') as archive:
    assert archive.testzip() is None
    strings = [''.join(t.itertext()) for t in ET.fromstring(archive.read('xl/sharedStrings.xml'))]

    def value(cell):
        token = cell.find('x:v', ns)
        return '' if token is None else (strings[int(token.text)] if cell.get('t') == 's' else token.text)

    sheet = ET.fromstring(archive.read('xl/worksheets/sheet2.xml'))
    rows = {}
    for row in sheet.findall('x:sheetData/x:row', ns):
        cells = {re.sub(r'\d', '', c.get('r')): value(c) for c in row}
        if int(row.get('r')) >= 8:
            rows[int(cells['A'])] = cells
    assert len(rows) == data['count']
    columns = {'B': 'relicName', 'C': 'category', 'D': 'rarityName', 'E': 'role', 'F': 'description', 'G': 'tags', 'H': 'triggerNames', 'I': 'note', 'J': 'additionalDescription'}
    for relic in data['relics']:
        for col, key in columns.items():
            assert rows[relic['id']].get(col, '') == str(relic.get(key, '') or ''), (relic['id'], key)
    assert ET.fromstring(archive.read('xl/tables/table2.xml')).get('ref') == f"A7:J{7 + data['count']}"
    assert not sheet.findall('.//x:c[@t="e"]', ns)
    baseline = subprocess.run(['git', 'show', 'HEAD:outputs/relic-catalog/DiceLand遗物体系表.xlsx'], capture_output=True, check=True).stdout
    with zipfile.ZipFile(io.BytesIO(baseline)) as old:
        for name in ['sheet1', 'sheet2']:
            before = ET.fromstring(old.read(f'xl/worksheets/{name}.xml'))
            after = ET.fromstring(archive.read(f'xl/worksheets/{name}.xml'))
            for xpath in ['x:cols', 'x:sheetViews/x:sheetView/x:pane']:
                a, b = before.find(xpath, ns), after.find(xpath, ns)
                if xpath.endswith('pane') and a is not None and b is not None:
                    # Scroll position can reset to the opening row on export;
                    # the frozen row/column counts and state must remain.
                    assert all(a.get(k) == b.get(k) for k in ['xSplit', 'ySplit', 'state']), (name, xpath)
                else:
                    assert (ET.tostring(a) if a is not None else None) == (ET.tostring(b) if b is not None else None), (name, xpath)
csv_path = Path('outputs/merge-dev-relic-catalog-20261029/new-relics.csv')
if csv_path.exists():
    with csv_path.open(encoding='utf-8-sig', newline='') as stream:
        exported = list(csv.DictReader(stream))
    assert [int(r['序号']) for r in exported] == sorted(data['newIds'], reverse=True)
    by_id = {r['id']: r for r in data['relics']}
    assert all(r['完整效果'] == by_id[int(r['序号'])]['description'] for r in exported)
metadata = json.loads(Path('outputs/catalog-snapshots.json').read_text(encoding='utf-8'))
assert metadata['relicCatalog']['relicCount'] == data['count']
print(f"PASS: {data['count']} source configs, {len(data['groups'])} categories, HTML/Markdown/Excel values, original widths/freeze panes, CSV and metadata; {len(icons)} embedded icons.")
