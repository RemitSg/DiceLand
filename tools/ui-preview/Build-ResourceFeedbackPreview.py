"""Encode the disposable Unity capture as a small, silent review GIF (requires Pillow)."""
import argparse
import json
from pathlib import Path
from PIL import Image


def build(frames_dir: Path, output: Path):
    manifest = json.loads((frames_dir / "preview-complete.json").read_text(encoding="utf-8"))
    if not manifest.get("success") or manifest.get("running"):
        raise ValueError("Unity capture has not completed successfully")
    sources = sorted(frames_dir.glob("feedback-[0-9][0-9][0-9].png"))
    if len(sources) != 150:
        raise ValueError(f"Expected all 150 captured frames; found {len(sources)}")
    # A shared palette keeps the unmoving forest background stable between frames.
    sheet = Image.new("RGB", (390 * 6, 180))
    for column, index in enumerate([0, 14, 25, 53, 83, 113]):
        with Image.open(sources[index]) as source:
            sheet.paste(source.convert("RGB").resize((390, 180), Image.Resampling.LANCZOS), (390 * column, 0))
    palette = sheet.quantize(colors=256, method=Image.Quantize.MEDIANCUT)
    frames = []
    for source_path in sources[::2]:
        with Image.open(source_path) as source:
            frames.append(source.convert("RGB").resize((1170, 540), Image.Resampling.LANCZOS)
                          .quantize(palette=palette, dither=Image.Dither.NONE))
    durations = [80, 90, 80] * 25  # Exactly 12fps on GIF's 10ms time grid.
    durations[-1] += 1000
    output.parent.mkdir(parents=True, exist_ok=True)
    frames[0].save(output, save_all=True, append_images=frames[1:], duration=durations,
                   loop=0, optimize=True, disposal=1)
    with Image.open(output) as result:
        print(f"{output}: {result.n_frames} frames, {result.size}, {output.stat().st_size} bytes; silent preview")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("frames_dir", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    build(args.frames_dir, args.output)
