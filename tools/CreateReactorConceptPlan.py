"""Rebuild R01's dimensioned SVG from its concept JSON; does not modify Unity."""
from pathlib import Path
from html import escape
import json
import math
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "docs/Art/Reaktor"
SPEC = ART / "Reaktor-R01-2026-10-24.geometry.json"
OUT = ART / "Reaktor-R01-2026-10-24-massplan.svg"
s = json.loads(SPEC.read_text(encoding="utf-8"))
h, c, v, r = (s[k] for k in ("housing", "collar", "services", "radiators"))
assert h["outerDiameter"] == 8 and h["height"] == 6
assert c["outerDiameter"] == 16
assert v["anglesDegrees"] == [0, 90, 180, 270]
assert v["clearWidth"] == 3 and v["outerWidth"] > v["clearWidth"]
assert v["tipRadius"] - v["interfaceRadius"] == v["exposedStubLength"]
assert 2 * v["tipRadius"] == v["overallTipSpan"]
assert v["ringInterfaceRadius"] - v["tipRadius"] == v["remainingExtensionLength"]
assert math.isclose(v["ceilingY"] - v["floorY"], v["clearHeight"])
assert h["yMax"] - h["yMin"] == h["height"]
assert c["yMax"] - c["yMin"] == c["height"]
assert r["centreY"] - r["thickness"] / 2 > v["outerYMax"]
max_radius = math.hypot(r["centreRadius"] + r["radialLength"] / 2, r["tangentialWidth"] / 2)
assert max_radius < s["stationV4"]["innerModuleInnerRadius"]
# For the four 45-degree panels the minimum coordinate exceeds the half-width
# of every cardinal service corridor: a conservative plan-envelope check.
panel_min_axis = ((r["centreRadius"] - r["radialLength"] / 2) - r["tangentialWidth"] / 2) / math.sqrt(2)
assert panel_min_axis > v["outerWidth"] / 2

parts = ['<svg xmlns="http://www.w3.org/2000/svg" width="1680" height="1060" viewBox="0 0 1680 1060">',
'''<defs><marker id="a" markerWidth="8" markerHeight="8" refX="4" refY="4" orient="auto-start-reverse"><path d="M0 0L8 4L0 8Z" fill="#e1ebf2"/></marker></defs>
<style>text{font-family:Arial,sans-serif;fill:#e1ebf2;font-size:18px} .title{font-size:30px;font-weight:bold}.sub{fill:#9db6ca;font-size:17px}.small{font-size:15px}.dim{stroke:#e1ebf2;fill:none;stroke-width:1.2;marker-start:url(#a);marker-end:url(#a)}.ext{stroke:#90a6b8;stroke-width:1}.axis{stroke:#587084;stroke-dasharray:7 7;stroke-width:1}.frame{fill:#101e2b;stroke:#637c90}.outline{stroke:#c4d3df;stroke-width:2}</style>
<rect width="1680" height="1060" fill="#08131e"/>
<text x="35" y="45" class="title">SPACEMINER · R01 · MASSPLAN</text>
<text x="35" y="76" class="sub">Orthografische Modellhüllen · Meter · Maßverhältnisse aus geometry.json · keine technische Reaktorauslegung</text>
<rect x="25" y="100" width="770" height="800" rx="8" class="frame"/>
<rect x="815" y="100" width="840" height="800" rx="8" class="frame"/>
<text x="45" y="133" class="title">DRAUFSICHT · +Y → 0</text>
<text x="840" y="133" class="title">SEITE · Blick entlang −Z</text>''']

def line(x1, y1, x2, y2, cls="ext"):
    parts.append(f'<line x1="{x1:.2f}" y1="{y1:.2f}" x2="{x2:.2f}" y2="{y2:.2f}" class="{cls}"/>')

def text(x, y, value, cls="", anchor="middle"):
    parts.append(f'<text x="{x:.2f}" y="{y:.2f}" text-anchor="{anchor}" class="{cls}">{escape(value)}</text>')

def rect(x, y, w, height, fill, stroke="#c4d3df"):
    parts.append(f'<rect x="{x:.2f}" y="{y:.2f}" width="{w:.2f}" height="{height:.2f}" fill="{fill}" stroke="{stroke}" stroke-width="1.6"/>')

def circle(x, y, radius, fill, stroke="#c4d3df"):
    parts.append(f'<circle cx="{x:.2f}" cy="{y:.2f}" r="{radius:.2f}" fill="{fill}" stroke="{stroke}" stroke-width="1.6"/>')

