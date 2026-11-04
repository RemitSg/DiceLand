"""Encode unmodified Unity MCP render frames. Requires Pillow; no game artwork is synthesized here."""
import json
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[2] / 'outputs/agent-battle/death-vfx-20261104'
for name in ('ally', 'enemy'):
    folder = root / name
    report = json.loads((folder / 'capture.json').read_text(encoding='utf-8-sig'))
    paths = sorted(folder.glob('[0-9][0-9][0-9].png'))
    assert len(paths) == report['frames']
    images = [Image.open(path).convert('RGB') for path in paths]
    palette_sample = Image.new('RGB', (320 * 4, 180 * 2))
    for index, source in enumerate(images[::max(1, len(images)//8)][:8]):
        palette_sample.paste(source.resize((320, 180)), ((index % 4)*320, (index // 4)*180))
    palette = palette_sample.quantize(colors=240)
    frames = [source.quantize(palette=palette, dither=Image.Dither.NONE) for source in images]
    durations = [round((i+1)*100/report['fps'])*10 - round(i*100/report['fps'])*10 for i in range(len(frames))]
    durations[0] += 350
    durations[-1] += 550
    output = root / f'{name}-death-dissolution.gif'
    frames[0].save(output, save_all=True, append_images=frames[1:], duration=durations, loop=0, optimize=True, disposal=1)
    sheet = Image.new('RGB', (480 * 3, 270 * 2))
    for index, frame_index in enumerate((0, 12, 17, 23, 34, 49)):
        sheet.paste(images[min(frame_index, len(images)-1)].resize((480, 270)), ((index % 3)*480, (index // 3)*270))
    sheet.save(root / f'{name}-contact.png')
    print(json.dumps({'file': str(output), 'frames': len(frames), 'bytes': output.stat().st_size, 'durationMs': sum(durations)}))
