"""Analytical Slot 01 layout; writes calculation and a metre-scale SVG proposal only."""
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
A = math.radians(4.85)
C, T = math.cos(A), math.tan(A)
WALL = .185  # Current inner pane face offset normal to radial side plane.
SERVICE, UTILITY = 1.0, .45
NEAR, FAR = 39.45, 51.0  # Keeps at least 1 m beyond the existing z38.44 door shoulders.
BOTTOM, TOP, SKIN = .25, 4.85, .12

def bounds(z, interior=False):
    extra = SKIN if interior else 0
    return (-z*T+(WALL+UTILITY+extra)/C,
            z*T-(WALL+SERVICE+extra)/C)

def width(z, interior=False):
    left,right = bounds(z,interior)
    return right-left

inner_near, inner_far = NEAR+SKIN, FAR-SKIN
height = TOP-BOTTOM-2*SKIN
length = inner_far-inner_near
partitions = [inner_near+length/3, inner_near+length*2/3]
area_before = (width(inner_near,True)+width(inner_far,True))*.5*length
partition_area = sum(width(z,True)*SKIN for z in partitions)
volume = (area_before-partition_area)*height
fill_volume = volume*.95  # Space for feed head, control and jam recovery.
ice_density = 917.0
water_fraction, mineral_density = .8,2500.0  # Explicit unconfirmed composition assumption.
mixed_solid_density = 1/(water_fraction/ice_density+(1-water_fraction)/mineral_density)
scenarios=[]
for packing in [.65,.75,.85]:
    pure = fill_volume*packing*ice_density
    raw = fill_volume*packing*mixed_solid_density
    scenarios.append({'solidPackingFraction':packing,'pureIceKg':pure,
        'pureIceMeltwaterLitersApprox':pure,'rawMixtureKg':raw,
        'rawMixturePotentialWaterLitersApprox':raw*water_fraction,
        'rawMixtureUnknownKg':raw*(1-water_fraction)})
result={
    'date':'2026-10-25','status':'Layout and analytical proposal; no Unity changes or new runtime acceptance.',
    'existingSlot':{'axisDegrees':200,'angleDegrees':9.7,'innerRadiusM':38,'outerRadiusM':54,
        'visibleClearHeightM':5.410,'nominalSectorFloorM2':math.radians(9.7)*.5*(54**2-38**2)},
    'proposal':{'tankAxisPlaneNearM':NEAR,'tankAxisPlaneFarM':FAR,
        'tankOuterLengthM':FAR-NEAR,'tankOuterWidthNearM':width(NEAR),'tankOuterWidthFarM':width(FAR),
        'tankBottomM':BOTTOM,'tankTopM':TOP,'tankOuterHeightM':TOP-BOTTOM,
        'tankWallAndInsulationAssumptionM':SKIN,'serviceClearanceM':SERVICE,'utilityClearanceM':UTILITY,
        'ringEntrySpaceNominalM':NEAR-38,'ringEntryClearOfDoorShouldersM':NEAR-38.44,'processingSpaceM':54-FAR,
        'ceilingClearanceM':5.415-TOP,'tankInnerHeightM':height,
        'tankInnerVolumeBeforePartitionsM3':area_before*height,
        'tankInnerVolumeAfterTwoPartitionsM3':volume,'operationalFillFraction':.95,
        'operationalFillVolumeM3':fill_volume},
    'assumptions':{'iceDensityKgM3':ice_density,'waterMassFraction':water_fraction,
        'unknownMineralDensityKgM3':mineral_density,'defaultPackingFraction':.75,
        'unknownDensityConfirmed':False},
    'capacityScenarios':scenarios,
    'limits':['Inner volume is analytical geometry, not a built or collision-checked mesh.',
        'Packing 65/75/85 percent is a sensitivity assumption, not measured cold-ice press performance.',
        'The unknown 20 percent is only modeled as 2500 kg/m3 mineral for this scenario; actual contact density is unknown.',
        'Water equivalent is potential H2O after complete recovery; not currently available clean liquid water.',
        '12 cm wall/insulation and 1 m service aisle are layout assumptions, no strength, pressure or heat-leak validation.',
        'Crusher, ram, feed header and service access need detailed layout; no gravity hopper or belt conveyor proposed.',
        'Only intact Slot01 counted; damaged02/03 do not add current capacity.']
}
out = ROOT/'docs/Pruefungen/Eislager-Slot01-Auslegung-2026-10-25.json'
out.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

# Standalone schematic from the same metre coordinates. Text is vector text.
scale, cx, base = 31,285,660
def point(x,z): return f'{cx+x*scale:.2f},{base-(z-38)*scale:.2f}'
def polygon(points,fill,stroke='#496474',dash=''):
    return f'<polygon points="{" ".join(point(x,z) for x,z in points)}" fill="{fill}" stroke="{stroke}" stroke-width="2" {dash}/>'
def trapezoid(z1,z2,inside=False):
    l1,r1=bounds(z1,inside);l2,r2=bounds(z2,inside)
    return [(l1,z1),(r1,z1),(r2,z2),(l2,z2)]
def text(x,y,value,size=19,fill='#193544',anchor='middle',rotation=None):
    rotate = f' transform="rotate({rotation} {x} {y})"' if rotation else ''
    return f'<text x="{x}" y="{y}" text-anchor="{anchor}" font-size="{size}" fill="{fill}"{rotate}>{value}</text>'
