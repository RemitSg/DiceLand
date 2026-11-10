import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';
const dir='outputs/relic-catalog',file=`${dir}/DiceLand遗物体系表.xlsx`;
const data=JSON.parse(await fs.readFile(`${dir}/catalog.json`,'utf8'));
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load(file));
const summary=wb.worksheets.getItem('体系总览'),detail=wb.worksheets.getItem('遗物完整表');
if(process.argv.includes('--notes-only')){
 const ids=process.argv.slice(2).filter(value=>/^\d+$/.test(value)).map(Number);
 if(!ids.length)throw Error('Pass at least one relic ID with --notes-only');
 const range=`A8:J${7+data.count}`;
 const before=detail.getRange(range).values;
 const summaryBefore=JSON.stringify(summary.getUsedRange().values);
 const formulasBefore=JSON.stringify(detail.getRange(range).formulas);
 const changedRows=new Set();
 for(const id of ids){
  const relic=data.relics.find(r=>r.id===id),index=before.findIndex(row=>row[0]===id);
  if(!relic||index<0)throw Error(`Missing relic ${id}`);
  const row=index+8;
  detail.getRange(`I${row}`).values=[[relic.note||'']];
  const band=detail.getRange(`A${row}:J${row}`),height=band.format.rowHeight;
  band.format.rowHeight=Math.max(typeof height==='number'?height:44,Math.ceil((relic.note||'').length/45)*15+12);
  changedRows.add(index);
 }
 wb.recalculate();
 const after=detail.getRange(range).values;
 for(let row=0;row<before.length;row++)for(let col=0;col<10;col++){
  if(changedRows.has(row)&&col===8)continue;
  if(before[row][col]!==after[row][col])throw Error(`Unrelated cell changed at ${row+8},${col+1}`);
 }
 if(summaryBefore!==JSON.stringify(summary.getUsedRange().values)||formulasBefore!==JSON.stringify(detail.getRange(range).formulas))throw Error('Unrelated summary or formulas changed');
 for(const id of ids){
  const row=8+after.findIndex(value=>value[0]===id);
  const note=data.relics.find(r=>r.id===id).note||'';
  if(detail.getRange(`I${row}`).values[0][0]!==note)throw Error(`Note mismatch for relic ${id}`);
  const blob=await wb.render({sheetName:'遗物完整表',range:`F${row}:J${row}`,scale:1,format:'png'});
  await fs.writeFile(`${dir}/relic-${id}-preview.png`,new Uint8Array(await blob.arrayBuffer()));
 }
 console.log((await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:20},summary:'final formula error scan'})).ndjson);
 const output=await SpreadsheetFile.exportXlsx(wb);await output.save(file);
 console.log(JSON.stringify({sheets:['体系总览','遗物完整表'],rows:after.length,noteIds:ids,otherValuesAndFormulasUnchanged:true}));
}else{
summary.getRange('A4').values=[[`${data.count} 件遗物，${data.groups.length} 个主体系。每件只计一次，跨体系效果见联动标签。`]];
summary.getRange('A5').values=[['2026-10-29。161 件当前配置；新入库 19 件及 8 件历史遗物已核对主要机制，含暗礁、碎潮盐瓶与浪鸣调整。其余历史备注沿用；编辑器验证，未完整试玩。']];
summary.getRange('A8:E24').values=data.groups.map(g=>[g.name,g.count,g.summary,g.examples,g.play]);
summary.getRange('B25').values=[[data.count]];
detail.getRange('A5').values=[['2026-10-29：161 件当前配置；143–161 及 12、19、30、59、79、112、134、138 已核对主要机制并同步调整。阴阳鱼实际 ID 为 yin_yang_fish；验证为针对性 EditMode 回归。']];
const order=new Map(data.groups.map((g,i)=>[g.name,i]));
const sorted=[...data.relics].sort((a,b)=>order.get(a.category)-order.get(b.category)||a.id-b.id);
const table=detail.tables.items[0];
const previous=detail.getUsedRange().values.slice(7).filter(row=>row[0]!==null&&row[0]!=='').length;
if(previous<sorted.length)table.rows.add(null,Array.from({length:sorted.length-previous},()=>Array(10).fill(null)));
for(let row=8+previous;row<=7+sorted.length;row++)detail.getRange(`A${row}:J${row}`).copyFrom(detail.getRange(`A${7+previous}:J${7+previous}`),'all');
detail.getRange(`A8:J${7+sorted.length}`).values=sorted.map(r=>[r.id,r.relicName,r.category,r.rarityName,r.role,r.description,r.tags,r.triggerNames,r.note||'',r.additionalDescription||'']);
for(let i=0;i<sorted.length;i++){
 const r=sorted[i],row=i+8;
 // Existing widths and table style remain; longer new mechanism notes need room.
 const lines=Math.max(Math.ceil(r.description.length/55),Math.ceil((r.note||'').length/45),Math.ceil((r.additionalDescription||'').length/37));
 detail.getRange(`A${row}:J${row}`).format.rowHeight=Math.max(44,lines*15+12);
}
wb.recalculate();
if(summary.getRange('B25').values[0][0]!==data.count)throw Error('Summary count mismatch');
const values=detail.getRange(`A8:J${7+data.count}`).values;
if(values.length!==data.count||new Set(values.map(r=>r[0])).size!==data.count)throw Error('Workbook identity mismatch');
for(let i=0;i<sorted.length;i++)if(values[i][1]!==sorted[i].relicName||values[i][8]!==String(sorted[i].note||''))throw Error(`Workbook row mismatch ${i}`);
const previewIds=process.argv.slice(2).map(Number);
const detailPreviews=(previewIds.length?previewIds:[146]).map(id=>{
 const row=8+sorted.findIndex(r=>r.id===id);
 return ['遗物完整表',`F${row}:J${row}`,`relic-${id}`];
});
for(const [sheetName,range,name]of[['体系总览','A2:E15','summary'],...detailPreviews]){
 const blob=await wb.render({sheetName,range,scale:1,format:'png'});
 await fs.writeFile(`${dir}/${name}-preview.png`,new Uint8Array(await blob.arrayBuffer()));
}
const output=await SpreadsheetFile.exportXlsx(wb);await output.save(file);
console.log(JSON.stringify({sheets:['体系总览','遗物完整表'],rows:values.length,newCount:data.newIds.length,lastRow:7+data.count}));
}