cx, cy, scale = 395, 450, 25
P = lambda x, z: (cx + x * scale, cy - z * scale)
for x1, z1, x2, z2 in [(0, -12, 0, 12), (-12, 0, 12, 0)]:
    line(*P(x1, z1), *P(x2, z2), "axis")
circle(cx, cy, c["outerDiameter"] / 2 * scale, "#b2bdc6")
for k in range(c["segmentCount"]):
    a = math.tau * k / c["segmentCount"]
    line(*P(4 * math.sin(a), 4 * math.cos(a)), *P(8 * math.sin(a), 8 * math.cos(a)))
for angle in v["anglesDegrees"]:
    a = math.radians(angle)
    # A small overlap at the curved collar rim represents the adapter saddle.
    corners = []
    for radial, tangent in [(7.75, -1.8), (11, -1.8), (11, 1.8), (7.75, 1.8)]:
        corners.append(P(radial * math.sin(a) + tangent * math.cos(a), radial * math.cos(a) - tangent * math.sin(a)))
    pts = " ".join(f"{x:.2f},{y:.2f}" for x, y in corners)
    parts.append(f'<polygon points="{pts}" fill="#25465e" class="outline"/>')
    # Clear aperture at the outer flange, drawn separately from outer width.
    line(*P(11 * math.sin(a) - 1.5 * math.cos(a), 11 * math.cos(a) + 1.5 * math.sin(a)),
         *P(11 * math.sin(a) + 1.5 * math.cos(a), 11 * math.cos(a) - 1.5 * math.sin(a)))
    tx, ty = P(12.2 * math.sin(a), 12.2 * math.cos(a))
    if angle in (90, 270):
        ty += 52
    text(tx, ty + 5, f'C{angle // 90 + 1} · {angle}°', "small")
for angle in r["anglesDegrees"]:
    a = math.radians(angle)
    corners = []
    for radial, tangent in [(8, -1), (12, -1), (12, 1), (8, 1)]:
        corners.append(P(radial * math.sin(a) + tangent * math.cos(a), radial * math.cos(a) - tangent * math.sin(a)))
    pts = " ".join(f"{x:.2f},{y:.2f}" for x, y in corners)
    parts.append(f'<polygon points="{pts}" fill="#1a2730" stroke="#efbd58" stroke-width="2"/>')
    for tangent in [-0.45, 0.45]:
        line(*P(7.3 * math.sin(a) + tangent * math.cos(a), 7.3 * math.cos(a) - tangent * math.sin(a)),
             *P(9 * math.sin(a) + tangent * math.cos(a), 9 * math.cos(a) - tangent * math.sin(a)))
circle(cx, cy, h["outerDiameter"] / 2 * scale, "#2f5873")
text(cx, cy - 5, 'A · geschlossene Hülle')
text(cx, cy + 23, 'Ø 8 m')
text(cx, cy + 145, 'B · Anschlusskragen Ø 16 m', "small dark")
# True measured extension lines: diameters are taken from circular rims.
line(cx - 100, cy - 100, 80, cy - 100)
line(cx - 100, cy + 100, 80, cy + 100)
line(86, cy - 100, 86, cy + 100, "dim")
text(65, cy, '8 m')
line(cx, cy - 200, 750, cy - 200)
line(cx, cy + 200, 750, cy + 200)
line(744, cy - 200, 744, cy + 200, "dim")
text(715, cy, '16 m')
line(cx - 275, cy + 45, cx - 275, 815)
line(cx + 275, cy + 45, cx + 275, 815)
line(cx - 275, 800, cx + 275, 800, "dim")
text(cx, 832, '22 m · gegenüberliegende Anschlussflansche')
line(cx - 37.5, cy - 275, cx - 37.5, 170)
line(cx + 37.5, cy - 275, cx + 37.5, 170)
line(cx - 37.5, 180, cx + 37.5, 180, "dim")
text(cx, 164, '3 m lichte Breite', "small")
text(cx, 865, 'D · 4 diagonale Radiatoren, je 4 × 2 m, separat montiert', "small")

sx, sy = 1215, 460
X = lambda x: sx + x * scale
Y = lambda y: sy - y * scale
line(X(-12), Y(0), X(12), Y(0), "axis")
rect(X(-4), Y(3), 8 * scale, 6 * scale, "#2f5873")
rect(X(-8), Y(1.6), 16 * scale, 3.2 * scale, "#b2bdc6")
for x in [-11, 8]:
    rect(X(x), Y(1.6), 3 * scale, 3.2 * scale, "#25465e")
