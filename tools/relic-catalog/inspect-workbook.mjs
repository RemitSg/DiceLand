import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';
const dir = 'outputs/relic-catalog';
const wb = await SpreadsheetFile.importXlsx(await FileBlob.load(`${dir}/DiceLand遗物体系表.xlsx`));
console.log((await wb.inspect({kind:'sheet,table', maxChars:2200, tableMaxRows:2, tableMaxCols:4})).ndjson);
for (const [sheetName,range,name] of [['体系总览','A2:E12','before-summary'],['遗物完整表','A7:J11','before-detail']]) {
  const preview=await wb.render({sheetName,range,scale:1,format:'png'});
  await fs.writeFile(`${dir}/${name}-preview.png`,new Uint8Array(await preview.arrayBuffer()));
}
