import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';

const directory=path.dirname(fileURLToPath(import.meta.url));
const root=path.resolve(directory,'../../..');
const sharp=createRequire(path.join(root,'tools/relic-catalog/build-catalog.mjs'))('sharp');
const size=384,columns=6,rows=4;
const frames=await Promise.all(Array.from({length:24},async(_,index)=>({
  input:await sharp(path.join(root,'outputs/agent-battle/blender-root-rend-20261109/frames',`rootrend_${String(index+1).padStart(4,'0')}.png`))
    .resize(size,size,{kernel:'lanczos3'}).png().toBuffer(),
  left:index%columns*size,top:Math.floor(index/columns)*size
})));
const output=path.join(root,'Assets/Data/Enemies/Enemies/044 WitheredCourt/VFX/Blender/044_RootRend_Blender_6x4.png');
await sharp({create:{width:size*columns,height:size*rows,channels:4,background:{r:0,g:0,b:0,alpha:0}}})
  .composite(frames).png().toFile(output);
console.log(JSON.stringify({output,frames:24,fps:48,columns,rows,cell:size,width:size*columns,height:size*rows}));