line(X(-11), Y(-1.3), X(11), Y(-1.3), "axis")
for sign in [-1, 1]:
    # Projection of two opposing diagonal cassettes; near/far overlap here.
    x0 = sign * 10 / math.sqrt(2) - 3 / math.sqrt(2)
    rect(X(x0), Y(1.975), 6 / math.sqrt(2) * scale, 0.15 * scale, "#1a2730", "#efbd58")
line(X(-4), Y(3), X(-4), 335)
line(X(4), Y(3), X(4), 335)
line(X(-4), 343, X(4), 343, "dim")
text(sx, 325, 'Hauptgehäuse Ø 8 m')
for y in [-3, 3]:
    line(X(4), Y(y), 1598, Y(y))
line(1588, Y(3), 1588, Y(-3), "dim")
text(1610, sy + 5, '6 m')
for y in [-1.6, 1.6]:
    line(X(11), Y(y), 1530, Y(y))
line(1518, Y(1.6), 1518, Y(-1.6), "dim")
text(1476, sy + 6, '3,2 m', "small")
line(X(-8), Y(-1.6), X(-8), 630)
line(X(8), Y(-1.6), X(8), 630)
line(X(-8), 615, X(8), 615, "dim")
text(sx, 645, 'Anschlusskragen Ø 16 m')
line(X(-11), Y(-1.6), X(-11), 575)
line(X(-8), Y(-1.6), X(-8), 575)
line(X(-11), 565, X(-8), 565, "dim")
text(X(-9.5), 594, '3 m Stutzen', "small")
# Closed front aperture, width shown in plan; height remains a separate value.
rect(855, 190, 3.6 * scale, 3.2 * scale, "#25465e")
rect(855 + 0.3 * scale, 190 + 0.3 * scale, 3 * scale, 2.6 * scale, "#111d28")
text(855, 175, 'C · Anschlussöffnung', "small", "start")
text(960, 223, '3,0 × 2,6 m lichte Öffnung', "small", "start")
text(960, 250, '3,6 × 3,2 m äußere Hülle', "small", "start")
text(960, 278, 'Geschlossenes Schott im Betrieb', "small", "start")
# Reference person's actual total drawing height is exactly 1.8 m at this scale.
px, feet, hh = 1450, 705, 1.8 * scale
circle(px, feet - hh + 4.5, 4.5, "#d4e1eb")
line(px, feet - hh + 9, px, feet - 17)
line(px, feet - 17, px - 6, feet)
line(px, feet - 17, px + 6, feet)
line(px - 10, feet - 30, px + 10, feet - 30)
text(px, 730, 'Mensch 1,8 m', "small")
text(840, 690, 'Y = 0: gemeinsame Anschlussmitte', "small", "start")
text(840, 715, 'Boden Y = −1,3 m · Decke Y = +1,3 m', "small", "start")
text(840, 740, 'Radiatoren: Y = +1,9 m, Dicke 0,15 m', "small", "start")
text(840, 790, 'Innenpaket bleibt verdeckt und unbemaßt.', "sub", "start")
text(840, 817, 'Hülle, Kragen, Stutzen und Kühler getrennt modellieren.', "sub", "start")
text(840, 852, 'Gezeigte Hüllstärken sind Modellannahmen, kein Strahlenschutz.', "small", "start")
text(35, 940, 'Einbau V4: Kragen r8 → Stutzen r11 → Ringanschluss r29; verbleibende Serviceverlängerung 18 m.', "", "start")
text(35, 972, f'Radiator-Eckradius {max_radius:.3f} m; Abstand zum Innenmodul-Hüllkreis r18: {18-max_radius:.3f} m (nur geometrisch).', "", "start")
text(35, 1005, 'Spielreaktor bleibt 2 kW · keine Wärme-/Strahlenschutzrechnung · kein Unity-Modell oder Build erstellt.', "sub", "start")
parts.append('<style>.dark{fill:#132433}</style></svg>')
OUT.write_text('\n'.join(parts) + '\n', encoding='utf-8')
ET.parse(OUT)
print(json.dumps({"svg": str(OUT.relative_to(ROOT)), "maxRadiatorCornerRadiusMetres": max_radius,
                  "clearanceToInnerModuleEnvelopeMetres": 18-max_radius,
                  "minimumPanelAxisCoordinateMetres": panel_min_axis,
                  "radiatorGapAboveServiceRoofMetres": r['centreY']-r['thickness']/2-v['outerYMax'],
                  "checks": "dimension arithmetic, cardinal ports, panel/port envelope separation, SVG XML"}, ensure_ascii=False))