def line(x1,y1,x2,y2,color='#456171',dashed=False):
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{color}" stroke-width="2"'+(' stroke-dasharray="7 5"' if dashed else '')+'/>'
svg=['<svg xmlns="http://www.w3.org/2000/svg" width="1120" height="830" viewBox="0 0 1120 830" role="img" aria-label="Eislager Slot01: maßstäblicher Grundriss und seitliche Höhenaufteilung, Entwurf">',
     '<rect width="1120" height="830" fill="#f5f8fa"/>','<g font-family="Segoe UI,Arial,sans-serif">',
     text(36,43,'EISLAGER 01 · RAUM- UND KAPAZITÄTSENTWURF',25,anchor='start'),
     text(36,72,'Aktueller 16-m-Lagerkeil · Vorschlag, noch keine gebaute Anlage',17,'#526875','start')]
floor=[(38*math.sin(a),38*math.cos(a)) for a in [-A+i*2*A/5 for i in range(6)]]
floor += [(54*math.sin(a),54*math.cos(a)) for a in [A-i*2*A/5 for i in range(6)]]
svg += [polygon(floor,'#e3eaee'),text(285,112,'Außen / Drohne',20)]
# Main service aisle and utility gap follow the real angled walls.
svg += [polygon([(bounds(NEAR)[1],NEAR),(NEAR*T-WALL/C,NEAR),(FAR*T-WALL/C,FAR),(bounds(FAR)[1],FAR)],'#e8dbac','#a6945f')]
svg += [polygon([(-NEAR*T+WALL/C,NEAR),(bounds(NEAR)[0],NEAR),(bounds(FAR)[0],FAR),(-FAR*T+WALL/C,FAR)],'#d0d9df','#7d909b')]
svg += [polygon(trapezoid(NEAR,FAR),'#d6ebf2','#28718b')]
cell_edges=[inner_near]+partitions+[inner_far]
for i,(low,high) in enumerate(zip(cell_edges,cell_edges[1:])):
    if i: low += SKIN/2
    if i<2: high -= SKIN/2
    svg += [polygon(trapezoid(low,high,True),'#b7dce9','#28718b')]
    svg += [text(cx-7,base-((low+high)/2-38)*scale,f'Kaltlager {i+1}',19)]
svg += [text(405,440,'Wartung 1,00 m',16,'#61532d',rotation=-90),
        text(183,440,'Leitungen 0,45 m',15,'#526875',rotation=-90),
        text(285,704,'Ringzugang · 4,40 m frei',19)]
# Receiver projects above processing hardware; shown dashed in plan.
svg += [polygon([(-.91,51.5),(.91,51.5),(.91,54.6),(-.91,54.6)],'none','#7d909b','stroke-dasharray="6 4"')]
svg += [polygon([(-3.6,51.25),(-.9,51.25),(-.9,53.2),(-3.6,53.2)],'#f2d7b5','#ad783c'),
        text(207,220,'Brecher',16),text(207,241,'+ Vorschub',15),
        polygon([(1.25,51.4),(3.1,51.4),(3.1,53.2),(1.25,53.2)],'#c7e2db','#4a8275'),
        text(353,220,'Aufbereitung',13),text(353,239,'Wassertank',13),
        text(285,148,'Receiver darüber',15,'#526875')]
svg += [line(285,156,285,192,'#7d909b',True),line(148,164,115,164),line(148,660,115,660),
        line(118,164,118,660),text(105,412,'16 m',17,rotation=-90),
        text(285,745,'3,0 m Technik · 11,55 m Tank · ≥ 1,0 m frei vor dem Schott',16)]
# Section diagram, same physical scale, not a second room floor.
svg += [text(590,127,'HÖHENAUFTEILUNG',20,anchor='start'),
        '<rect x="610" y="180" width="420" height="203" fill="#e3eaee" stroke="#496474" stroke-width="2"/>',
        '<rect x="610" y="201" width="420" height="173" fill="#b7dce9" stroke="#28718b" stroke-width="2"/>',
        text(820,167,'0,56 m Deckenraum',15),text(820,286,'4,60 m Tankaußenhöhe',20),
        text(820,317,'4,36 m lichte Innenhöhe',17),text(820,408,'0,25 m Unterbau / Anschlüsse',16),
        text(590,472,f'Innenvolumen: {volume:.0f} m³',25,anchor='start'),
        text(590,508,f'Davon betrieblich befüllbar: {fill_volume:.0f} m³',21,anchor='start'),
        text(590,557,'Bei 75 % Feststoff-Packung:',19,anchor='start'),
        text(590,592,f'Reines Eis: ca. {scenarios[1]["pureIceKg"]/1000:.0f} t',22,anchor='start'),
        text(590,627,f'80/20-Rohgemisch: ca. {scenarios[1]["rawMixtureKg"]/1000:.0f} t',22,anchor='start'),
        text(590,662,f'darin ca. {scenarios[1]["rawMixturePotentialWaterLitersApprox"]/1000:.0f}.000 L H₂O-Potenzial',20,anchor='start'),
        text(590,712,'Packung, Reststoffdichte und Isolation:',16,'#526875','start'),
        text(590,737,'Rechenannahmen, keine technische Abnahme.',16,'#526875','start'),
        text(36,802,'Kalte Zerkleinerung → geführter Transport → Verdichtung · Flüssigwasser nur nach bedarfsweiser Aufbereitung',17,anchor='start'),
        '</g></svg>']
art=ROOT/'docs/Art/Station/Eislager-Slot01-Grundriss-2026-10-25.svg'
art.write_text('\n'.join(svg),encoding='utf-8')
print(json.dumps({'volumeM3':volume,'fillM3':fill_volume,'widthNearM':width(NEAR),'widthFarM':width(FAR),'scenarios':scenarios},ensure_ascii=False,indent=2))
