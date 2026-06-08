export const C = {
  bg: "#0f1420",
  panel: "#182133",
  panel2: "#202b3f",
  ink: "#f6f7fb",
  muted: "#aeb8c9",
  cyan: "#58d7ff",
  gold: "#f4c95d",
  green: "#74d680",
  red: "#ff7a72",
  violet: "#b69cff",
  line: "#34445f",
};

export const W = 1280;
export const H = 720;
export const FONT = "Microsoft YaHei";

export function rect(slide, x, y, w, h, fill = C.panel, line = C.line, radius = 0) {
  const sh = slide.shapes.add({ geometry: "rect" });
  sh.frame = { left: x, top: y, width: w, height: h };
  sh.fill = fill;
  if (line === "none") sh.line = { fill, width: 0 };
  else sh.line = { fill: line, width: 1.2 };
  if (radius) sh.borderRadius = radius;
  return sh;
}

export function text(slide, value, x, y, w, h, opt = {}) {
  const sh = rect(slide, x, y, w, h, opt.fill ?? C.bg, opt.line ?? "none", opt.radius ?? 0);
  sh.text.set(value);
  sh.text.typeface = opt.font ?? FONT;
  sh.text.fontSize = opt.size ?? 24;
  sh.text.color = opt.color ?? C.ink;
  sh.text.bold = opt.bold ?? false;
  sh.text.insets = opt.insets ?? { top: 4, right: 6, bottom: 4, left: 6 };
  sh.text.verticalAlignment = opt.valign ?? "top";
  sh.text.alignment = opt.align ?? "left";
  sh.text.wrap = "square";
  sh.text.autoFit = "shrinkText";
  return sh;
}

export function title(slide, claim, kicker = "") {
  text(slide, kicker, 54, 26, 500, 28, { size: 15, color: C.gold, bold: true });
  text(slide, claim, 50, 56, 1120, 62, { size: 32, bold: true });
  rect(slide, 50, 122, 1180, 1.8, C.line, "none");
}

export function footer(slide, n) {
  text(slide, "Mobile Roguelite Turn-Based Game · Unity / Tuanjie 2022.3", 50, 675, 760, 22, {
    size: 13,
    color: "#7f8aa1",
  });
  text(slide, String(n).padStart(2, "0"), 1170, 668, 60, 28, {
    size: 16,
    color: C.gold,
    align: "right",
    bold: true,
  });
}

export function metric(slide, value, label, x, y, w = 190, h = 118, color = C.cyan) {
  rect(slide, x, y, w, h, C.panel, C.line, 8);
  const compact = h < 100;
  text(slide, value, x + 16, y + (compact ? 14 : 18), w - 32, compact ? 30 : 40, {
    size: compact ? 25 : 30,
    bold: true,
    color,
  });
  text(slide, label, x + 16, y + h - (compact ? 34 : 42), w - 32, compact ? 24 : 30, {
    size: compact ? 13 : 15,
    color: C.muted,
  });
}

export function bulletList(slide, items, x, y, w, h, opt = {}) {
  const content = items.map((item) => `· ${item}`).join("\n");
  return text(slide, content, x, y, w, h, {
    size: opt.size ?? 20,
    color: opt.color ?? C.ink,
    fill: opt.fill ?? C.bg,
    line: opt.line ?? "none",
    insets: opt.insets ?? { top: 8, right: 10, bottom: 8, left: 10 },
  });
}

export function tag(slide, label, x, y, w, color = C.cyan) {
  rect(slide, x, y, w, 34, "#111827", color, 8);
  text(slide, label, x + 8, y + 6, w - 16, 22, { size: 14, color, bold: true, align: "center" });
}

export function node(slide, label, sub, x, y, w, h, color = C.cyan) {
  rect(slide, x, y, w, h, C.panel, color, 8);
  text(slide, label, x + 14, y + 12, w - 28, 26, { size: 18, color: C.ink, bold: true });
  text(slide, sub, x + 14, y + 42, w - 28, h - 50, { size: 14, color: C.muted });
}

export function hbar(slide, x1, y, x2, color = C.gold) {
  rect(slide, x1, y, x2 - x1, 3, color, "none");
  rect(slide, x2 - 10, y - 5, 12, 12, color, "none");
}

export async function image(slide, path, x, y, w, h) {
  const img = await slide.images.add({ path, frame: { left: x, top: y, width: w, height: h } });
  return img;
}

export function codePanel(slide, lines, x, y, w, h, label = "Code evidence") {
  rect(slide, x, y, w, h, "#121a28", C.line, 8);
  const compact = h < 86;
  text(slide, label, x + 16, y + 10, w - 32, 22, { size: 13, color: C.gold, bold: true, fill: "#121a28" });
  text(slide, lines.join("\n"), x + 18, y + (compact ? 34 : 42), w - 36, h - (compact ? 42 : 54), {
    size: compact ? 10 : 14,
    color: "#dce6f5",
    font: "Consolas",
    fill: "#121a28",
    insets: { top: 2, right: 2, bottom: 2, left: 2 },
  });
}

export function chapter(slide, n, heading, sub) {
  rect(slide, 0, 0, W, H, C.bg, "none");
  rect(slide, 0, 0, 14, H, C.gold, "none");
  text(slide, `0${n}`, 70, 170, 140, 70, { size: 50, color: C.gold, bold: true });
  text(slide, heading, 70, 250, 760, 76, { size: 38, bold: true });
  text(slide, sub, 74, 338, 840, 92, { size: 22, color: C.muted });
}

export function base(slide, n, claim, kicker) {
  rect(slide, 0, 0, W, H, C.bg, "none");
  title(slide, claim, kicker);
  footer(slide, n);
}
