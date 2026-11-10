import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';
const dir = 'outputs/relic-catalog';
const wb = await SpreadsheetFile.importXlsx(await FileBlob.load(`${dir}/DiceLand遗物体系表.xlsx`));
console.log((await wb.inspect({kind:'sheet,table', maxChars:2200, tableMaxRows:2, tableMaxCols:4})).ndjson);
const ids=process.argv.slice(2).map(Number);
const detail=wb.worksheets.getItem('遗物完整表');
const catalog=JSON.parse(await fs.readFile(`${dir}/catalog.json`,'utf8'));
const data=detail.getRange(`A8:J${7+catalog.count}`).values;
const ranges=ids.length?ids.map(id=>{
  const index=data.findIndex(row=>row[0]===id);
  if(index<0)throw Error(`Missing relic ${id}`);
  const row=index+8;
  console.log(JSON.stringify({id,row,values:data[index]}));
  return ['遗物完整表',`A${row}:J${row}`,`before-relic-${id}`];
}):[['体系总览','A2:E12','before-summary'],['遗物完整表','A7:J11','before-detail']];
for (const [sheetName,range,name] of ranges) {
  const preview=await wb.render({sheetName,range,scale:1,format:'png'});
  await fs.writeFile(`${dir}/${name}-preview.png`,new Uint8Array(await preview.arrayBuffer()));
}
