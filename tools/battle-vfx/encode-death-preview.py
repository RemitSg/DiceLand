"""Encode unmodified Unity MCP render frames. Requires Pillow; no game artwork is synthesized here."""
import argparse
import json
from pathlib import Path
from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument('--source')
parser.add_argument('--contacts', action='store_true', help='Encode the clash and hit contact captures')
parser.add_argument('--preview-width', type=int, default=720)
args = parser.parse_args()
default_source = 'contact-fallout-20261105' if args.contacts else 'death-camera-dramatic-20261105'
root = Path(__file__).resolve().parents[2] / (args.source or f'outputs/agent-battle/{default_source}')
for name in (('clash', 'hit') if args.contacts else ('ally', 'enemy')):
    folder = root / name
    report = json.loads((folder / 'capture.json').read_text(encoding='utf-8-sig'))
    assert report.get('effectComplete', True), 'Capture must include the complete effect'
    assert report.get('cameraMotionComplete', True), 'Capture must include the completed camera return'
    assert report.get('clippedFrames', 0) == 0, 'Effect must stay inside the camera frame'
    paths = sorted(folder.glob('[0-9][0-9][0-9].png'))
    assert len(paths) == report['frames']
    images = [Image.open(path).convert('RGB') for path in paths]
    if 0 < args.preview_width < images[0].width:
        size = (args.preview_width, round(images[0].height * args.preview_width / images[0].width))
        images = [source.resize(size, Image.Resampling.LANCZOS) for source in images]
    palette_sample = Image.new('RGB', (320 * 4, 180 * 2))
    for index, source in enumerate(images[::max(1, len(images)//8)][:8]):
        palette_sample.paste(source.resize((320, 180)), ((index % 4)*320, (index // 4)*180))
    palette = palette_sample.quantize(colors=240)
    frames = [source.quantize(palette=palette, dither=Image.Dither.NONE) for source in images]
    durations = [round((i+1)*100/report['fps'])*10 - round(i*100/report['fps'])*10 for i in range(len(frames))]
    durations[0] += 350
    durations[-1] += 550
    output = root / (f'{name}.gif' if args.contacts else f'{name}-death-dissolution.gif')
    frames[0].save(output, save_all=True, append_images=frames[1:], duration=durations, loop=0, optimize=True, disposal=1)
    sheet = Image.new('RGB', (480 * 3, 270 * 2))
    samples = (0, 21, 29, 43, 60, 77) if args.contacts else (0, 17, 27, 42, 70, 110)
    for index, frame_index in enumerate(samples):
        sheet.paste(images[min(frame_index, len(images)-1)].resize((480, 270)), ((index % 3)*480, (index // 3)*270))
    sheet.save(root / f'{name}-contact.png')
    print(json.dumps({'file': str(output), 'frames': len(frames), 'bytes': output.stat().st_size, 'durationMs': sum(durations)}))
